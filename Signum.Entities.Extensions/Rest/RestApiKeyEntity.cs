using Signum.Entities;
using Signum.Entities.Authorization;
using Signum.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntTec.Entities
{
    [Serializable, EntityKind(EntityKind.Main, EntityData.Master)]
    public class RestApiKeyEntity : Entity
    {
        [NotNullable]
        [NotNullValidator]
        public Lite<UserEntity> User { get; set; }

        [SqlDbType(Size = 40)]
        [StringLengthValidator(AllowNulls = false, Min = 40, Max = 40)]
        [UniqueIndex(AllowMultipleNulls = true)]
        public string ApiKey { get; set; }
    }

    [AutoInit]
    public static class RestApiKeyOperation
    {
        public static ExecuteSymbol<RestApiKeyEntity> Save;
    }
}
