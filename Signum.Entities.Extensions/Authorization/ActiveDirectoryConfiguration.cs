using Signum.Entities;
using Signum.Utilities;
using System;
using System.ComponentModel;
using System.Linq.Expressions;
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
        ActiveDirectoryUsers,
        ActiveDirectoryGroups,
    }

    public enum ActiveDirectoryMessage
    {
        Id,
        DisplayName,
        Mail,
        GivenName,
        Surname,
        JobTitle,
        OnPremisesImmutableId,
        CompanyName,
        AccountEnabled,
        OnPremisesExtensionAttributes,
        OnlyActiveUsers,
        InGroup,
        Description,
        SecurityEnabled,
        Visibility,
        HasUser,
    }

    [Serializable]
    public class OnPremisesExtensionAttributesModel : ModelEntity
    {
        public string ExtensionAttribute1 { get; set; }
        public string ExtensionAttribute2 { get; set; }
        public string ExtensionAttribute3 { get; set; }
        public string ExtensionAttribute4 { get; set; }
        public string ExtensionAttribute5 { get; set; }
        public string ExtensionAttribute6 { get; set; }
        public string ExtensionAttribute7 { get; set; }
        public string ExtensionAttribute8 { get; set; }
        public string ExtensionAttribute9 { get; set; }
        public string ExtensionAttribute10 { get; set; }
        public string ExtensionAttribute11 { get; set; }
        public string ExtensionAttribute12 { get; set; }
        public string ExtensionAttribute13 { get; set; }
        public string ExtensionAttribute14 { get; set; }
        public string ExtensionAttribute15 { get; set; }
    }

    [Serializable, EntityKind(EntityKind.String, EntityData.Master), PrimaryKey(typeof(Guid))]
    public class ADGroupEntity : Entity
    {
        [UniqueIndex]
        [StringLengthValidator(Max = 100)]
        public string DisplayName { get; set; }

        [AutoExpressionField]
        public override string ToString() => As.Expression(() => DisplayName);
    }

    [AutoInit]
    public static class ADGroupOperation
    {
        public static readonly ExecuteSymbol<ADGroupEntity> Save;
        public static readonly DeleteSymbol<ADGroupEntity> Delete;
    }
}
