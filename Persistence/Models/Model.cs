using Persistence.Controllers.Base.CustomAttributes;
using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Persistence.Models
{
    public class Model<T> : IModel<T>
    {
        [System.ComponentModel.DataAnnotations.Key]
        [Column("uuid")]
        [SqlType(SqlTypes.GUID)]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid? Guid { get; set; }

        [Column("inserted")]
        [SqlType(SqlTypes.TIMESTAMP_WITHOUT_TIME_ZONE_NOT_NULL)]
        public DateTime? Insert { get; set; }

        [Column("updated")]
        [SqlType(SqlTypes.TIMESTAMP_WITHOUT_TIME_ZONE)]
        public DateTime? Update { get; set; }
    }
}