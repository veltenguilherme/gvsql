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
        public async Task<T> UpdateOrInsertAsync(T obj)
        {
            if (obj == null)
                throw new ArgumentNullException();

            Guid guid = default;
            string value = string.Empty;

            foreach (PropertyInfo property in obj.GetType().GetProperties())
            {
                if (property.GetCustomAttribute<KeyAttribute>() != null)
                {
                    if (property.GetValue(obj) == null)
                        return await InsertAsync(obj);

                    guid = (Guid)property.GetValue(obj);
                    continue;
                }

                if (Utils.IsBaseModel(property.PropertyType.BaseType) ||
                    IsDefaultValue(property, obj))
                    continue;

                SetValue(ref value, property.GetCustomAttribute<ColumnAttribute>()?.Name ?? property.Name.Underscore(), property, obj);
            }

            return await ToListAsync(await Provider.ExecuteScalarAsync($"UPDATE public.{Name} SET {value.Remove(value.Length - 1)} WHERE uuid = '{guid}' returning uuid"));
        }

        private void SetValue(ref string value, string columnAttributeName, PropertyInfo property, T obj)
        {
            if ("inserted".Equals(columnAttributeName) || "updated".Equals(columnAttributeName))
                return;

            value += $"{columnAttributeName} = @{columnAttributeName},";
            Provider.Parameters.Add(new NpgsqlParameter(columnAttributeName, GetValue(obj, property)));
        }
    }
}