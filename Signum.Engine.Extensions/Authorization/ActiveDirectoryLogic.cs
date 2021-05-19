using Signum.Entities.Authorization;
using Signum.Utilities;
using System;
using System.Collections.Generic;
using System.DirectoryServices.AccountManagement;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Signum.Engine.Authorization
{
#pragma warning disable CA1416 // Validate platform compatibility
    public static class ActiveDirectoryLogic
    {
        static PrincipalContext GetPrincipalContext()
        {
            var config = ((ActiveDirectoryAuthorizer)AuthLogic.Authorizer!).GetConfig();

            if (config.DirectoryRegistry_Username.HasText() && config.DirectoryRegistry_Password.HasText())
                return new PrincipalContext(ContextType.Domain, config.DomainName, config.DirectoryRegistry_Username + "@" + config.DomainName, config.DirectoryRegistry_Password!);
            else
                return new PrincipalContext(ContextType.Domain, config.DomainName);
        }

        public static Task<List<ActiveDirectoryUser>> SearchUser(string searchUserName)
        {
            using (var pc = GetPrincipalContext())
            {
                var principal = new UserPrincipal(pc)
                {
                    SamAccountName = searchUserName + "*"
                };

                var searcher = new PrincipalSearcher(principal);

                var result = searcher.FindAll().Select(a => new ActiveDirectoryUser
                {
                    UPN = a.UserPrincipalName,
                    DisplayName = a.DisplayName,
                    JobTitle = a.Description,
                    ObjectID = (Guid)a.Guid!,
                }).ToList();

                return Task.FromResult(result);
            }
        }
    }
}
