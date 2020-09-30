using System;
using Signum.Utilities;
using System.Text.RegularExpressions;
using Signum.Entities;
using System.Linq.Expressions;

namespace Signum.Entities.Authorization
{
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

    [AutoInit]
    public static class WebAuthnCredentialOperation
    {
        public static DeleteSymbol<WebAuthnCredentialEntity> Delete;
    }

    [Serializable, EntityKind(EntityKind.System, EntityData.Transactional), PrimaryKey(typeof(Guid))]
    public class WebAuthnMakeCredentialsOptionsEntity : Entity
    {
        public Lite<UserEntity> User { get; set; }

        public DateTime CreationDate { get; private set; } = TimeZoneManager.Now;

        [StringLengthValidator(MultiLine =  true)]
        public string Json { get; set; }
    }

    [Serializable, EntityKind(EntityKind.System, EntityData.Transactional), PrimaryKey(typeof(Guid))]
    public class WebAuthnAssertionOptionsEntity : Entity
    {
        public DateTime CreationDate { get; private set; } = TimeZoneManager.Now;

        [StringLengthValidator(MultiLine = true)]
        public string Json { get; set; }
    }


    [Serializable]
    public class WebAuthnConfigurationEmbedded : EmbeddedEntity
    {
        [StringLengthValidator(Max = 200)]
        public string ServerDomain { get; set; } = "localhost";

        [StringLengthValidator(Max = 200)]
        public string ServerName { get; set; } // = "YourApp";;

        [StringLengthValidator(Max = 200)]
        public string Origin { get; set; } = "https://localhost";

        public int TimestampDriftTolerance { get; set; } = 300000;
    }


}
