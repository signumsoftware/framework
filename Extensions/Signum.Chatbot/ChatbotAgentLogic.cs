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
    public static Dictionary<ChatbotAgentSymbol, ChatbotAgent> Agents = new Dictionary<ChatbotAgentSymbol, ChatbotAgent>();

    public static ResetLazy<Dictionary<string, IChatbotToolFactory>> AllTools;


    public static void Start(SchemaBuilder sb)
    {
        if (sb.NotDefined(MethodBase.GetCurrentMethod()))
        {

            SymbolLogic<ChatbotAgentSymbol>.Start(sb, () => Agents.Keys);


            AllTools = new ResetLazy<Dictionary<string, IChatbotToolFactory>>(() => Agents.SelectMany(a => a.Value.Tools)
            .ToDictionaryEx(a => a.Name, StringComparer.InvariantCultureIgnoreCase));



            IntroductionAgent.Register();
            SumarizerAgent.Register();
            SeachControlAgent.Register();
        }
    }

    public static ChatbotAgent GetAgent(ChatbotAgentSymbol symbol)
    {
        CreateAgentEntitiesIfNecessary();

        var code = Agents.GetOrThrow(symbol);
        var entity = AgentEntities.Value.GetOrThrow(symbol);
        return new ChatbotAgent(symbol, code, entity);
    }

    static object lockKey = new object();
    private static void CreateAgentEntitiesIfNecessary()
    {
        var missing = Agents.Where(kvp => !AgentEntities.Value.ContainsKey(kvp.Key)).ToList();
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

    public static void RegisterAgent(ChatbotAgentSymbol agentSymbol, ChatbotAgent agentCode)
    {
        Agents.Add(agentSymbol, agentCode);
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

        var key = Agents.Keys.FirstOrDefault(a =>
        string.Equals(a.Key, agentName, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(a.Key.After("."), agentName, StringComparison.OrdinalIgnoreCase));

        if (key == null)
            throw new InvalidOperationException($"No Agent '{agentName}' found");

        var result = GetAgent(key).LongDescriptionWithReplacements(promptName);

        return Task.FromResult(result);
    }
}


public class ChatbotAgent
{
    public ChatbotAgentSymbol Symbol;
    public Func<object?, string> GetPrompt;
    public bool IsListedInIntroduction;
    public List<IChatbotToolFactory> Tools = new List<IChatbotToolFactory>();
}
