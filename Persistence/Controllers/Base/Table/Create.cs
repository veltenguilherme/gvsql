using Humanizer;
using Persistence.Controllers.Base.CustomAttributes;
using Persistence.Models;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;

namespace Persistence.Controllers.Base.Table
{
    public class Create
    {
        public string Set<T>(string tableName)
        {
            if (string.IsNullOrEmpty(tableName) || string.IsNullOrWhiteSpace(tableName))
                return default;

            string sqlBase = Resource.tbl_create.Trim().Replace("{0}", tableName);
            List<string> sqlQueries = new List<string>();

            foreach (PropertyInfo property in typeof(T).GetProperties())
            {
                if (Utils.IsBaseModel(property.PropertyType.BaseType)) continue;

                string columnName = property.GetCustomAttribute<ColumnAttribute>()?.Name ?? property.Name.Underscore();
                string fkTableName = property.GetCustomAttribute<SqlJoin>()?.TableName;
                string fkColumnName = property.GetCustomAttribute<SqlJoin>()?.ColumnName;

                SqlTypes dataType = property.GetCustomAttribute<SqlType>() == null ? SqlTypes.DEFAULT : property.GetCustomAttribute<SqlType>().Type;
                SqlFkTypes fkType = property.GetCustomAttribute<SqlJoin>() == null ? SqlFkTypes.DEFAULT : property.GetCustomAttribute<SqlJoin>().FkType;

                if (SetForeignKey(fkType, dataType, columnName, fkColumnName, fkTableName, ref sqlQueries))
                    continue;

                SetDataType(dataType, columnName, ref sqlQueries);
            }

            if (sqlQueries.Count == 0) return sqlBase;

            Utils.RemoveLastComma(ref sqlQueries);

            string sql = string.Empty;
            sqlQueries.ForEach(x => sql += x);
            return sqlBase.Replace("{1}", sql);
        }

        private bool SetForeignKey(SqlFkTypes fkType, SqlTypes dataType, string columnName, string fkColumnName, string fkTableName, ref List<string> sql)
        {
            switch (fkType)
            {
                case SqlFkTypes.ON_DELETE_CASCADE_ON_UPDATE_NO_ACTION_NOT_NULL_UNIQUE:
                    sql.Add($"{columnName} {GetFkDataType(dataType)} REFERENCES {fkTableName}({fkColumnName}) ON DELETE CASCADE ON UPDATE NO ACTION NOT NULL UNIQUE,");
                    return true;

                case SqlFkTypes.ON_DELETE_CASCADE_ON_UPDATE_NO_ACTION_NOT_NULL:
                    sql.Add($"{columnName} {GetFkDataType(dataType)} REFERENCES {fkTableName}({fkColumnName}) ON DELETE CASCADE ON UPDATE NO ACTION NOT NULL,");
                    return true;

                case SqlFkTypes.ON_DELETE_CASCADE_ON_UPDATE_NO_ACTION:
                    sql.Add($"{columnName} {GetFkDataType(dataType)} REFERENCES {fkTableName}({fkColumnName}) ON DELETE CASCADE ON UPDATE NO ACTION,");
                    return true;

                default: return false;
            }
        }

        private void SetDataType(SqlTypes type, string columName, ref List<string> sql)
        {
            switch (type)
            {
                case SqlTypes.TEXT_UNIQUE: sql.Add($"{columName} text UNIQUE,"); return;
                case SqlTypes.TEXT_NOT_NULL: sql.Add($"{columName} text NOT NULL,"); return;
                case SqlTypes.TEXT_NOT_NULL_UNIQUE: sql.Add($"{columName} text NOT NULL UNIQUE,"); return;
                case SqlTypes.TEXT: sql.Add($"{columName} text,"); return;
                case SqlTypes.TIMESTAMP_WITHOUT_TIME_ZONE_NOT_NULL: sql.Add($"{columName} timestamp without time zone NOT NULL,"); return;
                case SqlTypes.TIMESTAMP_WITHOUT_TIME_ZONE: sql.Add($"{columName} timestamp without time zone,"); return;
                case SqlTypes.DATE: sql.Add($"{columName} date,"); return;
                case SqlTypes.DATE_NOT_NULL: sql.Add($"{columName} date NOT NULL,"); return;
                case SqlTypes.INTEGER: sql.Add($"{columName} integer,"); return;
                case SqlTypes.INTEGER_NOT_NULL: sql.Add($"{columName} integer NOT NULL,"); return;
                case SqlTypes.INTEGER_NOT_NULL_UNIQUE: sql.Add($"{columName} integer NOT NULL UNIQUE,"); return;
                case SqlTypes.BIG_INT: sql.Add($"{columName} bigint,"); return;
                case SqlTypes.NUMERIC_DEFAULT_VALUE_0: sql.Add($"{columName} numeric DEFAULT 0.00,"); return;
                case SqlTypes.BOOLEAN: sql.Add($"{columName} boolean,"); return;
                case SqlTypes.BYTEA: sql.Add($"{columName} bytea,"); return;
                case SqlTypes.BYTEA_NOT_NULL: sql.Add($"{columName} bytea NOT NULL,"); return;
                case SqlTypes.GUID: if (!"uuid".Equals(columName)) sql.Add($"{columName} uuid,"); return;                
                case SqlTypes.GUID_NOT_NULL: if (!"uuid".Equals(columName)) sql.Add($"{columName} uuid NOT NULL,"); return;                                
                case SqlTypes.DEFAULT:
                default: return;
            }
        }

        private string GetFkDataType(SqlTypes dataType)
        {
            switch (dataType)
            {
                case SqlTypes.GUID: return "uuid";
                default: return string.Empty;
            }
        }
    }
}