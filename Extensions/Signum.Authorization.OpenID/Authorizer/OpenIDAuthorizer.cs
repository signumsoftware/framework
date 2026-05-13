using Signum.API.Filters;
using Signum.Authorization.BaseAD;
using System.Security.Claims;
using System.Text.Json;

namespace Signum.Authorization.OpenID.Authorizer;

public class OpenIDAuthorizer : ICustomAuthorizer
{
    public Func<OpenIDConfigurationEmbedded?> GetConfig;

    public OpenIDAuthorizer(Func<OpenIDConfigurationEmbedded?> getConfig)
    {
        GetConfig = getConfig;
    }

    public virtual UserEntity Login(string userName, string password, out string authenticationType)
    {
        return AuthLogic.Login(userName, password, out authenticationType);
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
        var config = (OpenIDConfigurationEmbedded)ctx.Config;

        if (config.RoleMapping.Any() && ctx is OpenIDClaimsAutoCreateUserContext oidCtx)
        {
            var groups = ExtractRoles(oidCtx.ClaimsPrincipal, config.RoleClaimPath);

            var roles = config.RoleMapping
                .Where(m => groups.Any(g => g == m.ADNameOrGuid))
                .Select(a => a.Role)
                .Distinct()
                .NotNull()
                .ToList();

            if (roles.Any())
                return AuthLogic.GetOrCreateTrivialMergeRole(roles);
        }

        if (config.DefaultRole != null)
            return config.DefaultRole;

        if (throwIfNull)
            throw new InvalidOperationException("No Default Role set and no matching RoleMapping found");

        return null;
    }

    public virtual void UpdateUserInternal(UserEntity user, IAutoCreateUserContext ctx)
    {
        if (user.State == UserState.AutoDeactivate)
        {
            user.State = UserState.Active;
            user.DisabledOn = null;
        }

        if (ctx.ExternalId != null)
        {
            user.ExternalId = ctx.ExternalId;
            if (!UserEntity.AllowPasswordForUserWithExternalId)
            {
                user.PasswordHash = null;
                user.MustChangePassword = false;
            }
        }

        user.UserName = ctx.UserName;

        if (ctx.EmailAddress.HasText())
            user.Email = ctx.EmailAddress;

        if (user.CultureInfo == null && SignumCurrentContextFilter.CurrentContext is { } cc)
            user.CultureInfo = CultureServer.InferUserCulture(cc.HttpContext);
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

    // Extracts role values from the ClaimsPrincipal using a dotted path.
    // Simple paths ("roles", "groups") match claim types directly.
    // Dotted paths ("realm_access.roles") navigate from a JSON-valued claim.
    public static List<string> ExtractRoles(ClaimsPrincipal principal, string? roleClaimPath)
    {
        if (!roleClaimPath.HasText())
            return new List<string>();

        var parts = roleClaimPath.Split('.');

        if (parts.Length == 1)
        {
            return principal.Claims
                .Where(c => c.Type == roleClaimPath)
                .Select(c => c.Value)
                .ToList();
        }

        var claimValue = principal.Claims.SingleOrDefaultEx(c => c.Type == parts[0])?.Value;
        if (claimValue == null)
            return new List<string>();

        try
        {
            var doc = JsonDocument.Parse(claimValue);
            var current = doc.RootElement;

            for (int i = 1; i < parts.Length; i++)
            {
                if (current.ValueKind != JsonValueKind.Object || !current.TryGetProperty(parts[i], out current))
                    return new List<string>();
            }

            if (current.ValueKind == JsonValueKind.Array)
                return current.EnumerateArray()
                    .Where(e => e.ValueKind == JsonValueKind.String)
                    .Select(e => e.GetString()!)
                    .ToList();

            if (current.ValueKind == JsonValueKind.String)
                return new List<string> { current.GetString()! };

            return new List<string>();
        }
        catch
        {
            return new List<string>();
        }
    }
}
