using ModelContextProtocol.Server;
using System.ComponentModel;

namespace Signum.Agent.Skills;

public class CurrentDateSkill : ChatbotSkill
{
    public CurrentDateSkill()
    {
        ShortDescription = "Returns the current local date/time, UTC date/time, and server time zone";
        IsAllowed = () => true;
    }

    public record CurrentDateInfo(
        string LocalDateTime,
        string UtcDateTime,
        string TimeZoneId,
        string TimeZoneOffsetUtc
    );

    [McpServerTool, Description("Returns the current local date/time, UTC date/time, and server time zone")]
    public static CurrentDateInfo GetCurrentDate()
    {
        var now = DateTime.Now;
        var utcNow = DateTime.UtcNow;
        var tz = TimeZoneInfo.Local;

        return new CurrentDateInfo(
            LocalDateTime: now.ToString("O"),
            UtcDateTime: utcNow.ToString("O"),
            TimeZoneId: tz.Id,
            TimeZoneOffsetUtc: tz.GetUtcOffset(now).ToString()
        );
    }
}
