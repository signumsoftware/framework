using ModelContextProtocol.Server;
using Signum.API;
using System.ComponentModel;
using System.Reflection.Metadata;
using System.Text.Json;

namespace Signum.Chatbot.Agents;

public class OperationSkill : ChatbotSkill
{
    public OperationSkill()
    {
        ShortDescription = "Executes operations in an entity";
        IsAllowed = () => true;
    }

    [McpServerTool, Description("Gets the type information of a type")]
    public static TypeInfoTS? GetTypeInfo(string cleanTypeName, CancellationToken token)
    {
        var type = TypeLogic.GetType(cleanTypeName);

        if (type.IsEnumEntity())
            return ReflectionServer.GetEnumTypeInfo(type);

        if (type.IsEntity())
            return ReflectionServer.GetEntityTypeInfo(type);

        throw new InvalidOperationException("type is not an entity or an enum");
    }

    [McpServerTool, Description("Executes an operation on an entity")]
    public static  EntityPackTS ExecuteOperation(string entityJson, string operationKey, CancellationToken token)
    {
        var entity = JsonSerializer.Deserialize<Entity>(entityJson, SignumServer.JsonSerializerOptions)!;

        var operation = SymbolLogic<OperationSymbol>.ToSymbol(operationKey);

        var newEntity = OperationLogic.ServiceExecute(entity, operation);

        var canExecutes = OperationLogic.ServiceCanExecute(newEntity);

        var result = new EntityPackTS(newEntity,
            canExecutes.ToDictionary(a => a.Key.Key, a => a.Value));

        return result;
    }
}
