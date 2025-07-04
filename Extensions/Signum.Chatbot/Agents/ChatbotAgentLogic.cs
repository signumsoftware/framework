using Microsoft.AspNetCore.Mvc.Formatters;
using Signum.Authorization;
using Signum.Utilities;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace Signum.Chatbot.Agents;

public static class ChatbotAgentLogic
{
    public static Dictionary<ChatbotAgentTypeSymbol, ChatbotAgentCode> AgentCodes = new Dictionary<ChatbotAgentTypeSymbol, ChatbotAgentCode>();
    public static ResetLazy<Dictionary<ChatbotAgentTypeSymbol, ChatbotAgentEntity>> AgentEntities;


    public static void Start(SchemaBuilder sb)
    {
        if (sb.NotDefined(MethodBase.GetCurrentMethod()))
        {
            sb.Include<ChatbotAgentEntity>()
                .WithSave(ChatbotAgentOperation.Save)
                .WithDelete(ChatbotAgentOperation.Delete)
                .WithUniqueIndexMList(a => a.ChatbotPrompts, mle => new { mle.Parent, mle.Element.PromptName })
                .WithQuery(() => e => new
                {
                    Entity = e,
                    e.Id,
                    e.Key,
                    e.ShortDescription,
                });

            SymbolLogic<ChatbotAgentTypeSymbol>.Start(sb, () => AgentCodes.Keys);

            AgentEntities = sb.GlobalLazy(() => Database.Query<ChatbotAgentEntity>().ToDictionaryEx(a => a.Key), new InvalidateWith(typeof(ChatbotAgentEntity)));

            IntroductionAgent.Register();
            SumarizerAgent.Register();
        }
    }

    public static ChatbotAgent GetAgent(ChatbotAgentTypeSymbol symbol)
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
                        entity.Key = a.Key;
                        return entity;
                    }).SaveList();
                }
        }
    }

    public static void RegisterAgent(ChatbotAgentTypeSymbol agentSymbol, ChatbotAgentCode agentCode)
    {
        AgentCodes.Add(agentSymbol, agentCode);
    }
}


public class ChatbotAgentCode
{
    public Func<ChatbotAgentEntity> CreateDefaultEntity;
    public Func<bool> IsListed;
    public Dictionary<string, Func<object?, string>> MessageReplacement = new Dictionary<string, Func<object?, string>>();
    public Dictionary<string, Func<Arguments, string>> Resources = new Dictionary<string, Func<Arguments, string>>();
    public Dictionary<string, Func<Arguments, string>> Tools = new Dictionary<string, Func<Arguments, string>>();
}

public class ChatbotAgent
{
    public ChatbotAgentTypeSymbol Symbol;
    public ChatbotAgentCode Code;
    public ChatbotAgentEntity Entity;

    public ChatbotAgent(ChatbotAgentTypeSymbol symbol, ChatbotAgentCode code, ChatbotAgentEntity entity)
    {
        Symbol = symbol;
        Code = code;
        Entity = entity;
    }

    static Regex ReplacementRegex = new Regex(@"\$<(?<replacement>\w+)>");

    public string GetPrompt(string? name, object? context = null)
    {
        var prompt = Entity.ChatbotPrompts.SingleOrDefault(a => a.PromptName == name);

        if (prompt == null)
            throw new InvalidOperationException($"No prompt with name '{0}'. Did you mean {Entity.ChatbotPrompts.CommaOr(a => a.PromptName)}");


        return ReplacementRegex.Replace(prompt.Content, a => Code.MessageReplacement.GetOrThrow(a.Groups["replacement"].Value)(context));
    }
}

public class Arguments : Dictionary<string, string>
{


}
