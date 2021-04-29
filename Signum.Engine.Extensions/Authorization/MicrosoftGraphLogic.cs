using Microsoft.Graph;
using Microsoft.Graph.Auth;
using Signum.Engine.Mailing;
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

            var result = await graphClient.Users.Request().Filter($"startswith(displayName, '{subStr}')" /* OR startswith(displayName, '{subStr}') OR startswith(displayName, '{subStr}') OR mail eq '{subStr}'"*/).Top(top).GetAsync(token);

            return result.Select(a => new ActiveDirectoryUser
            {
                UPN = a.UserPrincipalName,
                DisplayName = a.DisplayName,
                JobTitle = a.JobTitle,
                ObjectID = Guid.Parse(a.Id),
            }).ToList();
        }


        public static UserEntity CreateUserFromAD(ActiveDirectoryUser user)
        {
            ClientCredentialProvider authProvider = GetClientCredentialProvider();
            GraphServiceClient graphClient = new GraphServiceClient(authProvider);
            var u = graphClient.Users[user.ObjectID.ToString()].Request().GetAsync().Result;


            var result = ((ActiveDirectoryAuthorizer)AuthLogic.Authorizer!).OnAutoCreateUser(new MicrosoftGraphCreateUserContext(u));

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
