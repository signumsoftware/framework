using Microsoft.AspNetCore.Mvc.Formatters;
using Signum.Authorization;
using Signum.Engine.Sync;
using Signum.Utilities;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Signum.Chatbot.Agents;

public static class ChatbotAgentLogic
{
    public static Dictionary<ChatbotAgentCodeSymbol, ChatbotAgentCode> AgentCodes = new Dictionary<ChatbotAgentCodeSymbol, ChatbotAgentCode>();
    public static ResetLazy<Dictionary<ChatbotAgentCodeSymbol, ChatbotAgentEntity>> AgentEntities;

    public static ResetLazy<Dictionary<string, Func<CommandArguments, CancellationToken, Task<string>>>> AllResources;


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

            AllResources = new ResetLazy<Dictionary<string, Func<CommandArguments, CancellationToken, Task<string>>>>(() => AgentCodes.SelectMany(a => a.Value.Resources).ToDictionaryEx(StringComparer.InvariantCultureIgnoreCase));

            sb.Schema.EntityEvents<ChatbotAgentCodeSymbol>().PreDeleteSqlSync += symbol =>
            {
                var parts = Administrator.UnsafeDeletePreCommandMList((ChatbotAgentEntity cp) => cp.Descriptions, 
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
        AllResources.Reset();
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
            var action = AllResources.Value.TryGetC(commandName);

            if (action == null)
                throw new InvalidOperationException("Unknown command: " + commandName);

            return await action(args, token);

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
    public Dictionary<string, Func<object?, string>> MessageReplacement = new Dictionary<string, Func<object?, string>>();
    public Dictionary<string, Func<CommandArguments, CancellationToken, Task<string>>> Resources = new Dictionary<string, Func<CommandArguments, CancellationToken, Task<string>>>();
    //public Dictionary<string, Func<Arguments, Task<string>>> Tools = new Dictionary<string, Func<Arguments, Task<string>>>();
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


        return ReplacementRegex.Replace(prompt.Content, a => Code.MessageReplacement.GetOrThrow(a.Groups["replacement"].Value)(context));
    }
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
