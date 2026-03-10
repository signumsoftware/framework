using ModelContextProtocol.Server;
using Signum.Authorization;
using System.ComponentModel;
using System.Globalization;

namespace Signum.Agent.Skills;

public class CurrentUserSkill : AgentSkill
{
    public CurrentUserSkill()
    {
        ShortDescription = "Returns information about the currently authenticated user";
        IsAllowed = () => true;
    }

    public record CurrentUserInfo(
        string UserId,
        string UserLiteKey,
        string UserName,
        string UserRole,
        string CurrentCulture,
        string CurrentUICulture
    );

    [McpServerTool, Description("Returns the current user's Id, Lite key, UserName, Role, CurrentCulture and CurrentUICulture")]
    public static CurrentUserInfo GetCurrentUser()
    {
        var userLite = UserEntity.Current;
        var user = userLite.Retrieve();

        return new CurrentUserInfo(
            UserId: userLite.Id.ToString(),
            UserLiteKey: userLite.Key(),
            UserName: user.UserName,
            UserRole: user.Role.ToString()!,
            CurrentCulture: CultureInfo.CurrentCulture.Name,
            CurrentUICulture: CultureInfo.CurrentUICulture.Name
        );
    }
}
