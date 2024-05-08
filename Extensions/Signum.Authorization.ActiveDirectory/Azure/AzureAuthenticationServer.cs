using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Signum.Authorization.ActiveDirectory.Azure;


public class AzureADAuthenticationServer
{
    public static bool LoginAzureADAuthentication(ActionContext ac, LoginWithAzureADRequest request, bool throwErrors)
    {
        using (AuthLogic.Disable())
        {
            try
            {
                var ada = (ActiveDirectoryAuthorizer)AuthLogic.Authorizer!;

                var config = ada.GetConfig();

                if (!config.LoginWithAzureAD)
                    return false;

                var principal = ValidateToken(request.idToken, out var jwtSecurityToken);

                var ctx = config.AzureB2C != null ?
                    new AzureB2CClaimsAutoCreateUserContext(principal, request.accessToken) :
                    new AzureClaimsAutoCreateUserContext(principal, request.accessToken);

                UserEntity? user = Database.Query<UserEntity>().SingleOrDefault(a => a.Mixin<UserADMixin>().OID == ctx.OID);

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

    //https://stackoverflow.com/questions/39866513/how-to-validate-azure-ad-security-token
    public static ClaimsPrincipal ValidateToken(string jwt, out JwtSecurityToken jwtSecurityToken)
    {
        var ada = (ActiveDirectoryAuthorizer)AuthLogic.Authorizer!;
        var adaConfig = ada.GetConfig();

        string stsDiscoveryEndpoint =
            adaConfig.AzureB2C != null ?
            $"https://{adaConfig.AzureB2C.TenantName}.b2clogin.com/{adaConfig.AzureB2C.TenantName}.onmicrosoft.com/{adaConfig.AzureB2C.SignInSignUp_UserFlow}/v2.0/.well-known/openid-configuration?p={adaConfig.AzureB2C.SignInSignUp_UserFlow}" :
            "https://login.microsoftonline.com/common/v2.0/.well-known/openid-configuration";

        var configManager = new ConfigurationManager<OpenIdConnectConfiguration>(stsDiscoveryEndpoint, new OpenIdConnectConfigurationRetriever());
        OpenIdConnectConfiguration config = configManager.GetConfigurationAsync().Result;

        var issuer = adaConfig.AzureB2C != null ?
            config.Issuer: 
            $"https://login.microsoftonline.com/{adaConfig.Azure_DirectoryID}/v2.0";

        TokenValidationParameters validationParameters = new TokenValidationParameters
        {
            ValidAudience = ada.GetConfig().Azure_ApplicationID.ToString(),
            ValidIssuer = issuer,

            ValidateAudience = true,
            ValidateIssuer = true,
            IssuerSigningKeys = config.SigningKeys, //2. .NET Core equivalent is "IssuerSigningKeys" and "SigningKeys"
            ValidateLifetime = true,
        };
        JwtSecurityTokenHandler tokendHandler = new JwtSecurityTokenHandler();

        var result = tokendHandler.ValidateToken(jwt, validationParameters, out SecurityToken secutityToken);

        jwtSecurityToken = (JwtSecurityToken)secutityToken;
        return result;
    }

}

