using Signum.Authorization.BaseAD;
using System.Text.Json.Serialization;

namespace Signum.Authorization.OpenID;

public class OpenIDConfigurationEmbedded : BaseADConfigurationEmbedded
{
    public bool Enabled { get; set; }

    [URLValidator, StringLengthValidator(Max = 300)]
    public string? Authority { get; set; }

    [StringLengthValidator(Max = 200)]
    public string? ClientId { get; set; }

    [StringLengthValidator(Max = 300)]
    public string? ClientSecret { get; set; }

    [StringLengthValidator(Max = 200)]
    public string? RoleClaimPath { get; set; }

    [StringLengthValidator(Max = 500)]
    public string? Scopes { get; set; }

    protected override string? PropertyValidation(PropertyInfo pi)
    {
        if (!Enabled)
            return null;

        if (pi.Name == nameof(Authority) && !Authority.HasText())
            return ValidationMessage._0IsNotSet.NiceToString(pi.NiceName());

        if (pi.Name == nameof(ClientId) && !ClientId.HasText())
            return ValidationMessage._0IsNotSet.NiceToString(pi.NiceName());

        return base.PropertyValidation(pi);
    }

    public string GetDiscoveryEndpoint() => $"{Authority!.TrimEnd('/')}/.well-known/openid-configuration";

    public string[] GetScopes() =>
        Scopes.HasText() ? Scopes.Split(' ', StringSplitOptions.RemoveEmptyEntries) : ["openid", "profile", "email"];

    public OpenIDConfigTS? ToOpenIDConfigTS() => !Enabled ? null : new OpenIDConfigTS
    {
        Authority = Authority!,
        ClientId = ClientId!,
        Scopes = GetScopes(),
    };
}

public class OpenIDConfigTS
{
    public string Authority;
    public string ClientId;
    public string[] Scopes;
}

[AllowUnauthenticated]
public enum OpenIDMessage
{
    [Description("Sign in with OpenID")]
    SignInWithOpenID,
}
