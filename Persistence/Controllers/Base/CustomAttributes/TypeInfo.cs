using Persistence.Models;
using System;

namespace Persistence.Controllers.Base.CustomAttributes
{
    [AttributeUsage(AttributeTargets.Property)]
    public class TypeInfo : Attribute
    {
        internal DataType Type { get; set; }
        internal object Value { get; set; }

        public TypeInfo(DataType type, object value = default)
        {
            Type = type;
            Value = value;
        }
    }
}