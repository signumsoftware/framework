using DocumentFormat.OpenXml.Spreadsheet;
using Signum.Authorization;
using Signum.Authorization.ActiveDirectory.Azure;
using System.DirectoryServices.AccountManagement;
using System.Runtime.InteropServices.Marshalling;
using System.Security.Claims;

#pragma warning disable CA1416 // Validate platform compatibility
namespace Signum.Authorization.ActiveDirectory;

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
    public string IdentityValue { get; private set; }
    public string? EmailAddress => this.GetUserPrincipal().EmailAddress != null ? this.GetUserPrincipal().EmailAddress : null;

    public string FirstName => this.GetUserPrincipal().GivenName;

    public string LastName => this.GetUserPrincipal().Surname;

    public Guid? OID => null;

    public string? SID => this.GetUserPrincipal().Sid.Value;

    UserPrincipal? userPrincipal;

    public DirectoryServiceAutoCreateUserContext(PrincipalContext principalContext, string localName, string identityValue, UserPrincipal? userPrincipal = null)
    {
        PrincipalContext = principalContext;
        UserName = localName;
        IdentityValue = identityValue;
        this.userPrincipal = userPrincipal;
    }

    public UserPrincipal GetUserPrincipal() //https://stackoverflow.com/questions/14278274/how-i-get-active-directory-user-properties-with-system-directoryservices-account
    {
        return userPrincipal ?? (userPrincipal = UserPrincipal.FindByIdentity(PrincipalContext, IdentityValue));
    }
}

public class AzureClaimsAutoCreateUserContext : IAutoCreateUserContext
{
    public ClaimsPrincipal ClaimsPrincipal { get; private set; }

    public string GetClaim(string type) => ClaimsPrincipal.Claims.SingleEx(a => a.Type == type).Value;

    public string? TryGetClaim(string type) => ClaimsPrincipal.Claims.SingleOrDefaultEx(a => a.Type == type)?.Value;

    public virtual Guid? OID
    {
        get
        {
            var oid = TryGetClaim("http://schemas.microsoft.com/identity/claims/objectidentifier")
                   ?? TryGetClaim("oid"); // fallback for AAD v2.0
            return oid != null ? Guid.Parse(oid) : null;
        }
    }

    public string? SID => null;

    public virtual string UserName => GetClaim("preferred_username");
    public virtual string? EmailAddress => GetClaim("preferred_username");

    public virtual string? FullName => TryGetClaim("name");

    public virtual string FirstName
    {
        get
        {
            var name = FullName;

            return name == null ? "Unknown" : 
                name.Contains(",") ? name.After(",").Trim() :
                name.TryBefore(" ")?.Trim() ?? name.DefaultToNull() ?? "Unknown";
        }
    }

    public virtual string LastName
    {
        get
        {
            var name = FullName;

            return name == null ? "Unknown" : 
                name.Contains(",") ? name.Before(",").Trim() : 
                name.TryAfter(" ")?.Trim() ??  "Unknown";
        }
    }


    public string AccessToken; 
    public AzureClaimsAutoCreateUserContext(ClaimsPrincipal claimsPrincipal, string accessToken)
    {
        this.ClaimsPrincipal = claimsPrincipal;
        this.AccessToken = accessToken;
    }
}

public class AzureB2CClaimsAutoCreateUserContext : AzureClaimsAutoCreateUserContext
{
  
    public override string UserName => GetClaim("emails");
    public override string? EmailAddress => GetClaim("emails");

    public override string? FullName => TryGetClaim("name") ?? " ".Combine(FirstName, LastName);
    public override string FirstName => GetClaim("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/givenname");
    public override string LastName => GetClaim("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/surname");

    public override Guid? OID => Guid.Parse(GetClaim("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier"));

    public AzureB2CClaimsAutoCreateUserContext(ClaimsPrincipal claimsPrincipal, string accessToken) : base(claimsPrincipal, accessToken)
    {
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
            var config = this.GetConfig();

            var windowsAD = config.WindowsAD;

            if (windowsAD != null && windowsAD.LoginWithActiveDirectoryRegistry)
            {
                try
                {
                    using (PrincipalContext pc = new PrincipalContext(ContextType.Domain, windowsAD.DomainName, userName, password))
                    {
                        if (pc.ValidateCredentials(userName, password, ContextOptions.Negotiate))
                        {
                            var localName = userName.TryBeforeLast('@') ?? userName.TryAfter('\\') ?? userName;

                            var dsacuCtx = new DirectoryServiceAutoCreateUserContext(pc, localName, identityValue: userName);
                            
                            var sid = dsacuCtx.GetUserPrincipal().Sid;

                            UserEntity? user = Database.Query<UserEntity>().SingleOrDefaultEx(a => a.Mixin<UserADMixin>().SID == sid.ToString()) ?? 
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
                                if (!GetConfig().AutoCreateUsers)
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
        else if (ctx.OID != null && this.GetConfig().AzureAD?.ApplicationID != null)
        {
            if (config.RoleMapping.Any())
            {
                var groups = ctx is AzureClaimsAutoCreateUserContext ac && this.GetConfig().AzureAD!.UseDelegatedPermission ? AzureADLogic.CurrentADGroupsInternal(ac.AccessToken) :
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
        if (user.State == UserState.AutoDeactivate)
        {
            user.State = UserState.Active;
            user.DisabledOn = null;
        }

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
