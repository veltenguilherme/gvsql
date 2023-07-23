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
                    IsDefaultValue(property, obj) ||
                    SetValue(property, obj, ref value, "inserted") ||
                    SetValue(property, obj, ref value, "updated") ||
                    property.GetCustomAttribute<ColumnAttribute>() == null)
                    continue;

                SetValue(ref value, property.GetCustomAttribute<ColumnAttribute>().Name, property, obj);
            }

            return await ToListAsync(await Provider.ExecuteScalarAsync($"UPDATE public.{Name} SET {value.Remove(value.Length - 1)} WHERE uuid = '{guid}' returning uuid"));
        }

        private bool SetValue(PropertyInfo property, T obj, ref string value, string columnAttributeName)
        {
            if (property.GetCustomAttribute<ColumnAttribute>() == null ||
                !property.GetCustomAttribute<ColumnAttribute>().Name.Contains(columnAttributeName))
                return false;

            SetValue(ref value, columnAttributeName, property, obj);
            return true;
        }

        private void SetValue(ref string value, string columnAttributeName, PropertyInfo property, T obj)
        {
            value += $"{columnAttributeName} = @{columnAttributeName},";
            Provider.Parameters.Add(new NpgsqlParameter(columnAttributeName, GetValue(obj, property)));
        }

        public static string GetDataType(Models.SqlTypes type)
        {
            switch (type)
            {
                case Models.SqlTypes.TEXT_NOT_NULL: return "text NOT NULL";
                case Models.SqlTypes.TEXT_NOT_NULL_UNIQUE: return "text NOT NULL UNIQUE";
                case Models.SqlTypes.TEXT: return "text";
                case Models.SqlTypes.TIMESTAMP_WITHOUT_TIME_ZONE_NOT_NULL: return "timestamp without time zone NOT NULL";
                case Models.SqlTypes.TIMESTAMP_WITHOUT_TIME_ZONE: return "timestamp without time zone";
                case Models.SqlTypes.DATE: return "date";
                case Models.SqlTypes.DATE_NOT_NULL: return "date NOT NULL";
                case Models.SqlTypes.INTEGER: return "integer";
                case Models.SqlTypes.INTEGER_NOT_NULL: return "integer NOT NULL";
                case Models.SqlTypes.BIG_INT: return "bigint";
                case Models.SqlTypes.NUMERIC_DEFAULT_VALUE_0: return "numeric DEFAULT 0.00";
                case Models.SqlTypes.BOOLEAN: return "boolean";
                case Models.SqlTypes.BYTEA: return "bytea";
                case Models.SqlTypes.BYTEA_NOT_NULL: return "bytea NOT NULL";
                case Models.SqlTypes.GUID: return "uuid";                
                case Models.SqlTypes.GUID_NOT_NULL: return "uuid NOT NULL";                                
                case Models.SqlTypes.DEFAULT:
                default: return default;
            }
        }

        internal static object GetDefaultValue(string type)
        {
            switch (type)
            {
                case "text":
                case "integer":
                case "bigint":
                case "numeric":
                case "bytea":
                default:
                    return 0;

                case "boolean":
                    return false;

                case "uuid":
                    return Guid.NewGuid();

                case "date":
                case "timestamp":
                    return DateTime.MinValue;
            }
        }
    }
}