using ModelContextProtocol.Server;
using System.ComponentModel;

namespace Signum.Agent.Skills;

public class GetUIContextSkill : AgentSkillCode
{
    public GetUIContextSkill()
    {
        ShortDescription = "Retrieves context information from the user's browser (current URL, language, screen size)";
        IsAllowed = () => true;
    }

    [McpServerTool, UITool, Description("Requests the current browser context from the UI (URL, language, screen dimensions). " +
        "Call this at the start of tasks where knowing the user's current page or locale is relevant.")]
    public static object GetUIContext()
    {
        throw new InvalidOperationException("This method should not be called on the server");
    }
}
