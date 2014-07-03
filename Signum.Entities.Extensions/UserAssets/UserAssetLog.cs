using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities.Basics;

namespace Signum.Entities.UserAssets
{
    [Serializable, EntityKind(EntityKind.System, EntityData.Transactional)]
    public class UserAssetLogDN : Entity
    {
        [NotNullable]
        Lite<IUserAssetEntity> asset;
        [NotNullValidator]
        public Lite<IUserAssetEntity> Asset
        {
            get { return asset; }
            set { Set(ref asset, value); }
        }

        [NotNullable]
        Lite<IUserDN> user;
        [NotNullValidator]
        public Lite<IUserDN> User
        {
            get { return user; }
            set { Set(ref user, value); }
        }

        DateTime creationDate = TimeZoneManager.Now;
        public DateTime CreationDate
        {
            get { return creationDate; }
            private set { Set(ref creationDate, value); }
        }
    }
}
