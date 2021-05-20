using Signum.Engine.Operations;
using Signum.Entities;
using Signum.Entities.Authorization;
using Signum.Services;
using Signum.Utilities;
using Signum.Utilities.Reflection;
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
                List<UserPrincipal> searchPrinciples = new List<UserPrincipal>();

                searchPrinciples.Add(new UserPrincipal(pc)
                {
                    SamAccountName = "*" + searchUserName + "*",
                });

                searchPrinciples.Add(new UserPrincipal(pc)
                {
                    DisplayName = "*" + searchUserName + "*",
                });


                if (searchUserName.Contains("@"))
                {
                    searchPrinciples.Add(new UserPrincipal(pc)
                    {
                        EmailAddress = searchUserName,
                    });
                }

                List<Principal> principals = new List<Principal>();
                var searcher = new PrincipalSearcher();

                foreach (var item in searchPrinciples)
                {
                    searcher = new PrincipalSearcher(item);
                    principals.AddRange(searcher.FindAll());
                }

                var result = principals.Select(a => new ActiveDirectoryUser
                {
                    UPN = a.UserPrincipalName,
                    DisplayName = a.DisplayName,
                    JobTitle = a.Description,
                    ObjectID = (Guid)a.Guid!,
                }).ToList();

                return Task.FromResult(result);
            }
        }

        public static UserEntity CreateUserFromAD(ActiveDirectoryUser adUser)
        {
            var ada = (ActiveDirectoryAuthorizer)AuthLogic.Authorizer!;

            var config = ada.GetConfig();

            using (var pc = GetPrincipalContext())
            {
                var principal = new UserPrincipal(pc)
                {
                    UserPrincipalName = adUser.UPN,
                };

                var userPc = new PrincipalSearcher(principal).FindOne();

                UserPrincipal userPrincipal = UserPrincipal.FindByIdentity(pc, userPc.SamAccountName);

                var acuCtx = new DirectoryServiceAutoCreateUserContext(pc, userPc.SamAccountName, config.DomainName!);

                using (ExecutionMode.Global())
                {
                    var user = Database.Query<UserEntity>().SingleOrDefaultEx(a => a.Mixin<UserADMixin>().SID == userPc.Sid.ToString());

                    if (user == null)
                    {
                        user = Database.Query<UserEntity>().SingleOrDefault(a => a.UserName == acuCtx.UserName) ??
                               (config.AllowMatchUsersBySimpleUserName ? Database.Query<UserEntity>().SingleOrDefault(a => a.Email == acuCtx.EmailAddress) : null);
                    }

                    if (user != null)
                    {
                        ada.UpdateUser(user, acuCtx);

                        return user;
                    }
                }

                var result = ada.OnAutoCreateUser(acuCtx);

                return result ?? throw new InvalidOperationException(ReflectionTools.GetPropertyInfo((ActiveDirectoryConfigurationEmbedded e) => e.AutoCreateUsers).NiceName() + " is not activated");

            }
        }
    }
}
