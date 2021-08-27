using Signum.Entities.Authorization;
using System;

namespace Signum.Entities.Rest
{
    [Serializable, EntityKind(EntityKind.Main, EntityData.Master)]
    public class RestApiKeyEntity : Entity
    {   
        public Lite<UserEntity> User { get; set; }

        [StringLengthValidator(Min = 20, Max = 100)]
        [UniqueIndex]
        public string ApiKey { get; set; }
    }

    [AutoInit]
    public static class RestApiKeyOperation
    {
        public static ExecuteSymbol<RestApiKeyEntity> Save;
        public static DeleteSymbol<RestApiKeyEntity> Delete;
    }
}
