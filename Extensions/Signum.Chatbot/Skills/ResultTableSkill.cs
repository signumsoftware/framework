using ModelContextProtocol.Server;
using System.ComponentModel;

namespace Signum.Chatbot.Agents;

public class ResultTableSkill : ChatbotSkill
{
    public ResultTableSkill()
    {
        ShortDescription = "Actually executes a query returning the results to the LLM";
        IsAllowed = () => true;
    }

    [McpServerTool, Description("Executes a FindOptions and returns a dynamic ResultTable")]
    public static ResultTableSimple GetResultTable(string findOptionsJson)
    {
        FindOptions fo = SearchSkill.ParseFindOptions(findOptionsJson);

        var qr = fo.ToQueryRequest();

        var rt = QueryLogic.Queries.ExecuteQuery(qr);

        return new ResultTableSimple
        {
            Columns = rt.Columns.Select((a, i) => KeyValuePair.Create("c" + i,  a.Token.FullKey())).ToDictionary(),
            Rows = rt.Rows.Select(r => rt.Columns.ToDictionary(c => "c" + c.Index, c => r[c.Index])).ToList(),
        };
    }
}

public class ResultTableSimple
{
    public Dictionary<string, string> Columns { get; internal set; }

    public List<Dictionary<string, object?>> Rows { get; internal set; }

}
