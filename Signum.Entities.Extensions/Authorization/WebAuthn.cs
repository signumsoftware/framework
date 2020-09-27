using System;
using Signum.Utilities;
using System.Text.RegularExpressions;
using Signum.Entities;
using System.Linq.Expressions;

namespace Signum.Entities.Authorization
{
    [Serializable, EntityKind(EntityKind.System, EntityData.Transactional), PrimaryKey(typeof(Guid))]
    public class WebAuthnCredentialsCreateOptionsEntity : Entity
    {
        public Lite<UserEntity> User { get; set; }

        public DateTime CreationDate { get; private set; } = TimeZoneManager.Now;

        [DbType(Size = int.MaxValue)]
        public string Json { get; set; }
    }

    [Serializable, EntityKind(EntityKind.System, EntityData.Transactional)]
    public class WebAuthnCredentialEntity : Entity
    {
        public Lite<UserEntity> User { get; set; }

        public DateTime CreationDate { get; private set; } = TimeZoneManager.Now;

        public byte[] CredentialId { get; set; }
        public int Counter { get; internal set; }
        [DbType(Size = 300)]
        public string CredType { get; internal set; }
        public Guid Aaguid { get; internal set; }
        public byte[] PublicKey { get; internal set; }
    }
}
