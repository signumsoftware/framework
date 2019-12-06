using System;
using Signum.Engine.Authorization;
using Signum.Entities.Authorization;
using Signum.Utilities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Http;
using System.Security.Principal;
using System.Linq;
using System.DirectoryServices.AccountManagement;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Signum.Engine;
using Signum.Engine.Operations;

namespace Signum.React.Authorization
{

    public class AzureADAuthenticationServer
    {
        public static bool LoginAzureADAuthentication(ActionContext ac, string jwt)
        {
            using (AuthLogic.Disable())
            {
                try
                {
                    var ada = (ActiveDirectoryAuthorizer)AuthLogic.Authorizer!;

                    if (!ada.GetConfig().LoginWithAzureAD)
                        return false;

                    var principal = ValidateToken(jwt, out var jwtSecurityToken);
                    var ctx = new AzureClaimsAutoCreateUserContext(principal);

                    UserEntity? user =
                        Database.Query<UserEntity>().SingleOrDefault(a => a.Mixin<UserOIDMixin>().OID == ctx.OID);

                    if(user == null && ada.GetConfig().AllowSimpleUserNames)
                    {
                        user = Database.Query<UserEntity>().SingleOrDefault(a => a.UserName == ctx.UserName);

                        if (user != null && user.Mixin<UserOIDMixin>().OID == null)
                        {
                            user.Mixin<UserOIDMixin>().OID = ctx.OID;
                            using (AuthLogic.Disable())
                            using (OperationLogic.AllowSave<UserEntity>())
                            {
                                user.Save();
                            }
                        }
                    }

                    if (user == null)
                    {
                        user = ada.OnAutoCreateUser(ctx);

                        if (user == null)
                            return false;
                    }

                    AuthServer.OnUserPreLogin(ac, user);
                    AuthServer.AddUserSession(ac, user);
                    return true;
                }
                catch
                {
                    return false;
                }
            }
        }

        //https://stackoverflow.com/questions/39866513/how-to-validate-azure-ad-security-token
        public static ClaimsPrincipal ValidateToken(string jwt, out JwtSecurityToken jwtSecurityToken)
        {
            var ada = (ActiveDirectoryAuthorizer)AuthLogic.Authorizer!;

            string stsDiscoveryEndpoint = "https://login.microsoftonline.com/common/v2.0/.well-known/openid-configuration";

            var configManager = new ConfigurationManager<OpenIdConnectConfiguration>(stsDiscoveryEndpoint, new OpenIdConnectConfigurationRetriever());

            OpenIdConnectConfiguration config = configManager.GetConfigurationAsync().Result;
            TokenValidationParameters validationParameters = new TokenValidationParameters
            {
                ValidAudience = ada.GetConfig().Azure_ApplicationID,
                ValidIssuer = "https://login.microsoftonline.com/" + ada.GetConfig().Azure_DirectoryID + "/v2.0",

                ValidateAudience = true,
                ValidateIssuer = true,
                IssuerSigningKeys = config.SigningKeys, //2. .NET Core equivalent is "IssuerSigningKeys" and "SigningKeys"
                ValidateLifetime = true
            };
            JwtSecurityTokenHandler tokendHandler = new JwtSecurityTokenHandler();

            SecurityToken secutityToken;
            var result = tokendHandler.ValidateToken(jwt, validationParameters, out secutityToken);

            jwtSecurityToken = (JwtSecurityToken)secutityToken;
            return result;
        }

    }

}
