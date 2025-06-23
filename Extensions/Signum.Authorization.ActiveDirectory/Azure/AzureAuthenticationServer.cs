using Microsoft.AspNetCore.Mvc;
using Microsoft.Graph.Me.ExportDeviceAndAppManagementData;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Signum.Authorization.ActiveDirectory.Azure;


public class AzureADAuthenticationServer
{
    public static bool LoginAzureADAuthentication(ActionContext ac, LoginWithAzureADRequest request, bool azureB2C, bool throwErrors)
    {
        using (AuthLogic.Disable())
        {
            try
            {
                var ada = (ActiveDirectoryAuthorizer)AuthLogic.Authorizer!;

                var config = ada.GetConfig();
                var azureAD = config.AzureAD;

                if (azureAD == null)
                    return false;

                AzureClaimsAutoCreateUserContext ctx;
                if (azureB2C)
                {
                    if (azureAD.AzureB2C?.LoginWithAzureB2C != true)
                        return false;

                    var principal = ValidateToken(request.idToken, azureB2C, out var jwtSecurityToken);

                    ctx = new AzureB2CClaimsAutoCreateUserContext(principal, request.accessToken);
                }
                else
                {
                    if (azureAD.LoginWithAzureAD != true)
                        return false;

                    var principal = ValidateToken(request.idToken, azureB2C, out var jwtSecurityToken);

                    ctx = new AzureClaimsAutoCreateUserContext(principal, request.accessToken);
                }

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

    public static Func<IEnumerable<string>>? ExtraValidAudiences;

    //https://stackoverflow.com/questions/39866513/how-to-validate-azure-ad-security-token
    public static ClaimsPrincipal ValidateToken(string jwt, bool azureB2C, out JwtSecurityToken jwtSecurityToken)
    {
        var ada = (ActiveDirectoryAuthorizer)AuthLogic.Authorizer!;
        var azureAD = ada.GetConfig().AzureAD!;

        string stsDiscoveryEndpoint = !azureB2C ? "https://login.microsoftonline.com/common/v2.0/.well-known/openid-configuration" :
            $"https://{azureAD.AzureB2C!.TenantName}.b2clogin.com/{azureAD.AzureB2C.TenantName}.onmicrosoft.com/{azureAD.AzureB2C.GetDefaultSignInFlow()}/v2.0/.well-known/openid-configuration?p={azureAD.AzureB2C.GetDefaultSignInFlow()}";

        var configManager = new ConfigurationManager<OpenIdConnectConfiguration>(stsDiscoveryEndpoint, new OpenIdConnectConfigurationRetriever());
        OpenIdConnectConfiguration config = configManager.GetConfigurationAsync().Result;

        var issuer = !azureB2C ? $"https://login.microsoftonline.com/{azureAD.DirectoryID}/v2.0" :
            config.Issuer;

        TokenValidationParameters validationParameters = new TokenValidationParameters
        {
            ValidAudience = azureAD.ApplicationID.ToString(),
            ValidAudiences = ExtraValidAudiences?.Invoke(),
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

