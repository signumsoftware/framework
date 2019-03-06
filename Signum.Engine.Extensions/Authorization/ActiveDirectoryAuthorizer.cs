using Signum.Engine.Operations;
using Signum.Entities.Authorization;
using Signum.Services;
using Signum.Utilities;
using System;
using System.DirectoryServices.AccountManagement;

namespace Signum.Engine.Authorization
{
    public class AutoCreateUserContext
    {
        public readonly PrincipalContext PrincipalContext;
        public readonly string UserName;
        public readonly string DomainName;

        UserPrincipal userPrincipal;

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
        Func<ActiveDirectoryConfigurationEmbedded> GetConfig;

        public Func<AutoCreateUserContext, UserEntity> AutoCreateUser;  

        public ActiveDirectoryAuthorizer(Func<ActiveDirectoryConfigurationEmbedded> getConfig, Func<AutoCreateUserContext, UserEntity> autoCreateUser = null)
        {
            this.GetConfig = getConfig;
            this.AutoCreateUser = autoCreateUser;
        }

        public UserEntity Login(string userName, string password)
        {
            using (AuthLogic.Disable())
            {
                var config = this.GetConfig();
                var domainName = userName.TryAfterLast('@');
                var localName = userName.TryBeforeLast('@') ?? userName;

                UserEntity user;

                if (config.DomainName.HasText() && (domainName == null || config.DomainName.ToLower() == domainName?.ToLower()))
                {
                    try
                    {
                        using (PrincipalContext pc = new PrincipalContext(ContextType.Domain, config.DomainName))
                        {
                            if (pc.ValidateCredentials(localName + "@" + config.DomainName, password))
                            {
                                user = AuthLogic.RetrieveUser(userName);

                                if (user == null)
                                {
                                    if (this.AutoCreateUser != null)
                                    {
                                        user = this.AutoCreateUser(new AutoCreateUserContext(pc, localName, domainName));
                                        if(user != null && user.IsNew)
                                        {
                                            using (ExecutionMode.Global())
                                            using (OperationLogic.AllowSave<UserEntity>())
                                            {
                                                user.Save();
                                            }
                                        }
                                    }
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
    }
}
