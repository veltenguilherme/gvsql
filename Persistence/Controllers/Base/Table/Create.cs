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
                string columnName = property.GetCustomAttribute<ColumnAttribute>() == null ? null : property.GetCustomAttribute<ColumnAttribute>().Name;
                string fkTableName = property.GetCustomAttribute<Fk>() == null ? null : property.GetCustomAttribute<Fk>().TableName;
                string fkColumnName = property.GetCustomAttribute<Fk>() == null ? null : property.GetCustomAttribute<Fk>().ColumnName;

                DataType dataType = property.GetCustomAttribute<CustomAttributes.TypeInfo>() == null ? DataType.DEFAULT : property.GetCustomAttribute<CustomAttributes.TypeInfo>().Type;
                FkType fkType = property.GetCustomAttribute<Fk>() == null ? FkType.DEFAULT : property.GetCustomAttribute<Fk>().Type;

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

        private bool SetForeignKey(FkType fkType, DataType dataType, string columnName, string fkColumnName, string fkTableName, ref List<string> sql)
        {
            switch (fkType)
            {
                case FkType.ON_DELETE_CASCADE_ON_UPDATE_NO_ACTION_NOT_NULL_UNIQUE:
                    sql.Add($"{columnName} {GetFkDataType(dataType)} REFERENCES {fkTableName}({fkColumnName}) ON DELETE CASCADE ON UPDATE NO ACTION NOT NULL UNIQUE,");
                    return true;

                case FkType.ON_DELETE_CASCADE_ON_UPDATE_NO_ACTION_NOT_NULL:
                    sql.Add($"{columnName} {GetFkDataType(dataType)} REFERENCES {fkTableName}({fkColumnName}) ON DELETE CASCADE ON UPDATE NO ACTION NOT NULL,");
                    return true;

                case FkType.ON_DELETE_CASCADE_ON_UPDATE_NO_ACTION:
                    sql.Add($"{columnName} {GetFkDataType(dataType)} REFERENCES {fkTableName}({fkColumnName}) ON DELETE CASCADE ON UPDATE NO ACTION,");
                    return true;

                default: return false;
            }
        }

        private void SetDataType(DataType type, string columName, ref List<string> sql)
        {
            switch (type)
            {
                case DataType.TEXT_NOT_NULL: sql.Add($"{columName} text NOT NULL,"); return;
                case DataType.TEXT_NOT_NULL_UNIQUE: sql.Add($"{columName} text NOT NULL UNIQUE,"); return;
                case DataType.TEXT: sql.Add($"{columName} text,"); return;
                case DataType.TIMESTAMP_WITHOUT_TIME_ZONE_NOT_NULL: sql.Add($"{columName} timestamp without time zone NOT NULL,"); return;
                case DataType.TIMESTAMP_WITHOUT_TIME_ZONE: sql.Add($"{columName} timestamp without time zone,"); return;
                case DataType.DATE: sql.Add($"{columName} date,"); return;
                case DataType.DATE_NOT_NULL: sql.Add($"{columName} date NOT NULL,"); return;
                case DataType.INTEGER: sql.Add($"{columName} integer,"); return;
                case DataType.INTEGER_NOT_NULL: sql.Add($"{columName} integer NOT NULL,"); return;
                case DataType.BIG_INT: sql.Add($"{columName} bigint,"); return;
                case DataType.NUMERIC_DEFAULT_VALUE_0: sql.Add($"{columName} numeric DEFAULT 0.00,"); return;
                case DataType.BOOLEAN: sql.Add($"{columName} boolean,"); return;
                case DataType.BYTEA: sql.Add($"{columName} bytea,"); return;
                case DataType.BYTEA_NOT_NULL: sql.Add($"{columName} bytea NOT NULL,"); return;
                case DataType.GUID:
                case DataType.DEFAULT:
                default: return;
            }
        }

        private string GetFkDataType(DataType dataType)
        {
            switch (dataType)
            {
                case DataType.GUID: return "uuid";
                default: return string.Empty;
            }
        }
    }
}