using System;

namespace Persistence.Controllers.Base.CustomAttributes
{
    [AttributeUsage(AttributeTargets.Property)]
    public class SqlJoinType : Attribute
    {
        internal string PatternTableName { get; set; }
        internal string FkName { get; set; }
        internal Models.SqlJoinTypes Type { get; set; }

        public SqlJoinType(string patternTableName, string fkName, Models.SqlJoinTypes type = Models.SqlJoinTypes.INNER)
        {
            PatternTableName = patternTableName;
            FkName = fkName;
            Type = type;
        }
    }
}