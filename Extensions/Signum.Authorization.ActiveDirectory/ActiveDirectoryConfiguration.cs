using Signum.Authorization;
using Signum.Scheduler;
using System.ComponentModel;

namespace Signum.Authorization.ActiveDirectory;

public class ActiveDirectoryConfigurationEmbedded : EmbeddedEntity
{
    [StringLengthValidator(Max = 200)]
    public string? DomainName { get; set; }

    public string? DirectoryRegistry_Username { get; set; }

    [Format(FormatAttribute.Password)]
    public string? DirectoryRegistry_Password { get; set; }

    //Azure Portal -> Azure Active Directory -> App Registrations -> + New Registration

    [Description("Azure Application (client) ID")]
    public Guid? Azure_ApplicationID { get; set; } 

    [Description("Azure Directory (tenant) ID")]
    public Guid? Azure_DirectoryID { get; set; }

    [Description("Azure B2C")]
    public AzureB2CEmbedded? AzureB2C { get; set; }

    //Only for Microsoft Graph / Sending Emails 
    //Your App Registration -> Certificates & secrets -> + New client secret
    [StringLengthValidator(Max = 100), Description("Azure Client Secret Value")]
    public string? Azure_ClientSecret { get; set; }

    public bool UseDelegatedPermission { get; set; }

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
        }

        if (LoginWithAzureAD)
        {
            if (pi.Name == nameof(Azure_ApplicationID) && Azure_ApplicationID == null)
                return ValidationMessage._0IsNotSet.NiceToString(pi.NiceName());

            if (pi.Name == nameof(Azure_DirectoryID) && Azure_DirectoryID == null)
                return ValidationMessage._0IsNotSet.NiceToString(pi.NiceName());
        }

        return base.PropertyValidation(pi);
    }
}

//https://learn.microsoft.com/en-us/azure/active-directory-b2c/configure-authentication-sample-spa-app
public class AzureB2CEmbedded : EmbeddedEntity
{
    [StringLengthValidator(Max = 100)]
    public string TenantName { get; set; }

    [StringLengthValidator(Max = 100)]
    public string SignInSignUp_UserFlow { get; set; }
}

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

[AutoInit]
public static class ActiveDirectoryTask
{
    public static readonly SimpleTaskSymbol DeactivateUsers;
}

