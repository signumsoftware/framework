using Signum.Entities.Authorization;
using System;

namespace Signum.Entities.Rest
{
    [Serializable, EntityKind(EntityKind.Main, EntityData.Master)]
    public class RestApiKeyEntity : Entity
    {
        [NotNullValidator]
        public Lite<UserEntity> User { get; set; }

        [StringLengthValidator(AllowNulls = false, Min = 20, Max = 100)]
        [UniqueIndex(AllowMultipleNulls = true)]
        public string ApiKey { get; set; }
    }

    [AutoInit]
    public static class RestApiKeyOperation
    {
        public static ExecuteSymbol<RestApiKeyEntity> Save;
        public static DeleteSymbol<RestApiKeyEntity> Delete;
    }
}
