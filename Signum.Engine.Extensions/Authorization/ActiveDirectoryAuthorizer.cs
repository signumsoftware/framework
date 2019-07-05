using Signum.Engine.Operations;
using Signum.Entities;
using Signum.Entities.Authorization;
using Signum.Services;
using Signum.Utilities;
using System;
using System.DirectoryServices.AccountManagement;
using System.Linq;

namespace Signum.Engine.Authorization
{
    public class AutoCreateUserContext
    {
        public readonly PrincipalContext PrincipalContext;
        public readonly string UserName;
        public readonly string DomainName;

        UserPrincipal? userPrincipal;

        public AutoCreateUserContext(PrincipalContext principalContext, string userName, string domainName)
        {
            PrincipalContext = principalContext;
            UserName = userName;
            DomainName = domainName;
        }

        public UserPrincipal GetUserPrincipal() //https://stackoverflow.com/questions/14278274/how-i-get-active-directory-user-properties-with-system-directoryservices-account
        {
            return userPrincipal ?? (userPrincipal = UserPrincipal.FindByIdentity(PrincipalContext, UserName));
        }
    }

    public class ActiveDirectoryAuthorizer : ICustomAuthorizer
    {
        public Func<ActiveDirectoryConfigurationEmbedded> GetConfig;

        public ActiveDirectoryAuthorizer(Func<ActiveDirectoryConfigurationEmbedded> getConfig)
        {
            this.GetConfig = getConfig;
        }

        public virtual UserEntity Login(string userName, string password)
        {
            using (AuthLogic.Disable())
            {
                var config = this.GetConfig();
                var domainName = userName.TryAfterLast('@') ?? userName.TryBefore('\\') ?? config.DomainName;
                var localName = userName.TryBeforeLast('@') ?? userName.TryAfter('\\') ?? userName;
                
                UserEntity? user;

                if (domainName != null && config.LoginWithActiveDirectoryRegistry)
                {
                    try
                    {
                        using (PrincipalContext pc = new PrincipalContext(ContextType.Domain, domainName))
                        {
                            if (pc.ValidateCredentials(localName + "@" + domainName, password))
                            {
                                user = AuthLogic.RetrieveUser(userName);

                                if (user == null)
                                {
                                    user = OnAutoCreateUser(pc, domainName, localName);
                                }

                                if (user != null)
                                {
                                    AuthLogic.OnUserLogingIn(user);
                                    return user;
                                }
                                else
                                {
                                    throw new InvalidOperationException(ActiveDirectoryAuthorizerMessage.ActiveDirectoryUser0IsNotAssociatedWithAUserInThisApplication.NiceToString(localName));
                                }
                            }
                        }
                    }
                    catch (PrincipalServerDownException)
                    {
                        // Do nothing for this kind of Active Directory exception
                    }
                }

                user = AuthLogic.Login(userName, Security.EncodePassword(password));

                return user;
            }
        }


        public UserEntity? OnAutoCreateUser(PrincipalContext pc, string domainName, string localName)
        {
            if (!GetConfig().AutoCreateUsers)
                return null;

            var user = this.AutoCreateUserInternal(new AutoCreateUserContext(pc, localName, domainName!));
            if (user != null && user.IsNew)
            {
                using (ExecutionMode.Global())
                using (OperationLogic.AllowSave<UserEntity>())
                {
                    user.Save();
                }
            }

            return user;
        }

        public virtual UserEntity? AutoCreateUserInternal(AutoCreateUserContext ctx)
        {
            return new UserEntity
            {
                UserName = ctx.UserName,
                PasswordHash = Security.EncodePassword(Guid.NewGuid().ToString()),
                Email = ctx.GetUserPrincipal().EmailAddress,
                Role = GetRole(ctx, throwIfNull: true)!,
                State = UserState.Saved,
            };
        }

        public virtual Lite<RoleEntity>? GetRole(AutoCreateUserContext ctx, bool throwIfNull)
        {
            var groups = ctx.GetUserPrincipal().GetGroups();
            var config = GetConfig();
            var role = config.RoleMapping.FirstOrDefault(m =>
            {
                Guid.TryParse(m.ADNameOrGuid, out var guid);
                return groups.Any(g => g.Name == m.ADNameOrGuid || g.Guid == guid);
            })?.Role ?? config.DefaultRole;

            if (role == null && throwIfNull)
                throw new InvalidOperationException("No matching RoleMapping found for any role: \r\n" + groups.ToString(a => a.Name, "\r\n"));

            return role;
        }
    }
}
