using Microsoft.AspNetCore.Mvc;
using Microsoft.Graph.Me.ExportDeviceAndAppManagementData;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Signum.Authorization.AzureAD.Authorizer;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Signum.Authorization.AzureAD;


public class AzureADAuthenticationServer
{
    public static bool LoginAzureADAuthentication(ActionContext ac, LoginWithAzureADRequest request, string adVariant, bool throwErrors)
    {
        using (AuthLogic.Disable())
        {
            try
            {
                var ada = (AzureADAuthorizer)AuthLogic.Authorizer!;
                var config = ada.GetConfig(adVariant);
                if (config == null || !config.Enabled)
                    return false;

                var principal = ValidateToken(request.idToken, config, out var jwtSecurityToken);
                AzureClaimsAutoCreateUserContext ctx = config.Type switch
                {
                    AzureADType.AzureAD => new AzureClaimsAutoCreateUserContext(principal, request.accessToken, config),
                    AzureADType.B2C => new AzureB2CClaimsAutoCreateUserContext(principal, request.accessToken, config),
                    AzureADType.ExternalID => new AzureExternalIDAutoCreateUserContext(principal, request.accessToken, config),
                    _ => throw new UnexpectedValueException($"Unexpected AzureADType {config.Type}")
                };

                UserEntity? user = Database.Query<UserEntity>().SingleOrDefault(a => a.Mixin<UserAzureADMixin>().OID == ctx.OID);

                if (user == null)
                {
                    user = Database.Query<UserEntity>().SingleOrDefault(a => a.UserName == ctx.UserName) ??
                    (ctx.UserName.Contains("@") && config.AllowMatchUsersBySimpleUserName ? Database.Query<UserEntity>().SingleOrDefault(a => a.Email == ctx.UserName || a.UserName == ctx.UserName.Before("@")) : null);
                }

                if (user == null)
                {
                    if (!config.AutoCreateUsers)
                        return false;

                    user = ada.OnCreateUser(ctx);
                }
                else
                {
                    if (user.State == UserState.Deactivated)
                        return throwErrors ? throw new InvalidOperationException(LoginAuthMessage.User0IsDeactivated.NiceToString(user)) : false;

                    if (config.AutoUpdateUsers)
                        ada.UpdateUser(user, ctx);
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

    public static Func<IEnumerable<string>>? ExtraValidAudiences;

    //https://stackoverflow.com/questions/39866513/how-to-validate-azure-ad-security-token
    public static ClaimsPrincipal ValidateToken(string jwt, AzureADConfigurationEmbedded config, out JwtSecurityToken jwtSecurityToken)
    {
        string stsDiscoveryEndpoint = config.GetDiscoveryEndpoint();

        var configManager = new ConfigurationManager<OpenIdConnectConfiguration>(stsDiscoveryEndpoint, new OpenIdConnectConfigurationRetriever());
        OpenIdConnectConfiguration c = configManager.GetConfigurationAsync().Result;

        var issuer = config.Type == AzureADType.AzureAD? $"https://login.microsoftonline.com/{config!.DirectoryID}/v2.0" : c.Issuer;

        TokenValidationParameters validationParameters = new TokenValidationParameters
        {
            ValidAudience = config!.ApplicationID.ToString(),
            ValidAudiences = ExtraValidAudiences?.Invoke(),
            ValidIssuer = issuer,

            ValidateAudience = true,
            ValidateIssuer = true,
            IssuerSigningKeys = c.SigningKeys, //2. .NET Core equivalent is "IssuerSigningKeys" and "SigningKeys"
            ValidateLifetime = true,
        };
        JwtSecurityTokenHandler tokendHandler = new JwtSecurityTokenHandler();

        var result = tokendHandler.ValidateToken(jwt, validationParameters, out SecurityToken secutityToken);

        jwtSecurityToken = (JwtSecurityToken)secutityToken;
        return result;
    }

  
}

