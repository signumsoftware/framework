using Signum.Entities.Scheduler;
using System;
using Signum.Utilities;

namespace Signum.Entities.Authorization
{
    [Serializable, EntityKind(EntityKind.System, EntityData.Transactional)]
    public class ResetPasswordRequestEntity : Entity
    {
        [UniqueIndex(AvoidAttachToUniqueIndexes = true)]
        public string Code { get; set; }
        
        public UserEntity User { get; set; }

        public DateTime RequestDate { get; set; }

        public bool Used { get; set; }
        
        public bool IsValid => As.Expression(() => !Used && TimeZoneManager.Now < RequestDate.AddHours(24));
    }

    [AutoInit]
    public static class ResetPasswordRequestOperation
    {
        public static readonly ExecuteSymbol<ResetPasswordRequestEntity> Execute;
    }
}
