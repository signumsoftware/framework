using Microsoft.AspNetCore.Http;
using ModelContextProtocol;
using ModelContextProtocol.Server;
using Signum.API;
using Signum.Chart;
using Signum.Chatbot.Agents;
using System.ComponentModel;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Signum.Chatbot.Skills;

public class ChartSkill : ChatbotSkill
{
    public ChartSkill()
    {
        ShortDescription = "Expands the Search skill with charting capabilities";
        IsAllowed = () => true;
    }

    [McpServerTool, Description("Gets the available Chart Scripts")]
    public static Dictionary<string, SimpleChatScript> GetChartScripts()
    {
        return ChartScriptLogic.Scripts.Select(s => new SimpleChatScript
        {
            Key = s.Key.Key.After("."),
            Columns = s.Value.Columns.ToList()
        }).ToDictionary(a => a.Key);
    }

    [McpServerTool, Description("Convert ChartOptions to a url")]
    public static string GetChartUrl(string chartOptions)
    {
        ChartOptions fo = ParseChartOptions(chartOptions);

        return ChartOptionsEncoder.ChartOptionsPath(fo);
    }

    private static ChartOptions ParseChartOptions(string chartOptions)
    {
        ChartOptions co;
        try
        {
            co = JsonSerializer.Deserialize<ChartOptions>(chartOptions, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                IncludeFields = true,
                Converters = {
                    new JsonStringEnumConverter(),
                }
            })!;
        }
        catch (Exception e)
        {
            throw new McpException(e.Message, e);
        }

        var queryName = QueryLogic.ToQueryName(co.QueryName);
        var qd = QueryLogic.Queries.QueryDescription(queryName);

        var error = co.Validate(qd);
        if (error.HasText())
            throw new McpException(error);
        return co;
    }
}

public class ChartOptions
{
    public string QueryName { get; set; }
    public string ChartScript { get; set; }
    public List<FilterOption>? FilterOptions { get; set; }
    public List<ChartColumnOption> ChartColumnOptions { get; set; }


    internal string? Validate(QueryDescription qd)
    {
        var agg = SubTokensOptions.CanAggregate;
        var jsonOptions = SignumServer.JsonSerializerOptions;
        var sb = new StringBuilder();

        var cs = ChartScriptLogic.Scripts.Values.FirstOrDefault(a=>a.Symbol.Key.After(".") == ChartScript);
        if (cs == null)
            sb.AppendLine($"chartScript: '{ChartScript}' is not a valid ChartScript");

        if (FilterOptions != null)
        {
            int i = 0;
            foreach (var f in FilterOptions)
                f.Validate(sb, $"filterOptions[{i}]", qd, agg, jsonOptions);
        }

        if (cs != null)
        {
            if (ChartColumnOptions == null)
                sb.AppendLine($"chartColumnOptions: is required");
            else
            {
                var maxCount = Math.Max(ChartColumnOptions.Count, cs.Columns.Count);
                for (int i = 0; i <  maxCount; i++)
                {
                    var cco = ChartColumnOptions.ElementAtOrDefault(i);
                    var scriptColumn = cs.Columns.ElementAtOrDefault(i);
                    if(scriptColumn == null)
                    {
                        sb.AppendLine($"chartColumnOptions[{i}]: chartScript '{ChartScript}' only has {cs.Columns.Count} columns");
                    }
                    else
                    {
                        if(cco == null)
                        {
                            if(!scriptColumn.IsOptional)
                                sb.AppendLine($"chartColumnOptions[{i}]: chartScript '{ChartScript}' requires a a token for '{cs.Columns[i].Name}'");
                        }
                        else if (scriptColumn.Name != ChartColumnOptions[i].ScriptColumnName)
                        {
                            sb.AppendLine($"chartColumnOptions[{i}].scriptColumnName: '{ChartColumnOptions[i].ScriptColumnName}' does not match the name '{scriptColumn.Name}' of column {i} in chartScript '{ChartScript}'");
                        }
                        else
                        {
                            cco.Validate(sb, $"chartColumnOptions[{i}]", qd, scriptColumn, agg);
                        }
                    }
                }
            }
        }

        if (sb.Length == 0)
            return null;

        return sb.ToString();
    }
}

public class ChartColumnOption
{
    public string ScriptColumnName { get; set; }
    public string? Token { get; set; }
    public int? OrderByIndex { get; set; }
    public OrderType? OrderByType { get; set; }

    internal void Validate(StringBuilder sb, string path, QueryDescription qd, ChartScriptColumn scriptColumn, SubTokensOptions agg)
    {
        var options = SubTokensOptions.CanElement  | agg;
        if (Token == null)
        {
            if (scriptColumn.IsOptional == false)
                sb.AppendLine($"{path}.token (Column '{ScriptColumnName}'): is required");
        }
        else
        {
            var parsedToken = QueryUtils.TryParse(Token, qd, options, out var error);
            if (error.HasText())
            {
                sb.AppendLine($"{path}.token (Column '{ScriptColumnName}'): {error}");
                return;
            }

            if (parsedToken != null && !ChartUtils.IsChartColumnType(parsedToken, scriptColumn.ColumnType))
                sb.AppendLine($"{path}.token (Column '{ScriptColumnName}'): The type of the token '{parsedToken.FullKey()}' is '{parsedToken.Type.Name}', but a '{scriptColumn.ColumnType}' was expected.");


            if(OrderByIndex != null && OrderByIndex < 0)
                sb.AppendLine($"{path}.orderByIndex (Column '{ScriptColumnName}'): should be >= 0");
        }
    }
}

public class ChartOptionsEncoder
{
    public static string ChartOptionsPath(ChartOptions co)
    {
        var query = ChartOptionsPathQuery(co);
        var strQuery = FindOptionsEncoder.ToQueryString(query);
        return "/chart/" + co.QueryName + (string.IsNullOrEmpty(strQuery) ? "" : "?" + strQuery);
    }


    public static Dictionary<string, object?> ChartOptionsPathQuery(ChartOptions co)
    {
        var query = new Dictionary<string, object?>
        {
            ["script"] = co.ChartScript,
        };

        FindOptionsEncoder.EncodeFilters(query, co.FilterOptions);
        EncodeColumns(query, co.ChartColumnOptions);

        return query;
    }

    public static void EncodeColumns(Dictionary<string, object?> query, List<ChartColumnOption>? columns)
    {
        if (columns == null) 
            return;

        for (int i = 0; i < columns.Count; i++)
        {
            var co = columns[i];

            string value =
             (co.OrderByIndex != null
                 ? (co.OrderByIndex.Value.ToString() + (co.OrderByType ==  OrderType.Ascending ? "A" : "D") + "~")
                 : "") +
             (co.Token ?? "");

            query["column" + i] = value;
        }
    }

}
