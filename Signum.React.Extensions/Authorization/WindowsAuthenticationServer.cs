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

namespace Signum.React.Authorization
{
    public class WindowsAuthenticationServer
    {
        public static Func<WindowsPrincipal, UserEntity?>? AutoCreateUser = DefaultAutoCreateUser;

        public static UserEntity? DefaultAutoCreateUser(WindowsPrincipal wp)
        {
            if (!(AuthLogic.Authorizer is ActiveDirectoryAuthorizer ada))
                return null;

            var config = ada.GetConfig();
            var userName = wp.Identity.Name;
            var domainName = userName.TryAfterLast('@') ?? userName.TryBefore('\\') ?? config.DomainName;
            var localName = userName.TryBeforeLast('@') ?? userName.TryAfter('\\') ?? userName;

            if (domainName != null && domainName == config.DomainName)
            {
                UserEntity? user = AuthLogic.RetrieveUser(localName);
                if (user != null)
                    return user;
            }

            if (!config.AutoCreateUsers)
                return null;

            using (PrincipalContext pc = new PrincipalContext(ContextType.Domain, domainName))
            {
                var user = ada.OnAutoCreateUser(pc, domainName!, localName);
                return user;
            }
        }

        public static bool LoginWindowsAuthentication(ActionContext ac)
        {
            using (AuthLogic.Disable())
            {
                try
                {
                    if (!(ac.HttpContext.User is WindowsPrincipal wp))
                        return false;

                    if (AuthLogic.Authorizer is ActiveDirectoryAuthorizer ada && !ada.GetConfig().LoginWithWindowsAuthenticator)
                        return false;

                    UserEntity? user = AuthLogic.RetrieveUser(wp.Identity.Name);

                    if (user == null)
                    {
                        if (AutoCreateUser == null)
                            return false;

                        user = AutoCreateUser(wp);

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
    }
  
}
