using ModelContextProtocol.Server;
using System.ComponentModel;

namespace Signum.Chatbot.Agents;

public class AutocompleteSkill : ChatbotSkill
{
    public AutocompleteSkill()
    {
        ShortDescription = "Finds entities by name";
        IsAllowed = () => true;
    }

    [McpServerTool, Description("Returns the lites (entities) of some type that contain subString")]
    public static async Task<List<Lite<Entity>>> AutoCompleteLite(string typeName, string subString, CancellationToken token)
    {
        var types = typeName.Split(",").Select(a => TypeLogic.GetType(a.Trim())).ToArray();

        var entities = await AutocompleteUtils.FindLiteLikeAsync(Implementations.By(types), subString, 5, token);

        return entities;
    }
}
