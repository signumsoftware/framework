namespace Signum.Authorization.BaseAD;

public static class TokenCredentialOverride
{
    public static Func<string, IDisposable?>? OverrideProvider { get; set; } // = SignumTokenCredentials.OverrideAuthenticationProvider;

    public static IDisposable? Override(string accessToken) => OverrideProvider?.Invoke(accessToken);
}
