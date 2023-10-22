using Persistence.Controllers.Base.CustomAttributes;
using System;
using System.ComponentModel;
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

        [SqlType(SqlTypes.TIMESTAMP_WITHOUT_TIME_ZONE_NOT_NULL)]
        public DateTime? Inserted { get; set; }

        [SqlType(SqlTypes.TIMESTAMP_WITHOUT_TIME_ZONE)]
        public DateTime? Updated { get; set; }
    }
}