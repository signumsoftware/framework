using Signum.Entities;
using Signum.Utilities;
using System;
using System.ComponentModel;
using System.Reflection;

namespace Signum.Entities.Authorization
{
    [Serializable]
    public class ActiveDirectoryConfigurationEmbedded : EmbeddedEntity
    {
        [StringLengthValidator(Max = 200)]
        public string? DomainName { get; set; }

        [StringLengthValidator(Max = 250)]
        public string? DomainServer { get; set; }

        public string? DirectoryRegistry_Username { get; set; }

        [Format(FormatAttribute.Password)]
        public string? DirectoryRegistry_Password { get; set; }

        [StringLengthValidator(Max = 100), Description("Azure Application (client) ID")]
        public string? Azure_ApplicationID { get; set; }

        [StringLengthValidator(Max = 100), Description("Azure Directory (tenant) ID")]
        public string? Azure_DirectoryID { get; set; }

        [StringLengthValidator(Max = 100), Description("Azure Client Secret ID")]
        public string? Azure_ClientSecret { get; set; }

        public bool LoginWithWindowsAuthenticator { get; set; }
        public bool LoginWithActiveDirectoryRegistry { get; set; }
        public bool LoginWithAzureAD { get; set; }

        public bool AllowMatchUsersBySimpleUserName { get; set; } = true;

        public bool AutoCreateUsers { get; set; }
        public bool AutoUpdateUsers { get; set; }

        [PreserveOrder, NoRepeatValidator]
        public MList<RoleMappingEmbedded> RoleMapping { get; set; } = new MList<RoleMappingEmbedded>();

        public Lite<RoleEntity>? DefaultRole { get; set; }

        protected override string? PropertyValidation(PropertyInfo pi)
        {
            if(LoginWithWindowsAuthenticator || LoginWithActiveDirectoryRegistry)
            {
                if (pi.Name == nameof(DomainName) && !DomainName.HasText())
                    return ValidationMessage._0IsNotSet.NiceToString(pi.NiceName());

                if (pi.Name == nameof(DomainServer) && !DomainServer.HasText())
                    return ValidationMessage._0IsNotSet.NiceToString(pi.NiceName());
            }

            if (LoginWithAzureAD)
            {
                if (pi.Name == nameof(Azure_ApplicationID) && !Azure_ApplicationID.HasText())
                    return ValidationMessage._0IsNotSet.NiceToString(pi.NiceName());

                if (pi.Name == nameof(Azure_DirectoryID) && !Azure_DirectoryID.HasText())
                    return ValidationMessage._0IsNotSet.NiceToString(pi.NiceName());
            }

            return base.PropertyValidation(pi);
        }
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

    [AllowUnathenticated]
    public enum UserADMessage
    {
        [Description("Find '{0}' in Active Directory")]
        Find0InActiveDirectory,
        [Description("Find in Active Directory")]
        FindInActiveDirectory,
        [Description("No user containing '{0}' found in Active Directory")]
        NoUserContaining0FoundInActiveDirectory,
        [Description("Select Active Directory User")]
        SelectActiveDirectoryUser,
        [Description("Please select the user from Active Directory that you want to import")]
        PleaseSelectTheUserFromActiveDirectoryThatYouWantToImport,
        [Description("Name or e-Mail")]
        NameOrEmail,
    }

    public enum UserADQuery
    {
        ActiveDirectoryUsers
    }
}
