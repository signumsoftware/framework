using Signum.Scheduler;
using System.DirectoryServices;
using System.DirectoryServices.AccountManagement;
using Signum.API;
using Signum.Mailing;
using Signum.Authorization.ActiveDirectory.Azure;
using DocumentFormat.OpenXml.Vml.Office;
using Signum.Engine.Sync;
using DocumentFormat.OpenXml.Wordprocessing;

namespace Signum.Authorization.ActiveDirectory.Windows;

#pragma warning disable CA1416 // Validate platform compatibility
public static class WindowsActiveDirectoryLogic
{
    public static void Start(SchemaBuilder sb, bool deactivateUsersTask)
    {
        if (sb.AlreadyDefined(MethodInfo.GetCurrentMethod()))
            return;

        PermissionLogic.RegisterTypes(typeof(ActiveDirectoryPermission));

        if (sb.WebServerBuilder != null)
        {
            ReflectionServer.RegisterLike(typeof(ActiveDirectoryPermission), () => ActiveDirectoryPermission.InviteUsersFromAD.IsAuthorized());
            ReflectionServer.RegisterLike(typeof(OnPremisesExtensionAttributesModel), () => false);
        }

        Lite.RegisterLiteModelConstructor((UserEntity u) => new UserLiteModel
        {
            UserName = u.UserName,
            ToStringValue = u.ToString(),
            OID = u.Mixin<UserADMixin>().OID,
            SID = u.Mixin<UserADMixin>().SID,
        });

        if (deactivateUsersTask)
        {
            SimpleTaskLogic.Register(ActiveDirectoryTask.DeactivateUsers, stc =>
            {
                var config = ((ActiveDirectoryAuthorizer)AuthLogic.Authorizer!).GetConfig();

                var windowsAD = config.GetWindowsAD();

                var list = Database.Query<UserEntity>().ToList();

                using (var domainContext = new PrincipalContext(ContextType.Domain, windowsAD.DomainName, windowsAD.DirectoryRegistry_Username + "@" + windowsAD.DomainName, windowsAD.DirectoryRegistry_Password!))
                {
                    stc.ForeachWriting(list, u => u.UserName, u =>
                    {
                        var foundUser = UserPrincipal.FindByIdentity(domainContext, IdentityType.SamAccountName, u.UserName);

                        if (u.State == UserState.Active)
                        {
                            if (foundUser != null && foundUser.Enabled.HasValue && foundUser.Enabled == false)
                            {
                                stc.StringBuilder.AppendLine($"User {u.Id} ({u.UserName}) with SID {u.Mixin<UserADMixin>().SID} has been deactivated in AD");
                                u.Execute(UserOperation.Deactivate);
                            }
                            else
                            {

                            }

                            if (foundUser == null && u.PasswordHash == null)
                            {
                                stc.StringBuilder.AppendLine($"User {u.Id} ({u.UserName}) with SID {u.Mixin<UserADMixin>().SID} has been deactivated in AD");
                                u.Execute(UserOperation.Deactivate);
                            }

                        }

                        if (u.State == UserState.Deactivated)
                        {
                            if (foundUser != null && foundUser.Enabled.HasValue && foundUser.Enabled == true)
                            {
                                stc.StringBuilder.AppendLine($"User {u.Id} ({u.UserName}) with SID {u.Mixin<UserADMixin>().SID} has been reactivated in AD");
                                u.Execute(UserOperation.Reactivate);
                            }
                        }
                    });

                    return null;
                }
            });
        }
    }

    static PrincipalContext GetPrincipalContext()
    {
        var config = ((ActiveDirectoryAuthorizer)AuthLogic.Authorizer!).GetConfig();
        var windowsAD = config.GetWindowsAD();

        if (windowsAD.DirectoryRegistry_Username.HasText() && windowsAD.DirectoryRegistry_Password.HasText())
            return new PrincipalContext(ContextType.Domain, windowsAD.DomainName, windowsAD.DirectoryRegistry_Username + "@" + windowsAD.DomainName, windowsAD.DirectoryRegistry_Password!);
        else
            return new PrincipalContext(ContextType.Domain, windowsAD.DomainName);
    }

