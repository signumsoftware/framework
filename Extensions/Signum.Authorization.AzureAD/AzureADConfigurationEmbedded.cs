using Signum.Authorization.BaseAD;
using Signum.Mailing;
using System.ComponentModel;
using System.Text.Json.Serialization;

namespace Signum.Authorization.AzureAD;

public class AzureADConfigurationEmbedded : BaseADConfigurationEmbedded
{
    public bool Enabled { get; set; }

    public AzureADType Type { get; set; }

    [Description("Application (client) ID")]
    public Guid ApplicationID { get; set; }

    [Description("Directory (tenant) ID")]
    public Guid DirectoryID { get; set; }

    [StringLengthValidator(Max = 100)]
    public string? TenantName { get; set; }

    [StringLengthValidator(Max = 300)]
    public string? SignInSignUp_UserFlow { get; set; }

    [StringLengthValidator(Max = 300)]
    public string? SignIn_UserFlow { get; set; }

    [StringLengthValidator(Max = 300)]
    public string? SignUp_UserFlow { get; set; }

    [StringLengthValidator(Max = 300)]
    public string? EditProfile_UserFlow { get; set; }

    [StringLengthValidator(Max = 300)]
    public string? ResetPassword_UserFlow { get; set; }
    //Only for Microsoft Graph / Sending Emails 
    //Your App Registration -> Certificates & secrets -> + New client secret
    [StringLengthValidator(Max = 100), Description("Client Secret Value")]
    public string? ClientSecret { get; set; }

    public bool UseDelegatedPermission { get; set; }


    protected override string? PropertyValidation(PropertyInfo pi)
    {
        if (Enabled == false)
            return null;

        if (Type == AzureADType.B2C)
        {
            if (pi.Name == nameof(SignInSignUp_UserFlow) && SignInSignUp_UserFlow.IsNullOrEmpty() && SignIn_UserFlow.IsNullOrEmpty())
                return ValidationMessage._0IsNotSet.NiceToString(pi.NiceName());
        }

        if(Type == AzureADType.ExternalID)
        {
            if (pi.Name == nameof(TenantName) && TenantName.HasText() && !TenantName.Contains("."))
                return ValidationMessage._0DoesNotHaveAValid1Format.NiceToString(pi.NiceName(), "b2clogin domain");

            if (pi.Name == nameof(SignInSignUp_UserFlow) && SignInSignUp_UserFlow.HasText() && !URLValidatorAttribute.AbsoluteUrlRegex.IsMatch(SignInSignUp_UserFlow))
                return ValidationMessage._0DoesNotHaveAValid1Format.NiceToString(pi.NiceName(), "URL");
        }

   
        return validator.Validate(this, pi) ?? base.PropertyValidation(pi);
    }

    static StateValidator<AzureADConfigurationEmbedded, AzureADType> validator = new (
       m => m.Type,    m => m.TenantName, m => m.SignInSignUp_UserFlow, m => m.SignIn_UserFlow, m => m.SignUp_UserFlow, m => m.EditProfile_UserFlow, m => m.ResetPassword_UserFlow)
        {
{AzureADType.AzureAD,    false,              false,                        false,                 false,                      false,                        false  },
{AzureADType.B2C,        true,               null, /*<--- either --->*/    null,                  null,                       null,                         null  },
{AzureADType.ExternalID, true,               true,                        false,                  false,                      false,                        false  },
        };

    public AzureADConfigTS? ToAzureADConfigTS(string[]? scopes) => !Enabled ? null : new AzureADConfigTS
    {
        Type = Type,
        ApplicationId = ApplicationID.ToString(),
        TenantId = DirectoryID.ToString(),
        TenantName = TenantName,
        SignInSignUp_UserFlow = SignInSignUp_UserFlow,
        SignIn_UserFlow = SignIn_UserFlow,
        SignUp_UserFlow = SignUp_UserFlow,
        EditProfile_UserFlow = EditProfile_UserFlow,
        ResetPassword_UserFlow = ResetPassword_UserFlow,
        Scopes = scopes ?? this.DefaultScopes(),
    };

    public string[] DefaultScopes()
    {
        switch (this.Type)
        {
            case AzureADType.AzureAD: return ["user.read"];
            case AzureADType.B2C: return ["openid", "profile", "email"];
            case AzureADType.ExternalID: return ["user.read"];
            default: throw new UnexpectedValueException(this.Type);
        }
    }

    internal string DefaultSignIn()
    {
        return SignInSignUp_UserFlow.DefaultText(SignIn_UserFlow!);
    }

    public string GetDiscoveryEndpoint() => Type switch
    {
        AzureADType.AzureAD => "https://login.microsoftonline.com/common/v2.0/.well-known/openid-configuration",
        AzureADType.B2C => $"https://{TenantName}.b2clogin.com/{TenantName}.onmicrosoft.com/{DefaultSignIn()}/v2.0/.well-known/openid-configuration?p={DefaultSignIn()}",
        AzureADType.ExternalID => SignInSignUp_UserFlow + "/v2.0/.well-known/openid-configuration?appid=" + ApplicationID,
        _ => throw new UnexpectedValueException(Type),
    };
};

public class AzureADConfigTS
{
    public AzureADType Type;

    public string ApplicationId;
    public string TenantId;

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public string? TenantName;
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public string? SignInSignUp_UserFlow;
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public string? SignIn_UserFlow;
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public string? SignUp_UserFlow;
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public string? ResetPassword_UserFlow;
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public string? EditProfile_UserFlow;

    public string[] Scopes; 
}

public enum AzureADType
{
    AzureAD,
    //https://learn.microsoft.com/en-us/azure/active-directory-b2c/configure-authentication-sample-spa-app
    B2C,
    //https://learn.microsoft.com/en-us/entra/external-id/customers/overview-customers-ciam
    ExternalID
}
