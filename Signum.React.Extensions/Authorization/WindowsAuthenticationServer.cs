using Signum.Engine.Authorization;
using Signum.Entities.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Principal;
using System.DirectoryServices.AccountManagement;
using Signum.Utilities.Reflection;

namespace Signum.React.Authorization;

#pragma warning disable CA1416 // Validate platform compatibility

public class WindowsAuthenticationServer
{
    private static PrincipalContext GetPrincipalContext(string domainName, ActiveDirectoryConfigurationEmbedded config)
    {
        if (config.DirectoryRegistry_Username.HasText())
            return new PrincipalContext(ContextType.Domain, domainName, config.DirectoryRegistry_Username + "@" + config.DomainServer, config.DirectoryRegistry_Password);

        if (config.DomainServer.HasText())
            return new PrincipalContext(ContextType.Domain, config.DomainServer);
        
        return new PrincipalContext(ContextType.Domain, domainName); //Uses current user
    }

    public static string? LoginWindowsAuthentication(ActionContext ac)
    {
        using (AuthLogic.Disable())
        {
            try
            {
                if (!(ac.HttpContext.User is WindowsPrincipal wp))
                    return $"User is not a WindowsPrincipal ({ac.HttpContext.User.GetType().Name})";

                if (AuthLogic.Authorizer is not ActiveDirectoryAuthorizer ada)
                    return "No AuthLogic.Authorizer set";

                var config = ada.GetConfig();

                if (!config.LoginWithWindowsAuthenticator)
                    return $"{ReflectionTools.GetPropertyInfo(() => ada.GetConfig().LoginWithWindowsAuthenticator)} is set to false";

                var userName = wp.Identity.Name!;
                var domainName = config.DomainName.DefaultToNull() ?? userName.TryAfterLast('@') ?? userName.TryBefore('\\')!;
                var localName = userName.TryBeforeLast('@') ?? userName.TryAfter('\\') ?? userName;


                var sid = ((WindowsIdentity)wp.Identity).User!.Value;

                UserEntity? user = Database.Query<UserEntity>().SingleOrDefaultEx(a => a.Mixin<UserADMixin>().SID == sid);

                if (user == null)
                {
                    user = Database.Query<UserEntity>().SingleOrDefault(a => a.UserName == userName) ??
                    (config.AllowMatchUsersBySimpleUserName ? Database.Query<UserEntity>().SingleOrDefault(a => a.Email == userName || a.UserName == localName) : null);
                }

                try
                {
                    if (user == null)
                    {
                        if (!config.AutoCreateUsers)
                            return null;

                        using (PrincipalContext pc = GetPrincipalContext(domainName, config))
                        {
                            user = Database.Query<UserEntity>().SingleOrDefaultEx(a => a.Mixin<UserADMixin>().SID == sid);

                            if (user == null)
                            {
                                if (ada.GetConfig().AutoCreateUsers)
                                    user = ada.OnCreateUser(new DirectoryServiceAutoCreateUserContext(pc, localName, domainName!));
                            }
                        }
                    }
                    else
                    {
                        if (config.AutoUpdateUsers)
                        {
                            using (PrincipalContext pc = GetPrincipalContext(domainName, config))
                            {
                                ada.UpdateUser(user, new DirectoryServiceAutoCreateUserContext(pc, localName, domainName!));
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    e.Data["Identity.Name"] = wp.Identity.Name;
                    e.Data["domainName"] = domainName;
                    e.Data["localName"] = localName;
                    throw;
                }

                if (user == null)
                {
                    if (user == null)
                        return "AutoCreateUsers is false";
                }


                AuthServer.OnUserPreLogin(ac, user);
                AuthServer.AddUserSession(ac, user);
                return null;
            }
            catch (Exception e)
            {
                e.LogException();
                return e.Message;
            }
        }
    }
}
  
