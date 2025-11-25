using DocumentFormat.OpenXml.Spreadsheet;
using Signum.Authorization.AzureAD;
using System.DirectoryServices.AccountManagement;
using System.Runtime.InteropServices.Marshalling;

#pragma warning disable CA1416 // Validate platform compatibility
namespace Signum.Authorization.AzureAD.Authorizer;

public class AzureADAuthorizer : ICustomAuthorizer
{ 
    public Func<string? /*adVariant*/, AzureADConfigurationEmbedded?> GetConfig;

    public AzureADAuthorizer(Func<string?, AzureADConfigurationEmbedded?> getConfig)
    {
        GetConfig = getConfig;
    }

    public virtual UserEntity Login(string userName, string password, out string authenticationType)
    {
        var passwordHashes = PasswordEncoding.EncodePasswordAlternatives(userName, password);

        return AuthLogic.Login(userName, passwordHashes, out authenticationType);
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
        var config = (AzureADConfigurationEmbedded)ctx.Config;

        if (ctx.OID != null && config != null)
        {
            if (config.RoleMapping.Any())
            {
                var groups = ctx is AzureClaimsAutoCreateUserContext ac && config.UseDelegatedPermission ? 
                    AzureADLogic.CurrentADGroupsInternal(ac.AccessToken) :
                    AzureADLogic.CurrentADGroupsInternal(ctx.OID!.Value);

                var roles = config.RoleMapping.Where(m =>
                {
                    Guid.TryParse(m.ADNameOrGuid, out var guid);
                    var found = groups.Any(g => g.DisplayName == m.ADNameOrGuid || g.Id == guid);

                    return found;
                }).Select(a => a.Role).Distinct().NotNull().ToList();

                if (roles.Any())
                {
                    var result = AuthLogic.GetOrCreateTrivialMergeRole(roles);

                    return result;
                }

                if (config.DefaultRole != null)
                    return config.DefaultRole;

                if (throwIfNull)
                    throw new InvalidOperationException("No Default Role set and no matching RoleMapping found for any role: \n" + groups.ToString(a => a.Id + ": " + a.DisplayName, "\n"));
                else
                    return null;
            }
            else
            {
                if (config.DefaultRole != null)
                    return config.DefaultRole;

                if (throwIfNull)
                    throw new InvalidOperationException("No Default Role set");
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

        if (ctx.OID != null)
        {
            user.Mixin<UserAzureADMixin>().OID = ctx.OID;
            if (!UserAzureADMixin.AllowPasswordForActiveDirectoryUsers)
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
        return AzureADLogic.FindActiveDirectoryUsers(subString, count, token);
    }

    public UserEntity CreateADUser(ActiveDirectoryUser user)
    {
        return AzureADLogic.CreateUserFromAD(user);
    }
}
