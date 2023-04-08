using Microsoft.Graph;
using Microsoft.Graph.Auth;
using Microsoft.Identity.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Signum.ActiveDirectory;
public static class AuthenticationProviderUtils
{
    public static AsyncThreadVariable<IAuthenticationProvider?> AuthenticationProvider = Statics.ThreadVariable<IAuthenticationProvider?>("OverrideAuthenticationProvider");

    public static IDisposable OverrideAuthenticationProvider(IAuthenticationProvider value)
    {
        var old = AuthenticationProvider.Value;
        AuthenticationProvider.Value = value;
        return new Disposable(() => AuthenticationProvider.Value = old);
    }

    public static IAuthenticationProvider GetAuthProvider(this ActiveDirectoryConfigurationEmbedded activeDirectoryConfig, string[]? scopes = null)
    {
        if (AuthenticationProvider.Value is var ap && ap != null)
            return ap;

        IConfidentialClientApplication confidentialClientApplication = ConfidentialClientApplicationBuilder
        .Create(activeDirectoryConfig.Azure_ApplicationID.ToString())
        .WithTenantId(activeDirectoryConfig.Azure_DirectoryID.ToString())
        .WithClientSecret(activeDirectoryConfig.Azure_ClientSecret)
        .Build();

        var authResultDirect = confidentialClientApplication.AcquireTokenForClient(scopes ?? new string[] { "https://graph.microsoft.com/.default" }).ExecuteAsync().Result;

        ClientCredentialProvider authProvider = new ClientCredentialProvider(confidentialClientApplication);
        return authProvider;
    }

    public static IDisposable OverrideAuthenticationProvider(string accessToken) =>
        OverrideAuthenticationProvider(new AccessTokenProvider(accessToken));
}

public class AccessTokenProvider : IAuthenticationProvider
{
    private string accessToken;

    public AccessTokenProvider(string accessToken)
    {
        this.accessToken = accessToken;
    }

    public Task AuthenticateRequestAsync(HttpRequestMessage request)
    {
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        return Task.CompletedTask;
    }
}
