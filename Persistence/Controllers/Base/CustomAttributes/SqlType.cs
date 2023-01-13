using Persistence.Models;
using System;

namespace Persistence.Controllers.Base.CustomAttributes
{
    [AttributeUsage(AttributeTargets.Property)]
    public class SqlType : Attribute
    {
        internal SqlTypes Type { get; set; }
        internal object Value { get; set; }

        public SqlType(SqlTypes type, object value = default)
        {
            Type = type;
            Value = value;
        }
    }
}