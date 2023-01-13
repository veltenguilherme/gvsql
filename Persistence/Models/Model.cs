using Persistence.Controllers.Base.CustomAttributes;
using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;

namespace Persistence.Models
{
    public class Model<T> : IModel<T>
    {
        [DefaultValue("00000000-0000-0000-0000-000000000000")] //swagger
        [System.ComponentModel.DataAnnotations.Key]
        [Column("uuid")]
        [SqlType(SqlTypes.GUID)]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid? Guid { get; set; }

        [DefaultValue(null)]
        [Column("inserted")]
        [SqlType(SqlTypes.TIMESTAMP_WITHOUT_TIME_ZONE_NOT_NULL)]
        public DateTime? Inserted { get; set; }

        [DefaultValue(null)]
        [Column("updated")]
        [SqlType(SqlTypes.TIMESTAMP_WITHOUT_TIME_ZONE)]
        public DateTime? Updated { get; set; }
    }
}