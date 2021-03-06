using Persistence.Models;
using System;

namespace Persistence.Controllers.Base.CustomAttributes
{
    [AttributeUsage(AttributeTargets.Property)]
    public class Fk : Attribute
    {
        internal string ColumnName { get; set; }
        internal string TableName { get; set; }
        internal FkType Type { get; set; }

        public Fk(string tableName, string columnName, FkType type)
        {
            Type = type;
            TableName = tableName;
            ColumnName = columnName;
        }
    }
}