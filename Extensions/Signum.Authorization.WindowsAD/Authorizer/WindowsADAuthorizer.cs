using Signum.Authorization.ADGroups;
using System.DirectoryServices.AccountManagement;

#pragma warning disable CA1416 // Validate platform compatibility
namespace Signum.Authorization.WindowsAD.Authorizer;

public class WindowsADAuthorizer : ICustomAuthorizer
{
    public Func<WindowsADConfigurationEmbedded?> GetConfig;

    public WindowsADAuthorizer(Func<WindowsADConfigurationEmbedded?> getConfig)
    {
        GetConfig = getConfig;
    }

    public virtual UserEntity Login(string userName, string password, out string authenticationType)
    {
        var passwordHashes = PasswordEncoding.EncodePasswordAlternatives(userName, password);

        if (AuthLogic.TryRetrieveUser(userName, passwordHashes) != null)
            return AuthLogic.Login(userName, passwordHashes, out authenticationType); //Database is faster than Active Directory

        UserEntity? user = LoginWithActiveDirectoryRegistry(userName, password);
        if (user != null)
        {
            authenticationType = "adRegistry";
            return user;
        }

        return AuthLogic.Login(userName, passwordHashes, out authenticationType);
    }

    public virtual UserEntity? LoginWithActiveDirectoryRegistry(string userName, string password)
    {
        using (AuthLogic.Disable())
        {
            var config = GetConfig();


            if (config != null && config.LoginWithActiveDirectoryRegistry)
            {
                try
                {
                    using (PrincipalContext pc = new PrincipalContext(ContextType.Domain, config.DomainName, userName, password))
                    {
                        if (pc.ValidateCredentials(userName, password, ContextOptions.Negotiate))
                        {
                            var localName = userName.TryBeforeLast('@') ?? userName.TryAfter('\\') ?? userName;

                            var dsacuCtx = new DirectoryServiceAutoCreateUserContext(pc, localName, identityValue: userName);
                            
                            var sid = dsacuCtx.GetUserPrincipal().Sid;

                            UserEntity? user = Database.Query<UserEntity>().SingleOrDefaultEx(a => a.Mixin<UserWindowsADMixin>().SID == sid.ToString()) ?? 
                                AuthLogic.RetrieveUser(localName);

                            if (user != null)
                            {
                                UpdateUser(user, dsacuCtx);

                                if (user.State == UserState.Deactivated)
                                    throw new InvalidOperationException(LoginAuthMessage.User0IsDeactivated.NiceToString(user));

                                AuthLogic.OnUserLogingIn(user);
                                return user;
                            }
                            else
                            {
                                if (!config.AutoCreateUsers)
                                    throw new InvalidOperationException(ActiveDirectoryAuthorizerMessage.ActiveDirectoryUser0IsNotAssociatedWithAUserInThisApplication.NiceToString(localName));

                                user = OnCreateUser(dsacuCtx);

                                if (user.State == UserState.Deactivated)
                                    throw new InvalidOperationException(LoginAuthMessage.User0IsDeactivated.NiceToString(user));

                                AuthLogic.OnUserLogingIn(user);
                                return user;
                            }
                        }
                    }
                }
                catch (PrincipalServerDownException)
                {
                    // Do nothing for this kind of Active Directory exception
                }
            }

            return null;
        }
    }

    public virtual UserEntity OnCreateUser(IAutoCreateUserContext ctx)
    {
        using (var tr = new Transaction())
        {
            var user = CreateUserInternal(ctx);
            if (user.IsNew)
            {
                using (ExecutionMode.Global())
                using (OperationLogic.AllowSave<UserEntity>())
                {
                    user.Save();
                }
            }

            return tr.Commit(user); 
        }
    }

    public virtual UserEntity CreateUserInternal(IAutoCreateUserContext ctx)
    {
        var result = new UserEntity
        {
            UserName = ctx.UserName,
            PasswordHash = null,
            Email = ctx.EmailAddress,
            Role = GetRole(ctx, throwIfNull: true)!,
            State = UserState.Active,
        };

        UpdateUserInternal(result, ctx);

        return result;
    }

    public virtual Lite<RoleEntity>? GetRole(IAutoCreateUserContext ctx, bool throwIfNull)
    {
        var config = GetConfig();
        if (ctx is DirectoryServiceAutoCreateUserContext ds)
        {
            var groups = ds.GetUserPrincipal().GetGroups(ds.PrincipalContext).ToList();
            var roles = config!.RoleMapping.Where(m =>
            {
                Guid.TryParse(m.ADNameOrGuid, out var guid);
                var found = groups.Any(g => g.Name == m.ADNameOrGuid || g.Guid == guid);

                return found;
            }).Select(a => a.Role).Distinct().NotNull().ToList();

            if (roles.Any())
            {
                var result = AuthLogic.GetOrCreateTrivialMergeRole(roles);

                return result;
            }
            else
            {
                if (config.DefaultRole != null)
                    return config.DefaultRole;

                if (throwIfNull)
                    throw new InvalidOperationException("No Default Role set and no matching RoleMapping found for any role: \n" + groups.ToString(a => a.Name, "\n"));
                else
                    return null;
            }
        }
        else
        {
            if (config?.DefaultRole != null)
                return config.DefaultRole;

            if (throwIfNull)
                throw new InvalidOperationException("No default role set");
            else
                return null;

        }
    }


    public virtual void UpdateUserInternal(UserEntity user, IAutoCreateUserContext ctx)
    {
        if (user.State == UserState.AutoDeactivate)
        {
            user.State = UserState.Active;
            user.DisabledOn = null;
        }

        if (ctx.SID != null)
        {
            user.Mixin<UserWindowsADMixin>().SID = ctx.SID;
            if (!UserWindowsADMixin.AllowPasswordForActiveDirectoryUsers)
                user.PasswordHash = null;
        }

        user.UserName = ctx.UserName;

        if (ctx.EmailAddress.HasText())
            user.Email = ctx.EmailAddress;
    }

    public virtual void UpdateUser(UserEntity user, IAutoCreateUserContext ctx)
    {
        using (var tr = new Transaction())
        {
            UpdateUserInternal(user, ctx);

            if (GraphExplorer.IsGraphModified(user))
            {
                using (AuthLogic.Disable())
                using (OperationLogic.AllowSave<UserEntity>())
                {
                    user.Save();
                }
            }

            tr.Commit();
        }

    }

    public Task<List<ActiveDirectoryUser>> FindUser(string subString, int count, CancellationToken token)
    {
        return WindowsADLogic.SearchUser(subString, count);
    }

    public UserEntity CreateADUser(ActiveDirectoryUser user)
    {
        return WindowsADLogic.CreateUserFromAD(user);
    }
}
