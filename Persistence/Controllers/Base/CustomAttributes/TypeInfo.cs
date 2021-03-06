using Persistence.Models;
using System;

namespace Persistence.Controllers.Base.CustomAttributes
{
    [AttributeUsage(AttributeTargets.Property)]
    public class TypeInfo : Attribute
    {
        internal DataType Type { get; set; }

        public TypeInfo(DataType type)
        {
            Type = type;
        }
    }
}