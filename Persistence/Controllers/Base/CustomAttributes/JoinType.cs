using System;

namespace Persistence.Controllers.Base.CustomAttributes
{
    [AttributeUsage(AttributeTargets.Property)]
    public class JoinType : Attribute
    {
        internal string PatternTableName { get; set; }
        internal Models.JoinType Type { get; set; }

        public JoinType(string patternTableName, Models.JoinType type = Models.JoinType.INNER)
        {
            PatternTableName = patternTableName;
            Type = type;
        }
    }
}