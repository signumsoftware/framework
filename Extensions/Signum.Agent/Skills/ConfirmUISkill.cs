using ModelContextProtocol.Server;
using System.ComponentModel;

namespace Signum.Agent.Skills;

public class ConfirmUISkill : AgentSkillCode
{
    public ConfirmUISkill()
    {
        ShortDescription = "Asks the user for confirmation or a choice before proceeding with a sensitive action";
        IsAllowed = () => true;
    }

    [McpServerTool, UITool, Description(
        "Shows an inline confirmation dialog in the chat with a title, message and a set of buttons. " +
        "Returns the label of the button the user clicked. " +
        "Use this before any destructive or irreversible action to get explicit user approval.")]
    public static string Confirm(
        [Description("Short title for the confirmation, e.g. \"Delete order\"")] string title,
        [Description("Full description of what the user is about to confirm")] string message,
        [Description("Labels for the buttons the user can click, e.g. [\"Confirm\", \"Cancel\"]")] string[] buttons)
    {
        throw new InvalidOperationException("This method should not be called on the server");
    }
}
