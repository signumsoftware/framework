using Signum.Engine.Authorization;
using Signum.Entities;
using Signum.Entities.Authorization;
using Signum.Services;
using Signum.Utilities;
using System;
using System.Collections.Generic;
using System.DirectoryServices.AccountManagement;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Signum.Engine.Authorization
{
    public class ActiveDirectoryAuthorizer : ICustomAuthorizer
    {
        Func<ActiveDirectoryConfigurationEmbedded> GetConfig;

        public Func<PrincipalContext, UserEntity> AutoCreateUser;  //https://stackoverflow.com/questions/14278274/how-i-get-active-directory-user-properties-with-system-directoryservices-account

        public ActiveDirectoryAuthorizer(Func<ActiveDirectoryConfigurationEmbedded> getConfig, Func<PrincipalContext, UserEntity> autoCreateUser = null)
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
                                        user = this.AutoCreateUser(pc);
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
