using ModelContextProtocol.Server;
using Signum.API;
using System.ComponentModel;

namespace Signum.Agent.Skills;

public class RetrieveSkill : ChatbotSkill
{
    public RetrieveSkill()
    {
        ShortDescription = "Retrieves full entity by name";
        IsAllowed = () => true;
    }

    [McpServerTool, Description("Returns a full entity (and his can executes) given its type and id")]
    public static async Task<EntityPackTS> RetrieveEntity(string typeName, string id, CancellationToken token)
    {
        try
        {
            var type = TypeLogic.GetType(typeName);

            var entity = await Database.RetrieveAsync(type, PrimaryKey.Parse(id, type), token);

            var canExecutes = OperationLogic.ServiceCanExecute(entity);

            var result = new EntityPackTS(entity,
                canExecutes.ToDictionary(a => a.Key.Key, a => a.Value));

            return result;
        }
        catch (Exception e)
        {
            AddTypeNameHint(e, typeName);

            if (e is EntityNotFoundException)
                e.Data["Hint"] = (e.Data["Hint"] as string).DefaultToNull() is string existing
                    ? existing + "\n" + "The entity with that ID does not exist. Check the ID or use a search to find the correct one."
                    : "The entity with that ID does not exist. Check the ID or use a search to find the correct one.";

            throw;
        }
    }

    static void AddTypeNameHint(Exception e, string typeName)
    {
        var similar = TypeLogic.NameToType.Keys
            .Where(k => k.Contains(typeName, StringComparison.OrdinalIgnoreCase))
            .Take(5)
            .ToList();

        if (similar.Any())
            e.Data["Hint"] = $"Similar type names: {similar.ToString(", ")}";
    }
}
