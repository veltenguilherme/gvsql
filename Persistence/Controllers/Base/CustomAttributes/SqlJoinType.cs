using Persistence.Models;
using System;

namespace Persistence.Controllers.Base.CustomAttributes
{
    [AttributeUsage(AttributeTargets.Property)]
    public class SqlJoin : Attribute
    {
        internal string TableName { get; set; }
        internal string ColumnName { get; set; }
        internal string FkName { get; set; }
        internal SqlJoinTypes JoinType { get; set; }
        internal SqlFkTypes FkType { get; set; }

        public SqlJoin(string tableName,
                       SqlJoinTypes joinType = SqlJoinTypes.INNER, SqlFkTypes fkType = SqlFkTypes.ON_DELETE_CASCADE_ON_UPDATE_NO_ACTION_NOT_NULL,
                       string fkName = default, string columnName = "uuid")
        {
            TableName = tableName;
            ColumnName = columnName;
            FkName = fkName;
            JoinType = joinType;
            FkType = fkType;
        }
    }
}