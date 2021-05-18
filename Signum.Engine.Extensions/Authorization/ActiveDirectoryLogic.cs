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
    public static class ActiveDirectoryLogic
    {
        public static PrincipalContext GetPrincipalContext()
        {
            var config = ((ActiveDirectoryAuthorizer)AuthLogic.Authorizer!).GetConfig();

            if (config.DirectoryRegistry_Username.HasText() && config.DirectoryRegistry_Password.HasText())
            {
                return new PrincipalContext(ContextType.Domain, config.DomainName, config.DirectoryRegistry_Username + "@" + config.DomainName, config.DirectoryRegistry_Password!);
            }
            else
            {
                throw new InvalidOperationException();
            }
        }

        public static  async Task<List<ActiveDirectoryUser>> SearchUser(string searchUserName)
        {
            var pc = GetPrincipalContext();
            PrincipalSearchResult<Principal> result;

            var principal = new UserPrincipal(pc)
            {
                SamAccountName = searchUserName + "*"
            };

            var searcher = new PrincipalSearcher(principal);


            result  = searcher.FindAll(); 
           

            return await Task.Run(() =>  result.Select(a => new ActiveDirectoryUser
            {
                UPN = a.UserPrincipalName,
                DisplayName = a.DisplayName,
                JobTitle = a.Description,
                ObjectID = (Guid)a.Guid!,
            }).ToList());


        }
    }
}
