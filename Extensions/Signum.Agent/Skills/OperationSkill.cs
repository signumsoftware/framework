using ModelContextProtocol.Server;
using Signum.API;
using System.ComponentModel;
using System.Text.Json;

namespace Signum.Agent.Skills;

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
        try
        {
            var type = TypeLogic.GetType(cleanTypeName);

            if (type.IsEnumEntity())
                return ReflectionServer.GetEnumTypeInfo(type);

            if (type.IsEntity())
                return ReflectionServer.GetEntityTypeInfo(type);

            throw new InvalidOperationException("type is not an entity or an enum");
        }
        catch (Exception e)
        {
            AddTypeNameHint(e, cleanTypeName);
            throw;
        }
    }

    [McpServerTool, Description("Executes an operation on an entity")]
    public static EntityPackTS ExecuteOperation(string entityJson, string operationKey, CancellationToken token)
    {
        try
        {
            var entity = JsonSerializer.Deserialize<Entity>(entityJson, SignumServer.JsonSerializerOptions)!;

            var operation = SymbolLogic<OperationSymbol>.ToSymbol(operationKey);

            var newEntity = OperationLogic.ServiceExecute(entity, operation);

            var canExecutes = OperationLogic.ServiceCanExecute(newEntity);

            var result = new EntityPackTS(newEntity,
                canExecutes.ToDictionary(a => a.Key.Key, a => a.Value));

            return result;
        }
        catch (Exception e) when (e is not InvalidOperationException)
        {
            if (e is UnauthorizedAccessException || e.Message.Contains("not allowed", StringComparison.OrdinalIgnoreCase))
                e.Data["Hint"] = "You don't have permission for this operation. Try a different approach or ask the user.";

            if (e is JsonException)
                e.Data["Hint"] = "The entity JSON format appears invalid. Make sure it matches the expected schema from GetTypeInfo.";

            throw;
        }
    }

    static void AddTypeNameHint(Exception e, string cleanTypeName)
    {
        var similar = TypeLogic.NameToType.Keys
            .Where(k => k.Contains(cleanTypeName, StringComparison.OrdinalIgnoreCase))
            .Take(5)
            .ToList();

        if (similar.Any())
            e.Data["Hint"] = $"Similar type names: {similar.ToString(", ")}";
    }
}
