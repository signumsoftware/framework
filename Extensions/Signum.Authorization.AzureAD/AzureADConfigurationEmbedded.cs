using Signum.Authorization.ADGroups;
using System.ComponentModel;

namespace Signum.Authorization.AzureAD;

public class AzureADConfigurationEmbedded : BaseADConfigurationEmbedded
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

    [StringLengthValidator(Max = 300)]
    public string? SignInSignUp_UserFlow { get; set; }

    [StringLengthValidator(Max = 300)]
    public string? SignIn_UserFlow { get; set; }

    [StringLengthValidator(Max = 300)]
    public string? SignUp_UserFlow { get; set; }

    [StringLengthValidator(Max = 300)]
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
