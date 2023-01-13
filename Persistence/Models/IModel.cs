using System;

namespace Persistence.Models
{
    public interface IModel<T>
    {
        public Guid? Guid { get; set; }
        public DateTime? Insert { get; set; }
        public DateTime? Update { get; set; }
    }

    public enum SqlTypes
    {
        GUID = 1,
        TEXT,
        TEXT_NOT_NULL,
        TEXT_NOT_NULL_UNIQUE,
        TIMESTAMP_WITHOUT_TIME_ZONE,
        TIMESTAMP_WITHOUT_TIME_ZONE_NOT_NULL,
        DATE,
        DATE_NOT_NULL,
        INTEGER,
        INTEGER_NOT_NULL,
        BIG_INT,
        NUMERIC_DEFAULT_VALUE_0,
        BOOLEAN,
        BYTEA,
        BYTEA_NOT_NULL,
        DEFAULT
    }

    public enum SqlFkTypes
    {
        ON_DELETE_CASCADE_ON_UPDATE_NO_ACTION_NOT_NULL_UNIQUE = 1,
        ON_DELETE_CASCADE_ON_UPDATE_NO_ACTION_NOT_NULL,
        ON_DELETE_CASCADE_ON_UPDATE_NO_ACTION,
        DEFAULT
    }

    public enum SqlJoinTypes
    {
        INNER = 1,
        LEFT
    }
}