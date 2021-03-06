using Npgsql;
using Persistence.Controllers.Base;
using Persistence.Controllers.Base.CustomAttributes;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;
using System.Threading.Tasks;

namespace Persistence.Controllers
{
    public abstract partial class Table<T> : Controller<T>
    {
        private async Task<T> InsertAsync(T obj)
        {
            if (obj == null) throw new ArgumentNullException();

            string name = string.Empty;
            string value = string.Empty;

            foreach (PropertyInfo property in obj.GetType().GetProperties())
            {
                if (property.GetCustomAttribute<KeyAttribute>() != null ||
                    Utils.IsBaseModel(property.PropertyType.BaseType) ||
                    SetValue(property, obj, ref name, ref value, "inserted") ||
                    SetValue(property, obj, ref name, ref value, "updated") ||
                    property.GetCustomAttribute<ColumnAttribute>() == null) continue;

                SetValue(ref name, ref value, property.GetCustomAttribute<ColumnAttribute>().Name, property, obj);
            }

            string sql = $"INSERT INTO public.{Name}({name.Remove(name.Length - 1)})VALUES({value.Remove(value.Length - 1)}) returning uuid;";
            return await this.ToListAsync(await Provider.ExecuteScalarAsync(sql));
        }

        private bool SetValue(PropertyInfo property, T obj, ref string name, ref string value, string columnAttributeName)
        {
            if (property.GetCustomAttribute<ColumnAttribute>() == null) return false;

            if (!property.GetCustomAttribute<ColumnAttribute>().Name.Contains(columnAttributeName)) return false;
            SetValue(ref name, ref value, columnAttributeName, property, obj);
            return true;
        }

        private void SetValue(ref string name, ref string value, string propertyName, PropertyInfo property, T obj)
        {
            name += $"{propertyName},";
            value += $"@{propertyName},";
            Provider.Parameters.Add(new NpgsqlParameter(propertyName, property.GetValue(obj)));
        }
    }
}