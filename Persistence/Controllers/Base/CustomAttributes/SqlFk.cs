using Persistence.Models;
using System;

namespace Persistence.Controllers.Base.CustomAttributes
{
    [AttributeUsage(AttributeTargets.Property)]
    public class SqlFk : Attribute
    {
        internal string ColumnName { get; set; }
        internal string TableName { get; set; }
        internal SqlFkTypes Type { get; set; }

        public SqlFk(string tableName, string columnName, SqlFkTypes type)
        {
            Type = type;
            TableName = tableName;
            ColumnName = columnName;
        }
    }
}