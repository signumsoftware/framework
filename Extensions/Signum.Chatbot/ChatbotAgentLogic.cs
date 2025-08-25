using Microsoft.AspNetCore.Mvc.Formatters;
using Signum.Authorization;
using Signum.Chatbot.Agents;
using Signum.Engine.Sync;
using Signum.Utilities;
using System.Runtime.CompilerServices;
using System.Text.Json;
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
                .WithUniqueIndexMList(a => a.Descriptions, mle => new { mle.Parent, mle.Element.PromptName })
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
                var parts = Administrator.UnsafeDeletePreCommandMList((cp) => cp.Descriptions, 
                    Database.MListQuery((ChatbotAgentEntity cp) => cp.Descriptions).Where(mle => mle.Parent.Code.Is(symbol)));
                var parts2 = Administrator.UnsafeDeletePreCommand(Database.Query<ChatbotAgentEntity>().Where(uqp => uqp.Code.Is(symbol)));

                return SqlPreCommand.Combine(Spacing.Simple, parts, parts2);
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



    public static List<ChatbotAgent> GetListedAgents()
    {
        CreateAgentEntitiesIfNecessary();

        return AgentCodes.Where(kvp => kvp.Value.IsListed())
        .Select(kvp => new ChatbotAgent(kvp.Key, kvp.Value, AgentEntities.Value.GetOrThrow(kvp.Key)))
        .ToList();
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


    public static (string commandName, CommandArguments args) ParseCommand(string answer)
    {
        string commandName = answer.After("$").Before("(");
        var args = new CommandArguments(answer.After("(").BeforeLast(")"));

        return (commandName, args);
    }


    public async static Task<string> EvaluateTool(string commandName, CommandArguments args, CancellationToken token)
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

    public static Task<string> GetDescribe(CommandArguments args)
    {
        var agentName = args.GetArgument<string>(0);
        var promptName = args.TryArgumentC<string>(1);

        var key = AgentCodes.Keys.FirstOrDefault(a =>
        string.Equals(a.Key, agentName, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(a.Key.After("."), agentName, StringComparison.OrdinalIgnoreCase));

        if (key == null)
            throw new InvalidOperationException($"No Agent '{agentName}' found");

        var result = GetAgent(key).GetDescribe(promptName);

        return Task.FromResult(result);
    }
}


public class ChatbotAgentCode
{
    public Func<ChatbotAgentEntity> CreateDefaultEntity;
    public Func<bool> IsListed;
    public Dictionary<string, Func<object?, string>> MessageReplacements = new Dictionary<string, Func<object?, string>>();
    public List<IChatbotAgentTool> Tools = new List<IChatbotAgentTool>();
}

public interface IChatbotAgentTool
{
    string Name { get; }
    string? Description { get; }

    object DescribeTool();

    Task<object> DoExecuteAsync();
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

    Task<object> IChatbotAgentTool.DoExecuteAsync()
    {
        throw new NotImplementedException();
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

    public string GetDescribe(string? name, object? context = null)
    {
        if (name.IsNullOrEmpty())
            name = "Default";

        var prompt = Entity.Descriptions.SingleOrDefault(a => a.PromptName.DefaultToNull() == name.DefaultToNull());

        if (prompt == null)
            throw new InvalidOperationException($"No prompt with name '{0}'. Did you mean {Entity.Descriptions.CommaOr(a => a.PromptName)}");


        return ReplacementRegex.Replace(prompt.Content, a => Code.MessageReplacements.GetOrThrow(a.Groups["replacement"].Value)(context));
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

public class CommandArguments
{
    public string RawText;
    public JsonElement JsonArray; 

    public CommandArguments(string rawText)
    {
        RawText = rawText;
        JsonArray = JsonDocument.Parse("[" + rawText + "]").RootElement;
    }

    public T GetArgument<T>(int index)
    {
        return JsonArray[index].ToObject<T>();
    }

    public T? TryArgumentC<T>(int index)
        where T : class
    {
        if (index >= JsonArray.GetArrayLength())
            return null;

        return JsonArray[index].ToObject<T>();
    }

}
