using Microsoft.AspNetCore.Mvc.Formatters;
using Signum.Authorization;
using Signum.Chatbot.Agents;
using Signum.Engine.Sync;
using Signum.Utilities;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Signum.Chatbot;

public static class ChatbotAgentLogic
{
    public static Dictionary<ChatbotAgentCodeSymbol, ChatbotAgentCode> AgentCodes = new Dictionary<ChatbotAgentCodeSymbol, ChatbotAgentCode>();
    public static ResetLazy<Dictionary<ChatbotAgentCodeSymbol, ChatbotAgentEntity>> AgentEntities;

    public static ResetLazy<Dictionary<string, IChatbotAgentTool>> AllTools;


    public static void Start(SchemaBuilder sb)
    {
        if (sb.NotDefined(MethodBase.GetCurrentMethod()))
        {
            sb.Include<ChatbotAgentEntity>()
                .WithSave(ChatbotAgentOperation.Save)
                .WithDelete(ChatbotAgentOperation.Delete)
                .WithQuery(() => e => new
                {
                    Entity = e,
                    e.Id,
                    e.Code,
                    e.ShortDescription,
                });


            SymbolLogic<ChatbotAgentCodeSymbol>.Start(sb, () => AgentCodes.Keys);

            AgentEntities = sb.GlobalLazy(() => Database.Query<ChatbotAgentEntity>().ToDictionaryEx(a => a.Code), new InvalidateWith(typeof(ChatbotAgentEntity)));

            AllTools = new ResetLazy<Dictionary<string, IChatbotAgentTool>>(() => AgentCodes.SelectMany(a => a.Value.Tools)
            .ToDictionaryEx(a => a.Name, StringComparer.InvariantCultureIgnoreCase));

            sb.Schema.EntityEvents<ChatbotAgentCodeSymbol>().PreDeleteSqlSync += symbol =>
            {
                return Administrator.UnsafeDeletePreCommand(Database.Query<ChatbotAgentEntity>().Where(uqp => uqp.Code.Is(symbol)));
            };

            IntroductionAgent.Register();
            SumarizerAgent.Register();
            SeachControlAgent.Register();
        }
    }

    public static ChatbotAgent GetAgent(ChatbotAgentCodeSymbol symbol)
    {
        CreateAgentEntitiesIfNecessary();

        var code = AgentCodes.GetOrThrow(symbol);
        var entity = AgentEntities.Value.GetOrThrow(symbol);
        return new ChatbotAgent(symbol, code, entity);
    }

    static object lockKey = new object();
    private static void CreateAgentEntitiesIfNecessary()
    {
        var missing = AgentCodes.Where(kvp => !AgentEntities.Value.ContainsKey(kvp.Key)).ToList();
        if (missing.Any())
        {
            lock (lockKey)
                using (AuthLogic.Disable())
                using (OperationLogic.AllowSave<ChatbotAgentEntity>())
                {
                    missing.Select(a =>
                    {
                        var entity = a.Value.CreateDefaultEntity();
                        entity.Code = a.Key;
                        return entity;
                    }).SaveList();
                }
        }
    }

    public static void RegisterAgent(ChatbotAgentCodeSymbol agentSymbol, ChatbotAgentCode agentCode)
    {
        AgentCodes.Add(agentSymbol, agentCode);
        AllTools.Reset();
    }


    public async static Task<string> EvaluateTool(string commandName, JsonDocument args, CancellationToken token)
    {   
        try
        {
            var action = AllTools.Value.TryGetC(commandName);

            if (action == null)
                throw new InvalidOperationException("Unknown command: " + commandName);

            throw new InvalidOperationException();
            //return await action.DoExecuteAsync(args, token);

        }
        catch (Exception e)
        {
            if(commandName.HasText())
                return $"Error evaluating command '{commandName}'!\n{e.GetType().Name}: {e.Message}";
            else
                return $"Error evaluating command!\n{e.GetType().Name}: {e.Message}";
        }
    }

