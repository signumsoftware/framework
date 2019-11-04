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
using Signum.Utilities.Reflection;
using Signum.Engine.Basics;

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
            var userName = wp.Identity.Name!;
            var domainName = config.DomainName.DefaultToNull() ?? userName.TryAfterLast('@') ?? userName.TryBefore('\\');
            var localName = userName.TryBeforeLast('@') ?? userName.TryAfter('\\') ?? userName;

            if(ada.GetConfig().AllowSimpleUserNames)
            {
                UserEntity? user = AuthLogic.RetrieveUser(localName);
                if (user != null)
                    return user;
            }

            if (!config.AutoCreateUsers)
                return null;
            try
            {
                using (PrincipalContext pc = new PrincipalContext(ContextType.Domain, domainName))
                {
                    var user = ada.OnAutoCreateUser(new DirectoryServiceAutoCreateUserContext(pc, localName, domainName!));
                    return user;
                }
            }
            catch (Exception e)
            {
                e.Data["Identity.Name"] = wp.Identity.Name;
                e.Data["domainName"] = domainName;
                e.Data["localName"] = localName;
                throw;
            }
        }

        public static string? LoginWindowsAuthentication(ActionContext ac)
        {
            using (AuthLogic.Disable())
            {
                try
                {
                    if (!(ac.HttpContext.User is WindowsPrincipal wp))
                        return $"User is not a WindowsPrincipal ({ac.HttpContext.User.GetType().Name})";

                    if (AuthLogic.Authorizer is ActiveDirectoryAuthorizer ada && !ada.GetConfig().LoginWithWindowsAuthenticator)
                        return $"{ReflectionTools.GetPropertyInfo(() => ada.GetConfig().LoginWithWindowsAuthenticator)} is set to false";

                    UserEntity? user = AuthLogic.RetrieveUser(wp.Identity.Name!);

                    if (user == null)
                    {
                        if (AutoCreateUser == null)
                            return "AutoCreateUser is null";

                        user = AutoCreateUser(wp);

                        if (user == null)
                            return "AutoCreateUser returned null";
                    }

                    AuthServer.OnUserPreLogin(ac, user);
                    AuthServer.AddUserSession(ac, user);
                    return null;
                }
                catch(Exception e)
                {
                    e.LogException();
                    return e.Message;
                }
            }
        }
    }
  
}
