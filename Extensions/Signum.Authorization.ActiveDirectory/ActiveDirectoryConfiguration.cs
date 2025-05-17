using Microsoft.Graph.Groups.Item.MembersWithLicenseErrors;
using Signum.Authorization;
using Signum.Scheduler;
using System.ComponentModel;

namespace Signum.Authorization.ActiveDirectory;

public class ActiveDirectoryConfigurationEmbedded : EmbeddedEntity
{
    public WindowsActiveDirectoryEmbedded? WindowsAD { get; set; }

    public WindowsActiveDirectoryEmbedded GetWindowsAD() => WindowsAD ?? throw new ApplicationException("WindowsAD is not set in " + typeof(ActiveDirectoryConfigurationEmbedded).Name);

    public AzureActiveDirectoryEmbedded? AzureAD { get; set; }

    public AzureActiveDirectoryEmbedded GetAzureAD() => AzureAD ?? throw new ApplicationException("AzureAD is not set in " + typeof(AzureActiveDirectoryEmbedded).Name);

    public bool AllowMatchUsersBySimpleUserName { get; set; } = true;

    public bool AutoCreateUsers { get; set; }
    public bool AutoUpdateUsers { get; set; }

    [PreserveOrder, NoRepeatValidator]
    public MList<RoleMappingEmbedded> RoleMapping { get; set; } = new MList<RoleMappingEmbedded>();

    public Lite<RoleEntity>? DefaultRole { get; set; }
}

public class WindowsActiveDirectoryEmbedded : EmbeddedEntity
{
    public bool LoginWithWindowsAuthenticator { get; set; }

    public bool LoginWithActiveDirectoryRegistry { get; set; }

    [StringLengthValidator(Max = 200)]
    public string? DomainName { get; set; }

    public string? DirectoryRegistry_Username { get; set; }

    [Format(FormatAttribute.Password)]
    public string? DirectoryRegistry_Password { get; set; }

    protected override string? PropertyValidation(PropertyInfo pi)
    {
        if (LoginWithWindowsAuthenticator || LoginWithActiveDirectoryRegistry)
        {
            if (pi.Name == nameof(DomainName) && !DomainName.HasText())
                return ValidationMessage._0IsNotSet.NiceToString(pi.NiceName());
        }

        return base.PropertyValidation(pi);
    }

}

public class AzureActiveDirectoryEmbedded : EmbeddedEntity
{
    public bool LoginWithAzureAD { get; set; }

    //Azure Portal -> Azure Active Directory -> App Registrations -> + New Registration

    [Description("Application (client) ID")]
    public Guid ApplicationID { get; set; }

    [Description("Directory (tenant) ID")]
    public Guid DirectoryID { get; set; }

    [Description("Azure B2C")]
    public AzureB2CEmbedded? AzureB2C { get; set; }

    //Only for Microsoft Graph / Sending Emails 
    //Your App Registration -> Certificates & secrets -> + New client secret
    [StringLengthValidator(Max = 100), Description("Client Secret Value")]
    public string? ClientSecret { get; set; }

    public bool UseDelegatedPermission { get; set; }

    public AzureADConfigTS? ToAzureADConfigTS() => !LoginWithAzureAD && AzureB2C == null ? null : new AzureADConfigTS
    {
        LoginWithAzureAD = LoginWithAzureAD,
        ApplicationId = ApplicationID.ToString(),
        TenantId = DirectoryID.ToString(),
        AzureB2C = AzureB2C == null || AzureB2C.LoginWithAzureB2C == false ? null : AzureB2C.ToAzureB2CConfigTS()
    };


}

//https://learn.microsoft.com/en-us/azure/active-directory-b2c/configure-authentication-sample-spa-app
public class AzureB2CEmbedded : EmbeddedEntity
{
    public bool LoginWithAzureB2C { get; set; }

    [StringLengthValidator(Max = 100)]
    public string TenantName { get; set; }

    [StringLengthValidator(Max = 100)]
    public string? SignInSignUp_UserFlow { get; set; }

    [StringLengthValidator(Max = 100)]
    public string? SignIn_UserFlow { get; set; }

    [StringLengthValidator(Max = 100)]
    public string? SignUp_UserFlow { get; set; }

    [StringLengthValidator(Max = 100)]
    public string? ResetPassword_UserFlow { get; set; }

    internal AzureB2CConfigTS ToAzureB2CConfigTS() => new AzureB2CConfigTS
    {
        TenantName = TenantName,
        SignInSignUp_UserFlow = SignInSignUp_UserFlow,
        SignIn_UserFlow = SignIn_UserFlow,
        SignUp_UserFlow = SignUp_UserFlow,
        ResetPassword_UserFlow = ResetPassword_UserFlow,
    };

    public string GetDefaultSignInFlow()
    {
        return SignInSignUp_UserFlow.DefaultText(SignIn_UserFlow!);
    }

    protected override string? PropertyValidation(PropertyInfo pi)
    {
        if (pi.Name == nameof(SignInSignUp_UserFlow) && SignInSignUp_UserFlow.IsNullOrEmpty() && LoginWithAzureB2C && SignIn_UserFlow.IsNullOrEmpty())
            return ValidationMessage._0IsNotSet.NiceToString(pi.NiceName());

        return base.PropertyValidation(pi);
    }
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

[AllowUnauthenticated]
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

public class AzureADConfigTS
{
    public bool LoginWithAzureAD;
    public string ApplicationId;
    public string TenantId;

    public AzureB2CConfigTS? AzureB2C;
}

public class AzureB2CConfigTS
{
    public string TenantName;
    public string? SignInSignUp_UserFlow;
    public string? SignIn_UserFlow;
    public string? SignUp_UserFlow;
    public string? ResetPassword_UserFlow;
}
