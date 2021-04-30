using Microsoft.Graph;
using Microsoft.Graph.Auth;
using Signum.Engine.Mailing;
using Signum.Engine.Operations;
using Signum.Entities.Authorization;
using Signum.Entities.Mailing;
using Signum.Utilities;
using Signum.Utilities.Reflection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Signum.Engine.Authorization
{
    public static class MicrosoftGraphLogic
    {
        public static Func<ClientCredentialProvider> GetClientCredentialProvider = () => ((ActiveDirectoryAuthorizer)AuthLogic.Authorizer!).GetConfig().GetAuthProvider();

        public static async Task<List<ActiveDirectoryUser>> FindActiveDirectoryUsers(string subStr, int top, CancellationToken token)
        {
            
            ClientCredentialProvider authProvider = GetClientCredentialProvider();
            GraphServiceClient graphClient = new GraphServiceClient(authProvider);

            subStr = subStr.Replace("'", "''");

            var query = subStr.Contains("@") ? $"mail eq '{subStr}'" :
                subStr.Contains(",") ? $"startswith(givenName, '{subStr.After(",").Trim()}') AND startswith(surname, '{subStr.Before(",").Trim()}') OR startswith(displayname, '{subStr.Trim()}')" :
                subStr.Contains(" ") ? $"startswith(givenName, '{subStr.Before(" ").Trim()}') AND startswith(surname, '{subStr.After(" ").Trim()}') OR startswith(displayname, '{subStr.Trim()}')" :
                 $"startswith(givenName, '{subStr}') OR startswith(surname, '{subStr}') OR startswith(displayname, '{subStr.Trim()}') OR startswith(mail, '{subStr.Trim()}')";

            var result = await graphClient.Users.Request().Filter(query).Top(top).GetAsync(token);

            return result.Select(a => new ActiveDirectoryUser
            {
                UPN = a.UserPrincipalName,
                DisplayName = a.DisplayName,
                JobTitle = a.JobTitle,
                ObjectID = Guid.Parse(a.Id),
            }).ToList();
        }


        public static UserEntity CreateUserFromAD(ActiveDirectoryUser adUser)
        {
            ClientCredentialProvider authProvider = GetClientCredentialProvider();
            GraphServiceClient graphClient = new GraphServiceClient(authProvider);
            var msGraphUser = graphClient.Users[adUser.ObjectID.ToString()].Request().GetAsync().Result;

            using (ExecutionMode.Global())
            {
                var user = Database.Query<UserEntity>().SingleOrDefaultEx(a => a.Mixin<UserOIDMixin>().OID == Guid.Parse(msGraphUser.Id));
                if(user != null)
                    return user;

                var config = ((ActiveDirectoryAuthorizer)AuthLogic.Authorizer!).GetConfig();

                user = Database.Query<UserEntity>().SingleOrDefault(a => a.UserName == msGraphUser.UserPrincipalName) ??
                       (msGraphUser.UserPrincipalName.Contains("@") && config.AllowMatchUsersBySimpleUserName ? Database.Query<UserEntity>().SingleOrDefault(a => a.Email == msGraphUser.UserPrincipalName || a.UserName == msGraphUser.UserPrincipalName.Before("@")) : null);

                if (user != null && user.Mixin<UserOIDMixin>().OID == null)
                {
                    using (AuthLogic.Disable())
                    using (OperationLogic.AllowSave<UserEntity>())
                    {
                        user.Mixin<UserOIDMixin>().OID = Guid.Parse(msGraphUser.Id);
                        user.UserName = msGraphUser.UserPrincipalName;
                        user.Email = msGraphUser.UserPrincipalName;
                        if (!UserOIDMixin.AllowUsersWithPassswordAndOID)
                            user.PasswordHash = null;
                        user.Save();
                    }

                    return user;
                }

            }

            var result = ((ActiveDirectoryAuthorizer)AuthLogic.Authorizer!).OnAutoCreateUser(new MicrosoftGraphCreateUserContext(msGraphUser));

            return result ?? throw new InvalidOperationException(ReflectionTools.GetPropertyInfo((ActiveDirectoryConfigurationEmbedded e) => e.AutoCreateUsers).NiceName() + " is not activated");
        }
    }

    public class MicrosoftGraphCreateUserContext : IAutoCreateUserContext
    {
        public MicrosoftGraphCreateUserContext(User user)
        {
            User = user;
        }

        public User User { get; set; }

        public string UserName => User.UserPrincipalName;
        public string? EmailAddress => User.UserPrincipalName;

        public string FirstName => User.GivenName;
        public string LastName => User.Surname;

        public Guid? OID => Guid.Parse(User.Id);
    }

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    public class ActiveDirectoryUser
    {
        public string DisplayName;
        public string UPN;
        public Guid ObjectID;

        public string JobTitle;
    }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
}
