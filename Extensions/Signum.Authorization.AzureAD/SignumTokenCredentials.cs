using Microsoft.Graph;
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
using Signum.Authorization.AzureAD.Authorizer;

namespace Signum.Authorization.AzureAD;

public static class SignumTokenCredentials
{
    public static AsyncThreadVariable<TokenCredential?> OverridenTokenCredential = Statics.ThreadVariable<TokenCredential?>("OverrideAuthenticationProvider");


    public static IDisposable OverrideAuthenticationProvider(TokenCredential value)
    {
        var old = OverridenTokenCredential.Value;
        OverridenTokenCredential.Value = value;
        return new Disposable(() => OverridenTokenCredential.Value = old);
    }

    public static TokenCredential GetAuthorizerTokenCredential()
    {
        if (OverridenTokenCredential.Value is var ap && ap != null)
            return ap;

        var config = AuthLogic.Authorizer is AzureADAuthorizer ada ? ada.GetConfig(null) :
            throw new InvalidOperationException("AuthLogic.Authorizer is not an ActiveDirectoryAuthorizer");

        var azureAD = config ?? throw new InvalidOperationException("AzureAD not set");

        ClientSecretCredential result = new ClientSecretCredential(
            tenantId: azureAD.DirectoryID.ToString(),
            clientId: azureAD.ApplicationID.ToString(),
            clientSecret: azureAD.ClientSecret);

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
