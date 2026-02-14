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
    public static TypeInfoTS GetTypeInfo(string cleanTypeName, CancellationToken token)
    {
        Type type = GetTypeWithHint(cleanTypeName);

        if (type.IsEnumEntity())
            return ReflectionServer.GetEnumTypeInfo(type)!;

        if (type.IsEntity())
            return ReflectionServer.GetEntityTypeInfo(type)!;

        throw new InvalidOperationException("type is not an entity or an enum");
    }

    private static Type GetTypeWithHint(string cleanTypeName)
    {
        var s = Schema.Current;
        Type type;
        try
        {
            type = TypeLogic.GetType(cleanTypeName);
        }
        catch (Exception e)
        {
            StringDistance sd = new StringDistance();
            var similar = from kvp in TypeLogic.NameToType
                          where s.IsAllowed(kvp.Value, inUserInterface: true) == null
                          let dist = sd.SmithWatermanScore(kvp.Key, cleanTypeName)
                          where dist > cleanTypeName.Length
                          orderby dist descending
                          select kvp.Key;

            if (similar.Any())
                e.Data["Hint"] = $"Similar type names are {similar}";

            throw;
        }

        s.AssertAllowed(type, inUserInterface: true);
        return type;
    }

    [McpServerTool, Description("Construct an entity using an operation")]
    public static EntityPackTS Operation_Construct(string typeName, string operationKey, CancellationToken token)
    {
        var type = GetTypeWithHint(typeName);

        var operation = SymbolLogic<OperationSymbol>.ToSymbol(operationKey);

        var newEntity = OperationLogic.ServiceConstruct(type, operation);

        var canExecutes = OperationLogic.ServiceCanExecute(newEntity);

        var result = new EntityPackTS(newEntity,
            canExecutes.ToDictionary(a => a.Key.Key, a => a.Value));

        return result;
    }

    [McpServerTool, Description("Construct an entity from another entity using an operation")]
    public static EntityPackTS Operation_ConstructFrom(string entityJson, string operationKey, CancellationToken token)
    {
        Entity entity = DeserializeEntity(entityJson);

        var operation = SymbolLogic<OperationSymbol>.ToSymbol(operationKey);

        var newEntity = OperationLogic.ServiceConstructFrom(entity, operation);

        var canExecutes = OperationLogic.ServiceCanExecute(newEntity);

        var result = new EntityPackTS(newEntity,
            canExecutes.ToDictionary(a => a.Key.Key, a => a.Value));

        return result;
    }

    private static Entity DeserializeEntity(string entityJson)
    {
        return JsonSerializer.Deserialize<Entity>(entityJson, SignumServer.JsonSerializerOptions)!;
    }

    [McpServerTool, Description("Executes an operation on an entity")]
    public static EntityPackTS Operation_Execute(string entityJson, string operationKey, CancellationToken token)
    {
        var entity = DeserializeEntity(entityJson)!;

        var operation = SymbolLogic<OperationSymbol>.ToSymbol(operationKey);

        var newEntity = OperationLogic.ServiceExecute(entity, operation);

        var canExecutes = OperationLogic.ServiceCanExecute(newEntity);

        var result = new EntityPackTS(newEntity,
            canExecutes.ToDictionary(a => a.Key.Key, a => a.Value));

        return result;
    }

    [McpServerTool, Description("Executes an operation on an entity")]
    public static void Operation_Delete(string entityJson, string operationKey, CancellationToken token)
    {
        var entity = DeserializeEntity(entityJson)!;

        var operation = SymbolLogic<OperationSymbol>.ToSymbol(operationKey);

        OperationLogic.ServiceDelete(entity, operation);
    }
}
