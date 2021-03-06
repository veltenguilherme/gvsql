using System;

namespace Persistence.Controllers.Base.IO.Deserialization
{
    public class Model
    {
        public int Index { get; set; }

        public string ColumnName { get; set; }

        public object Value { get; set; }

        public Type Type { get; set; }
    }
}