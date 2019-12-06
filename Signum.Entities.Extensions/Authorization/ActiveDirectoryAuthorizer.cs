using Signum.Entities;
using Signum.Utilities;
using System;
using System.ComponentModel;

namespace Signum.Entities.Authorization
{
    [Serializable]
    public class ActiveDirectoryConfigurationEmbedded : EmbeddedEntity
    {
        [StringLengthValidator(Max = 200)]
        public string? DomainName { get; set; }

        [StringLengthValidator(Max = 250)]
        public string? DomainServer { get; set; }

        [StringLengthValidator(Max = 100), Description("Azure Application (client) ID")]
        public string? Azure_ApplicationID { get; set; }

        [StringLengthValidator(Max = 100), Description("Azure Directory (tenant) ID")]
        public string? Azure_DirectoryID { get; set; }

        public bool LoginWithWindowsAuthenticator { get; set; } = true;
        public bool LoginWithActiveDirectoryRegistry { get; set; } = true;
        public bool LoginWithAzureAD { get; set; } = true;

        public bool AllowSimpleUserNames { get; set; } = true;


        public bool AutoCreateUsers { get; set; }

        [PreserveOrder, NoRepeatValidator]
        public MList<RoleMappingEmbedded> RoleMapping { get; set; } = new MList<RoleMappingEmbedded>();

        public Lite<RoleEntity>? DefaultRole { get; set; }
    }

    [Serializable]
    public class RoleMappingEmbedded : EmbeddedEntity
    {
        [StringLengthValidator(Max = 100)]
        public string ADNameOrGuid { get; set; }

        public Lite<RoleEntity> Role { get; set; }
    }

    public enum ActiveDirectoryAuthorizerMessage
    {
        [Description("Active Directory user '{0}' is not associated with a user in this application.")]
        ActiveDirectoryUser0IsNotAssociatedWithAUserInThisApplication,
    }
}
