using Microsoft.AspNetCore.Mvc;
using System.Security.Principal;
using System.DirectoryServices.AccountManagement;
using Signum.Utilities.Reflection;
using Signum.Authorization;

namespace Signum.ActiveDirectory;

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

    public static bool LoginWindowsAuthentication(ActionContext ac, bool throwError)
    {
        using (AuthLogic.Disable())
        {
            try
            {
                if (!(ac.HttpContext.User is WindowsPrincipal wp))
                    return throwError ? throw new InvalidOperationException($"User is not a WindowsPrincipal ({ac.HttpContext.User.GetType().Name})")
                        : false;

                if (AuthLogic.Authorizer is not ActiveDirectoryAuthorizer ada)
                    return throwError ? throw new InvalidOperationException("No AuthLogic.Authorizer set") 
                        : false;

                var config = ada.GetConfig();

                if (!config.LoginWithWindowsAuthenticator)
                    return throwError ? throw new Exception($"{ReflectionTools.GetPropertyInfo(() => ada.GetConfig().LoginWithWindowsAuthenticator)} is set to false")
                        : false;

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
                        {
                            return throwError ? throw new InvalidOperationException("AutoCreateUsers is false") 
                                : false;
                        }

                        using (PrincipalContext pc = GetPrincipalContext(domainName, config))
                        {
                            user = Database.Query<UserEntity>().SingleOrDefaultEx(a => a.Mixin<UserADMixin>().SID == sid);

                            if (user == null)
                            {
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

                AuthServer.OnUserPreLogin(ac, user);
                AuthServer.AddUserSession(ac, user);
                return true;
            }
            catch (Exception e)
            {
                e.LogException();

                if (throwError)
                    throw;

                return false;
            }
        }
    }
}
  
