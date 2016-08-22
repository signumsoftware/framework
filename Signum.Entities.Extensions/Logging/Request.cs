using Signum.Entities;
using Signum.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using static System.Int32;

namespace Signum.Entities.RestLogging
{
    [Serializable, EntityKind(EntityKind.System, EntityData.Transactional)]
    public class RequestEntity : Entity
    {
        [NotNullable, SqlDbType(Size = MaxValue)]
        [StringLengthValidator(AllowNulls = false, Max = MaxValue)]
        public string URL { get; set; }

        [NotNullable, SqlDbType(Size = MaxValue)]
        [StringLengthValidator(AllowNulls = false, Min = 3, Max = MaxValue, MultiLine = true)]
        public string Response { get; set; }

        public DateTime CreationDate { get; private set; } = TimeZoneManager.Now;


        [NotNullable, PreserveOrder]
        [NotNullValidator, NoRepeatValidator]
        public MList<RequestValueEntity> Values { get; set; } = new MList<RequestValueEntity>();

    }

    [Serializable]
    public class RequestValueEntity : EmbeddedEntity
    {
        [NotNullable, SqlDbType(Size = 100)]
        [StringLengthValidator(AllowNulls = false, Min = 3, Max = 100)]
        public string Key { get; set; }

        [NotNullable, SqlDbType(Size = 100)]
        [StringLengthValidator(AllowNulls = false, Min = 0, Max = 100)]
        public string Value { get; set; }


    }
    
}
