using Microsoft.AspNetCore.Mvc;
using Signum.API;

namespace Signum.Templating;

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
