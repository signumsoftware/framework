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
using Azure.Core;
using Azure.Identity;

namespace Signum.Authorization.ActiveDirectory;

public static class AuthenticationProviderUtils
{
    public static AsyncThreadVariable<TokenCredential?> OverridenTokenCredential = Statics.ThreadVariable<TokenCredential?>("OverrideAuthenticationProvider");


    public static IDisposable OverrideAuthenticationProvider(TokenCredential value)
    {
        var old = OverridenTokenCredential.Value;
        OverridenTokenCredential.Value = value;
        return new Disposable(() => OverridenTokenCredential.Value = old);
    }

    public static TokenCredential GetTokenCredential(this ActiveDirectoryConfigurationEmbedded activeDirectoryConfig, string[]? scopes = null)
    {
        if (OverridenTokenCredential.Value is var ap && ap != null)
            return ap;

        ClientSecretCredential result = new ClientSecretCredential(
            tenantId: activeDirectoryConfig.Azure_DirectoryID.ToString(),
            clientId: activeDirectoryConfig.Azure_ApplicationID.ToString(),
            clientSecret: activeDirectoryConfig.Azure_ClientSecret);

        return result;
    }

    public static IDisposable OverrideAuthenticationProvider(string accessToken) =>
        OverrideAuthenticationProvider(new AccessTokenCredential(accessToken));
}

public class AccessTokenCredential : TokenCredential
{
    private string accessToken;

    public override AccessToken GetToken(TokenRequestContext requestContext, CancellationToken cancellationToken)
    {
        return new AccessToken(accessToken, default);
    }

    public override ValueTask<AccessToken> GetTokenAsync(TokenRequestContext requestContext, CancellationToken cancellationToken)
    {
        return new ValueTask<AccessToken>(new AccessToken(accessToken, default));
    }

    public AccessTokenCredential(string accessToken)
    {
        this.accessToken = accessToken;
    }
}
