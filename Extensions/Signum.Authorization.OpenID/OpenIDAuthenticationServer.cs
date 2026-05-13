using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Signum.Authorization.OpenID.Authorizer;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Signum.Authorization.OpenID;

public class OpenIDAuthenticationServer
{
    static readonly HttpClient HttpClient = new HttpClient();

    public static bool LoginOpenIDAuthentication(ActionContext ac, LoginWithOpenIDRequest request, bool throwErrors)
    {
        using (AuthLogic.Disable())
        {
            try
            {
                var authorizer = (OpenIDAuthorizer)AuthLogic.Authorizer!;
                var config = authorizer.GetConfig();
                if (config == null || !config.Enabled)
                    return false;

                var tokenResponse = ExchangeCodeForTokens(request.Code, request.RedirectUri, config);
                var principal = ValidateToken(tokenResponse.IdToken!, config, out _);
                var ctx = new OpenIDClaimsAutoCreateUserContext(principal, tokenResponse.AccessToken ?? "", config);

                UserEntity? user = Database.Query<UserEntity>().SingleOrDefault(a => a.ExternalId == ctx.ExternalId);

                if (user == null)
                {
                    user = Database.Query<UserEntity>().SingleOrDefault(a => a.UserName == ctx.UserName) ??
                        (ctx.UserName.Contains("@") && config.AllowMatchUsersBySimpleUserName
                            ? Database.Query<UserEntity>().SingleOrDefault(a => a.Email == ctx.UserName || a.UserName == ctx.UserName.Before("@"))
                            : null);
                }

                if (user == null)
                {
                    if (!config.AutoCreateUsers)
                        return false;

                    user = authorizer.OnCreateUser(ctx);
                }
                else
                {
                    if (user.State == UserState.Deactivated)
                        return throwErrors ? throw new InvalidOperationException(LoginAuthMessage.User0IsDeactivated.NiceToString(user)) : false;

                    if (config.AutoUpdateUsers)
                        authorizer.UpdateUser(user, ctx);
                }

                if (user.State == UserState.Deactivated)
                    return throwErrors ? throw new InvalidOperationException(LoginAuthMessage.User0IsDeactivated.NiceToString(user)) : false;

                AuthServer.OnUserPreLogin(ac, user);
                AuthServer.AddUserSession(ac, user);
                return true;
            }
            catch (Exception ex)
            {
                ex.LogException();
                if (throwErrors)
                    throw;

                return false;
            }
        }
    }

    static OpenIDTokenResponse ExchangeCodeForTokens(string code, string redirectUri, OpenIDConfigurationEmbedded config)
    {
        var discoveryDoc = GetDiscoveryDocument(config);

        var body = new FormUrlEncodedContent(
        [
            new KeyValuePair<string, string>("grant_type", "authorization_code"),
            new KeyValuePair<string, string>("code", code),
            new KeyValuePair<string, string>("redirect_uri", redirectUri),
            new KeyValuePair<string, string>("client_id", config.ClientId!),
            new KeyValuePair<string, string>("client_secret", config.ClientSecret!),
        ]);

        var response = HttpClient.PostAsync(discoveryDoc.TokenEndpoint, body).GetAwaiter().GetResult();
        response.EnsureSuccessStatusCode();

        var json = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
        return JsonSerializer.Deserialize<OpenIDTokenResponse>(json)!;
    }

    static OpenIdConnectConfiguration GetDiscoveryDocument(OpenIDConfigurationEmbedded config)
    {
        var configManager = new ConfigurationManager<OpenIdConnectConfiguration>(
            config.GetDiscoveryEndpoint(),
            new OpenIdConnectConfigurationRetriever());
        return configManager.GetConfigurationAsync().Result;
    }

    public static ClaimsPrincipal ValidateToken(string jwt, OpenIDConfigurationEmbedded config, out JwtSecurityToken jwtSecurityToken)
    {
        var discoveryDoc = GetDiscoveryDocument(config);

        JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

        var validationParameters = new TokenValidationParameters
        {
            ValidAudience = config.ClientId,
            ValidIssuer = discoveryDoc.Issuer,
            ValidateAudience = true,
            ValidateIssuer = true,
            IssuerSigningKeys = discoveryDoc.SigningKeys,
            ValidateLifetime = true,
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var principal = tokenHandler.ValidateToken(jwt, validationParameters, out var securityToken);
        jwtSecurityToken = (JwtSecurityToken)securityToken;
        return principal;
    }
}

public class OpenIDTokenResponse
{
    [JsonPropertyName("access_token")]
    public string? AccessToken { get; set; }

    [JsonPropertyName("id_token")]
    public string? IdToken { get; set; }

    [JsonPropertyName("token_type")]
    public string? TokenType { get; set; }

    [JsonPropertyName("expires_in")]
    public int ExpiresIn { get; set; }

    [JsonPropertyName("refresh_token")]
    public string? RefreshToken { get; set; }
}
