using ModelContextProtocol.Server;
using Signum.Authorization;
using System.ComponentModel;
using System.Globalization;

namespace Signum.Agent.Skills;

public class CurrentServerContextSkill : AgentSkill
{
    public CurrentServerContextSkill()
    {
        ShortDescription = "Returns the server context including date information, user information and url of the application";
        IsAllowed = () => true;
    }

    public record CurrentServerContext(
        CurrentDateInfo DateInfo,
        CurrentUserInfo UserInfo,
        CurrentCultureInfo Culture, 
        string? UrlPrefix
    );

    public record CurrentDateInfo(
        string LocalDateTime,
        string UtcDateTime,
        string TimeZoneId,
        string TimeZoneOffsetUtc
    );

    public record CurrentUserInfo(
       string UserId,
       string UserLiteKey,
       string UserName,
       string UserRole
    );

    public record CurrentCultureInfo(
       string CurrentCulture,
       string CurrentUICulture
    );

    [McpServerTool, Description("Returns the current local date/time, UTC date/time, and server time zone")]
    public static CurrentServerContext GetCurrentServerContext()
    {
        var now = DateTime.Now;
        var utcNow = DateTime.UtcNow;
        var tz = TimeZoneInfo.Local;

        var userLite = UserEntity.Current;
        var user = userLite.Retrieve();

        return new CurrentServerContext(
            new CurrentDateInfo(
            LocalDateTime: now.ToString("O"),
            UtcDateTime: utcNow.ToString("O"),
            TimeZoneId: tz.Id,
            TimeZoneOffsetUtc: tz.GetUtcOffset(now).ToString()
        ), new CurrentUserInfo(
            UserId: userLite.Id.ToString(),
            UserLiteKey: userLite.Key(),
            UserName: user.UserName,
            UserRole: user.Role.ToString()!
            ),
            new CurrentCultureInfo(
            CurrentCulture: CultureInfo.CurrentCulture.Name,
            CurrentUICulture: CultureInfo.CurrentUICulture.Name
            ),
            UrlPrefix: ChatbotLogic.UrlLeft()
        );
    }
}
