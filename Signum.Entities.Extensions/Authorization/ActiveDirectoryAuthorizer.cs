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
    }

    public enum ActiveDirectoryAuthorizerMessage
    {
        [Description("Active Directory user '{0}' is not associated with a user in this application.")]
        ActiveDirectoryUser0IsNotAssociatedWithAUserInThisApplication,
    }
}
