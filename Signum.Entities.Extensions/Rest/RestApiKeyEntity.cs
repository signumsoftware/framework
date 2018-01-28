using Signum.Entities;
using Signum.Entities.Authorization;
using Signum.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
