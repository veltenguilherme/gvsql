using System;

namespace Persistence.Controllers.Base.CustomAttributes
{
    [AttributeUsage(AttributeTargets.Property)]
    public class JoinType : Attribute
    {
        internal string PatternTableName { get; set; }
        internal string FkName { get; set; }
        internal Models.JoinType Type { get; set; }

        public JoinType(string patternTableName, string fkName, Models.JoinType type = Models.JoinType.INNER)
        {
            PatternTableName = patternTableName;
            FkName = fkName;
            Type = type;
        }
    }
}