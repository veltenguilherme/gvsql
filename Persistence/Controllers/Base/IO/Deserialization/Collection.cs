using System;
using System.Collections.Generic;
using System.Data.Common;

namespace Persistence.Controllers.Base.IO.Deserialization
{
    public class Collection : List<Model>
    {
        public Collection(DbDataRecord data) => Set(data);

        private void Set(DbDataRecord data)
        {
            if (data == null || data == default) return;

            for (int i = 0; i < data.FieldCount; i++)
                Add(Get(i, data.GetName(i), data.GetValue(i), data.GetFieldType(i)));
        }

        private Model Get(int index, string columnName, object value, Type type)
        {
            Model serialization = new Model()
            {
                Index = index,
                ColumnName = columnName,
                Value = value,
                Type = type
            };

            return serialization;
        }
    }
}