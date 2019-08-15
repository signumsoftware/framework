using System;

namespace Signum.Entities.Authorization
{
    [Serializable, EntityKind(EntityKind.System, EntityData.Transactional)]
    public class ResetPasswordRequestEntity : Entity
    {
        [StringLengthValidator(Max = 200)]
        public string Code { get; set; }
        
        public UserEntity User { get; set; }

        public DateTime RequestDate { get; set; }

        public bool Lapsed { get; set; }
    }

    [AutoInit]
    public static class ResetPasswordRequestOperation
    {
        public static readonly ExecuteSymbol<ResetPasswordRequestEntity> Execute;
    }
    }
