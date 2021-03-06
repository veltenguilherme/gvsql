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
            {
                this.Add(Get(i, data.GetName(i), data.GetValue(i), data.GetFieldType(i)));
            }
        }

        private Model Get(int Index, string ColumnName, object Value, Type Type)
        {
            Model serialization = new Model()
            {
                Index = Index,
                ColumnName = ColumnName,
                Value = Value,
                Type = Type
            };

            return serialization;
        }
    }
}