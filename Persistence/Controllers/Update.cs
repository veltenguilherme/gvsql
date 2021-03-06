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
        public async Task<T> UpdateOrInsertAsync(T obj)
        {
            if (obj == null) throw new ArgumentNullException();

            Guid guid = default;
            string value = string.Empty;

            foreach (PropertyInfo property in obj.GetType().GetProperties())
            {
                if (property.GetCustomAttribute<KeyAttribute>() != null)
                {
                    if (property.GetValue(obj) == null) return await InsertAsync(obj);
                    guid = (Guid)property.GetValue(obj);
                    continue;
                }

                if (Utils.IsBaseModel(property.PropertyType.BaseType)) continue;
                if (IsDefaultValue(property, obj)) continue;
                if (SetValue(property, obj, ref value, "inserted")) continue;
                if (SetValue(property, obj, ref value, "updated")) continue;
                if (property.GetCustomAttribute<ColumnAttribute>() == null) continue;

                SetValue(ref value, property.GetCustomAttribute<ColumnAttribute>().Name, property, obj);
            }

            string sql = $"UPDATE public.{Name} SET {value.Remove(value.Length - 1)} WHERE uuid = '{guid}' returning uuid";
            return await ToListAsync(await Provider.ExecuteScalarAsync(sql));
        }

        private bool SetValue(PropertyInfo property, T obj, ref string value, string columnAttributeName)
        {
            if (property.GetCustomAttribute<ColumnAttribute>() == null) return false;
            if (!property.GetCustomAttribute<ColumnAttribute>().Name.Contains(columnAttributeName)) return false;

            SetValue(ref value, columnAttributeName, property, obj);
            return true;
        }

        private void SetValue(ref string value, string columnAttributeName, PropertyInfo property, T obj)
        {
            value += $"{columnAttributeName} = @{columnAttributeName},";
            Provider.Parameters.Add(new NpgsqlParameter(columnAttributeName, property.GetValue(obj)));
        }
    }
}