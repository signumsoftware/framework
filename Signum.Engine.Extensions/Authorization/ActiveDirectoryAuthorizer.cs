using Signum.Entities.Authorization;
using Signum.Entities.Reflection;
using Signum.Services;
using System.DirectoryServices.AccountManagement;
using System.Security.Claims;

#pragma warning disable CA1416 // Validate platform compatibility
namespace Signum.Engine.Authorization;

public interface IAutoCreateUserContext
{
    public string UserName { get; }
    public string? EmailAddress { get; }
    public string FirstName { get; }
    public string LastName { get; }
    public Guid? OID { get;  }
    public string? SID { get; }
}

public class DirectoryServiceAutoCreateUserContext : IAutoCreateUserContext
{
    public readonly PrincipalContext PrincipalContext;
    public string UserName { get; private set; }
    public string DomainName { get; private set; }
    public string? EmailAddress => this.GetUserPrincipal().EmailAddress != null ? this.GetUserPrincipal().EmailAddress : null;

    public string FirstName => this.GetUserPrincipal().GivenName;

    public string LastName => this.GetUserPrincipal().Surname;

    public Guid? OID => null;

    public string? SID => this.GetUserPrincipal().Sid.Value;

    UserPrincipal? userPrincipal;

    public DirectoryServiceAutoCreateUserContext(PrincipalContext principalContext, string localName, string domainName, UserPrincipal? userPrincipal = null)
    {
        PrincipalContext = principalContext;
        UserName = localName;
        DomainName = domainName;
        this.userPrincipal = userPrincipal;
    }

    public UserPrincipal GetUserPrincipal() //https://stackoverflow.com/questions/14278274/how-i-get-active-directory-user-properties-with-system-directoryservices-account
    {
        return userPrincipal ?? (userPrincipal = UserPrincipal.FindByIdentity(PrincipalContext, DomainName + @"\" + UserName));
    }
}

public class AzureClaimsAutoCreateUserContext : IAutoCreateUserContext
{
    public ClaimsPrincipal ClaimsPrincipal { get; private set; }

    string GetClaim(string type) => ClaimsPrincipal.Claims.SingleEx(a => a.Type == type).Value;

    string? TryGetClain(string type) => ClaimsPrincipal.Claims.SingleOrDefaultEx(a => a.Type == type)?.Value;

    public Guid? OID => Guid.Parse(GetClaim("http://schemas.microsoft.com/identity/claims/objectidentifier"));

    public string? SID => null;

    public string UserName => GetClaim("preferred_username");
    public string? EmailAddress => GetClaim("preferred_username");

    public string? FullName => TryGetClain("name");

    public string FirstName
    {
        get
        {
            var name = FullName;

            return name == null ? "Unknown" : 
                name.Contains(",") ? name.After(",").Trim() :
                name.TryBefore(" ")?.Trim() ?? name.DefaultToNull() ?? "Unknown";
        }
    }

    public string LastName
    {
        get
        {
            var name = FullName;

            return name == null ? "Unknown" : 
                name.Contains(",") ? name.Before(",").Trim() : 
                name.TryAfter(" ")?.Trim() ??  "Unknown";
        }
    }

  

    public AzureClaimsAutoCreateUserContext(ClaimsPrincipal claimsPrincipal)
    {
        this.ClaimsPrincipal = claimsPrincipal;
    }
}

public class ActiveDirectoryAuthorizer : ICustomAuthorizer
{
    public Func<ActiveDirectoryConfigurationEmbedded> GetConfig;

    public ActiveDirectoryAuthorizer(Func<ActiveDirectoryConfigurationEmbedded> getConfig)
    {
        this.GetConfig = getConfig;
    }

    public virtual UserEntity Login(string userName, string password, out string authenticationType)
    {
        var passwordHash = Security.EncodePassword(password);
        if (AuthLogic.TryRetrieveUser(userName, passwordHash) != null)
            return AuthLogic.Login(userName, passwordHash, out authenticationType); //Database is faster than Active Directory

        UserEntity? user = LoginWithActiveDirectoryRegistry(userName, password);
        if (user != null)
        {
            authenticationType = "adRegistry";
            return user;
        }

        return AuthLogic.Login(userName, Security.EncodePassword(password), out authenticationType);
    }

    public virtual UserEntity? LoginWithActiveDirectoryRegistry(string userName, string password)
    {
        using (AuthLogic.Disable())
        {
            var config = this.GetConfig();
            var domainName = userName.TryAfterLast('@') ?? userName.TryBefore('\\') ?? config.DomainName;
            var localName = userName.TryBeforeLast('@') ?? userName.TryAfter('\\') ?? userName;

            if (domainName != null && config.LoginWithActiveDirectoryRegistry)
            {
                try
                {
                    using (PrincipalContext pc = new PrincipalContext(ContextType.Domain, domainName, localName + "@" + domainName, password))
                    {
                        if (pc.ValidateCredentials(localName + "@" + domainName, password, ContextOptions.Negotiate))
                        {
                            UserEntity? user = AuthLogic.RetrieveUser(userName);

                            var dsacuCtx = new DirectoryServiceAutoCreateUserContext(pc, localName, domainName!);

                            if (user != null)
                            {
                                UpdateUser(user, dsacuCtx);

                                AuthLogic.OnUserLogingIn(user);

                                return user;
                            }
                            else
                            {

                                if (!GetConfig().AutoCreateUsers)
                                    throw new InvalidOperationException(ActiveDirectoryAuthorizerMessage.ActiveDirectoryUser0IsNotAssociatedWithAUserInThisApplication.NiceToString(localName));

                                user = OnCreateUser(dsacuCtx);
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
            var user = this.CreateUserInternal(ctx);
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
            var roles = config.RoleMapping.Where(m =>
            {
                Guid.TryParse(m.ADNameOrGuid, out var guid);
                var found = groups.Any(g => g.Name == m.ADNameOrGuid || g.Guid == guid);

                return found;
            }).Select(a=>a.Role).Distinct().NotNull().ToList();

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
                    throw new InvalidOperationException("No Default Role set and no matching RoleMapping found for any role: \r\n" + groups.ToString(a => a.Name, "\r\n"));
                else
                    return null;
            }
        }
        else
        {
            if (config.DefaultRole != null)
                return config.DefaultRole;

            if (throwIfNull)
                throw new InvalidOperationException("No default role set");
            else
                return null;

        }
    }


    public virtual void UpdateUserInternal(UserEntity user, IAutoCreateUserContext ctx)
    {
        if (ctx.OID != null)
        {
            user.Mixin<UserADMixin>().OID = ctx.OID;
            if (!UserADMixin.AllowPasswordForActiveDirectoryUsers)
                user.PasswordHash = null;
        }

        if (ctx.SID != null)
        {
            user.Mixin<UserADMixin>().SID = ctx.SID;
            if (!UserADMixin.AllowPasswordForActiveDirectoryUsers)
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
}
