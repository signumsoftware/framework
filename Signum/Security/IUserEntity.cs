using System.Globalization;

namespace Signum.Security;

public interface IUserEntity : IEntity
{
}

public class UserWithClaims
{
    public readonly Lite<IUserEntity> User;
    public readonly Dictionary<string, object?> Claims;

    public static event Action<UserWithClaims, IUserEntity>? FillClaims = null;

    public UserWithClaims(Lite<IUserEntity> user, Dictionary<string, object?> claims)
    {
        this.User = user;
        this.Claims = claims;
    }

    public UserWithClaims(IUserEntity user)
    {
        User = user.ToLite();
        this.Claims = new Dictionary<string, object?>();
        FillClaims?.Invoke(this, user);
    }

    public object? GetClaim(string claimName)
    {
        return Claims.TryGetCN(claimName);
    }
}

public static class UserHolder
{
    public static readonly string UserSessionKey = "user";
    public static event Action? CurrentUserChanged;

    public static readonly SessionVariable<UserWithClaims> CurrentUserVariable = Statics.SessionVariable<UserWithClaims>(UserSessionKey);
    public static UserWithClaims/*?*/ Current
    {
        get { return CurrentUserVariable.Value; }
        set
        {
            CurrentUserVariable.Value = value;
            CurrentUserChanged?.Invoke();
        }
    }

    public static CultureInfo? CurrentUserCulture
    {
        get
        {
            var culture = UserHolder.Current?.GetClaim("Culture") as string;
            return culture == null ? null : System.Globalization.CultureInfo.GetCultureInfo(culture);
        }
    }

    public static IDisposable UserSession(IUserEntity user) => UserSession(new UserWithClaims(user));
    public static IDisposable UserSession(UserWithClaims user)
    {
        var result = ScopeSessionFactory.OverrideSession();
        UserHolder.Current = user;
        return result;
    }
}
