using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities;

namespace Signum.Entities.Authorization
{
    [Serializable, EntityKind(EntityKind.System, EntityData.Transactional)]
    public class ResetPasswordRequestEntity : Entity
    {
        public string Code { get; set; }

        [NotNullValidator]
        public UserEntity User { get; set; }

        public DateTime RequestDate { get; set; }

        public bool Lapsed { get; set; }
    }
}
