using Humanizer;
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
            if (obj == null)
                throw new ArgumentNullException();

            string name = string.Empty;
            string value = string.Empty;

            foreach (PropertyInfo property in obj.GetType().GetProperties())
            {
                if (property.GetCustomAttribute<KeyAttribute>() != null ||
                    IsDefaultValue(property, obj) ||
                    Utils.IsBaseModel(property.PropertyType.BaseType))
                    continue;

                SetValue(ref name, ref value, property.GetCustomAttribute<ColumnAttribute>()?.Name ?? property.Name.Underscore(), property, obj);
            }

            return await ToListAsync(await Provider.ExecuteScalarAsync($"INSERT INTO public.{Name}({name.Remove(name.Length - 1)})VALUES({value.Remove(value.Length - 1)}) returning uuid;"));
        }

        private void SetValue(ref string name, ref string value, string propertyName, PropertyInfo property, T obj)
        {
            if ("inserted".Equals(propertyName) || "updated".Equals(propertyName))
                return;

            name += $"{propertyName},";
            value += $"@{propertyName},";
            Provider.Parameters.Add(new NpgsqlParameter(propertyName, GetValue(obj, property)));
        }
    }
}