    public static Task<List<ActiveDirectoryUser>> SearchUser(string searchUserName, int limit)
    {
        using (var pc = GetPrincipalContext())
        {
            using (var searcher = new DirectorySearcher())
            {
                // Construct the LDAP OR filter
                var filters = new List<string>();

                filters.Add($"(sAMAccountName=*{searchUserName}*)");
                filters.Add($"(displayName=*{searchUserName}*)");

                if (searchUserName.Contains("@"))
                {
                    filters.Add($"(mail={searchUserName})");
                }

                // Combine the filters into an OR condition using the | operator
                var ldapFilter = $"(|{string.Join("", filters)})";
                searcher.Filter = $"(&(objectCategory=person)(objectClass=user){ldapFilter})";

                // Specify properties to load
                searcher.PropertiesToLoad.Add("userPrincipalName");
                searcher.PropertiesToLoad.Add("displayName");
                searcher.PropertiesToLoad.Add("description");
                searcher.PropertiesToLoad.Add("objectSid");

                // Enable paging
                searcher.SizeLimit = limit;

                // Perform the search
                var results = searcher.FindAll();

                // Map results to ActiveDirectoryUser objects
                var activeDirectoryUsers = new List<ActiveDirectoryUser>();
                foreach (SearchResult result in results)
                {
                    var user = new ActiveDirectoryUser
                    {
                        UPN = result.Properties["userPrincipalName"]?.Count > 0 ? result.Properties["userPrincipalName"][0].ToString()! : "",
                        DisplayName = result.Properties["displayName"]?.Count > 0 ? result.Properties["displayName"][0].ToString()! : "",
                        JobTitle = result.Properties["description"]?.Count > 0 ? result.Properties["description"][0].ToString()! : "",
                        SID = result.Properties["objectSid"]?.Count > 0 ? new System.Security.Principal.SecurityIdentifier((byte[])result.Properties["objectSid"][0], 0).ToString() : null,
                        ObjectID = null
                    };

                    activeDirectoryUsers.Add(user);
                }

                // Return distinct results
                var distinctUsers = activeDirectoryUsers
                    .DistinctBy(a => a.SID)
                    .OrderBy(a => a.UPN)
                    .ToList();

                return Task.FromResult(distinctUsers);
            }
        }
    }


    //public static Task<List<ActiveDirectoryUser>> SearchUser(string searchUserName)
    //{
    //    using (var pc = GetPrincipalContext())
    //    {
    //        List<UserPrincipal> searchPrinciples = new List<UserPrincipal>();

    //        searchPrinciples.Add(new UserPrincipal(pc)
    //        {
    //            SamAccountName = "*" + searchUserName + "*",
    //        });

    //        searchPrinciples.Add(new UserPrincipal(pc)
    //        {
    //            DisplayName = "*" + searchUserName + "*",
    //        });


    //        if (searchUserName.Contains("@"))
    //        {
    //            searchPrinciples.Add(new UserPrincipal(pc)
    //            {
    //                EmailAddress = searchUserName,
    //            });
    //        }

    //        List<Principal> principals = new List<Principal>();
    //        var searcher = new PrincipalSearcher();

    //        foreach (var item in searchPrinciples)
    //        {
    //            searcher = new PrincipalSearcher(item);
    //            principals.AddRange(searcher.FindOne());
    //        }

    //        var result = principals.Select(a => new ActiveDirectoryUser
    //        {
    //            UPN = a.UserPrincipalName,
    //            DisplayName = a.DisplayName,
    //            JobTitle = a.Description,
    //            SID = a.Sid.ToString(),
    //            ObjectID = null,
    //        })
    //            .DistinctBy(a => a.SID)
    //        .OrderBy(a => a.UPN)
    //        .ToList();

    //        return Task.FromResult(result);
    //    }
    //}

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

            var acuCtx = new DirectoryServiceAutoCreateUserContext(pc, userPc.SamAccountName, userPc.UserPrincipalName, userPrincipal);

            using (ExecutionMode.Global())
            using (var tr = new Transaction())
            {
                var user = Database.Query<UserEntity>().SingleOrDefaultEx(a => a.Mixin<UserADMixin>().SID == userPc.Sid.ToString());

                if (user == null)
                {
                    user = Database.Query<UserEntity>().SingleOrDefault(a => a.UserName == acuCtx.UserName) ??
                           (config.AllowMatchUsersBySimpleUserName ? Database.Query<UserEntity>().SingleOrDefault(a => a.Email == acuCtx.EmailAddress && acuCtx.EmailAddress != null) : null);
                }

                if (user != null)
                {
                    if (config.AutoUpdateUsers)
                        ada.UpdateUser(user, acuCtx);

                    return tr.Commit(user);
                }
                else
                {
                    var result = ada.OnCreateUser(acuCtx);

                    return tr.Commit(result);
                }
            }
        }
    }

    public static byte[]? GetProfilePicture(string userName, int? size = null)
    {
        using (AuthLogic.Disable())
        {
            using (var pc = GetPrincipalContext())
            {
                var config = ((ActiveDirectoryAuthorizer)AuthLogic.Authorizer!).GetConfig();

                var up = UserPrincipal.FindByIdentity(pc, userName);

                if (up == null)
                    return null;

                DirectoryEntry? directoryEntry = up.GetUnderlyingObject() as DirectoryEntry;

                if (directoryEntry == null)
                    return null;

                if (directoryEntry!.Properties.Contains("thumbnailPhoto"))
                {
                    var byteFile = directoryEntry!.Properties["thumbnailPhoto"][0] as byte[];

                    return byteFile;
                }
            }

            return null;
        }
    }


    public static bool CheckUserActive(string username)
    {
        var config = ((ActiveDirectoryAuthorizer)AuthLogic.Authorizer!).GetConfig();

        using (var domainContext = new PrincipalContext(ContextType.Domain, config.WindowsAD!.DomainName))
        {
            using (var foundUser = UserPrincipal.FindByIdentity(domainContext, IdentityType.SamAccountName, username))
            {
                if (foundUser.Enabled.HasValue)
                {
                    return (bool)foundUser.Enabled;
                }
                else
                {
                    return false;
                }
            }
        }
    }

  

    public static void CheckAllUserActive()
    {

    }
}
