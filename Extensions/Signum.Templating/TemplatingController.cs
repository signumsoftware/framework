using Microsoft.AspNetCore.Mvc;
using Signum.Engine.Templating;
using Signum.React.Facades;
using System.Web;

namespace Signum.React.Extensions.Templating;

public class TemplatingController : ControllerBase
{
    [HttpGet("api/templating/getGlobalVariables")]
    public List<GlobalVariableTS> GetGlobalVariables()
    {
        return GlobalValueProvider.GlobalVariables.Select(kvp => new GlobalVariableTS { Key = kvp.Key, Type = new TypeReferenceTS(kvp.Value.Type, null) }).ToList();
    }
}

public class GlobalVariableTS
{
    public string Key;
    public TypeReferenceTS Type;
}
