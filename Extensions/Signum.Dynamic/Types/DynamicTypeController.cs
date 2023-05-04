using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using Signum.Dynamic.Expression;

namespace Signum.Dynamic.Types;

public class DynamicTypeController : ControllerBase
{
    [HttpPost("api/dynamic/type/propertyType")]
    public string CodePropertyType([Required, FromBody] DynamicProperty property)
    {
        return DynamicTypeLogic.GetPropertyType(property);

    }

    [HttpGet("api/dynamic/type/expressionNames/{typeName}")]
    public List<string> ExpressionNames(string typeName)
    {
        if (!Schema.Current.Tables.ContainsKey(typeof(DynamicExpressionEntity)))
            return new List<string>();

        return Database.Query<DynamicExpressionEntity>().Where(a => a.FromType == typeName).Select(a => a.Name).ToList();
    }
}
