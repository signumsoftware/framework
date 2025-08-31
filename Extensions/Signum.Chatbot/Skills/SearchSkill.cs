using Signum.API.Controllers;
using Signum.DynamicQuery.Tokens;
using System.ComponentModel;
using System.Text.Json;

namespace Signum.Chatbot.Agents;

public class SearchSkill : ChatbotSkill
{
    public SearchSkill()
    {
        ShortDescription = "Explores the database schema to search any information in the database";
        IsAllowed = () => true;
        Replacements = new Dictionary<string, Func<object?, string>>()
        {
            { "<LIST_ROOT_QUERIES>", obj => QueryLogic.Queries.GetAllowedQueryNames(fullScreen: true)
                    .ToString(a => $"* {QueryUtils.GetKey(a)}: {QueryUtils.GetNiceName(a)}", "\n")
            }
        };
    }

    [SkillTool, Description("Gets query description")]
    public QueryDescriptionTS QueryDescription(string queryKey)
    {
        var qn = QueryLogic.ToQueryName(queryKey);
        var description = QueryLogic.Queries.QueryDescription(qn);
        var result = new QueryDescriptionTS(description);
        return result;
    }
}
