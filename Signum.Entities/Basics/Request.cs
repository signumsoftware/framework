using Signum.Entities;
using Signum.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Signum.Entities.Basics
{
    [Serializable, EntityKind(EntityKind.System, EntityData.Transactional)]
    public class RequestEntity : Entity
    {
        [NotNullable, SqlDbType(Size = 100)]
        [StringLengthValidator(AllowNulls = false, Min = 3, Max = 100)]
        public string Request { get; set; }

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
