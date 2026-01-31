using ModelContextProtocol.Server;
using Signum.API;
using System.ComponentModel;
using System.Reflection.Metadata;

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
        var type = TypeLogic.GetType(typeName);

        var entity = await Database.RetrieveAsync(type, PrimaryKey.Parse(id, type), token);

        var canExecutes = OperationLogic.ServiceCanExecute(entity);

        var result = new EntityPackTS(entity,
            canExecutes.ToDictionary(a => a.Key.Key, a => a.Value));

        return result;
    }
}