    public static Task<string> GetDescribe(JsonDocument args)
    {
        var agentName = args.RootElement.gets<string>(0);

        var key = AgentCodes.Keys.FirstOrDefault(a =>
        string.Equals(a.Key, agentName, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(a.Key.After("."), agentName, StringComparison.OrdinalIgnoreCase));

        if (key == null)
            throw new InvalidOperationException($"No Agent '{agentName}' found");

        var result = GetAgent(key).LongDescriptionWithReplacements(promptName);

        return Task.FromResult(result);
    }
}


public class ChatbotAgentCode
{
    public Func<ChatbotAgentEntity> CreateDefaultEntity;
    public Func<bool> IsListedInIntroduction;
    public Dictionary<string, Func<object?, string>> MessageReplacements = new Dictionary<string, Func<object?, string>>();
    public List<IChatbotAgentTool> Tools = new List<IChatbotAgentTool>();
}

public interface IChatbotAgentTool
{
    string Name { get; }
    string? Description { get; }

    object DescribeTool();

    Task<string> DoExecuteAsync(JsonDocument arguments, CancellationToken token);
}

public interface IToolPayload { } //To prevent simple types

public class ChatbotAgentTool<Request, Response> : IChatbotAgentTool
    where Request : IToolPayload
    where Response : IToolPayload
{
    public ChatbotAgentTool(string name)
    {
        Name = name;
    }

    public string Name { get; set; }

    public required string Description { get; set; }
    public required Func<Request, CancellationToken, Task<Response>> Execute { get; set; }

    public object DescribeTool() => new
    {
        type = "function",
        name = Name,
        description = Description,
        strict = true,
        parameters = JSonSchema.For(typeof(Request)),
    };


    Task<string> IChatbotAgentTool.DoExecuteAsync(JsonDocument arguments, CancellationToken token)
    {
        throw new InvalidOperationException();
        //return DoExecuteAsync(arguments, token);
    }
}

public static class JSonSchema
{
    public static object For(Type type) => ForInternal(type, new HashSet<Type>());
    public static object ForInternal(Type type, HashSet<Type> visited)
    {
        if (type == typeof(string))
            return new { type = "string" };

        if (type == typeof(int) || type == typeof(long) || type == typeof(short))
            return new { type = "integer" };

        if (type == typeof(float) || type == typeof(double) || type == typeof(decimal))
            return new { type = "number" };

        if (type == typeof(bool))
            return new { type = "boolean" };

        if (type == typeof(DateTime) || type == typeof(DateTimeOffset))
            return new { type = "string", format = "date-time" };

        if (type.IsEnum)
            return new
            {
                type = "string",
                enumValues = Enum.GetNames(type)
            };

        if (typeof(System.Collections.IEnumerable).IsAssignableFrom(type) && type != typeof(string))
        {
            var elementType = type.IsArray
                ? type.GetElementType()!
                : type.GetGenericArguments().FirstOrDefault() ?? typeof(object);

            return new
            {
                type = "array",
                items = ForInternal(elementType, visited)
            };
        }

        // Avoid infinite recursion
        if (visited.Contains(type))
            return new { type = "object" };

        visited.Add(type);

        // Default: treat as object with properties
        var props = type
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead)
            .ToDictionary(
                p => p.Name,
                p => ForInternal(p.PropertyType, visited)
            );

        return new
        {
            type = "object",
            properties = props
        };
    }
}

public class ChatbotAgent
{
    public ChatbotAgentCodeSymbol Symbol;
    public ChatbotAgentCode Code;
    public ChatbotAgentEntity Entity;

    public ChatbotAgent(ChatbotAgentCodeSymbol symbol, ChatbotAgentCode code, ChatbotAgentEntity entity)
    {
        Symbol = symbol;
        Code = code;
        Entity = entity;
    }

    static Regex ReplacementRegex = new Regex(@"\$<(?<replacement>\w+)>");

    public string LongDescriptionWithReplacements(object? context = null)
    {
        return ReplacementRegex.Replace(Entity.LongDescription, a => Code.MessageReplacements.GetOrThrow(a.Groups["replacement"].Value)(context));
    }

    internal AgentInfo ToInfo() => new AgentInfo
    {
        tools = this.Code.Tools.Select(a => a.Name).ToArray()
    };
}

public class AgentInfo
{
    public string[] tools;
}
