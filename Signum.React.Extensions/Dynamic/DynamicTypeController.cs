using Signum.Engine;
using Signum.Engine.Basics;
using Signum.Engine.Dynamic;
using Signum.Engine.DynamicQuery;
using Signum.Engine.Maps;
using Signum.Entities;
using Signum.Entities.Basics;
using Signum.Entities.Dynamic;
using Signum.Entities.Reflection;
using Signum.React.Json;
using Signum.Utilities;
using Signum.Utilities.ExpressionTrees;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Web.Http;

namespace Signum.React.Dynamic
{
    public class DynamicTypeController : ApiController
    {
        [Route("api/dynamic/type/propertyType"), HttpPost]
        public string CodePropertyType(DynamicProperty property)
        {
            return DynamicTypeLogic.GetPropertyType(property);
          
        }

        [Route("api/dynamic/type/autocompleteType"), HttpGet]
        public List<string> AutocompleteType(string query, int limit) //Not comprehensive, just useful
        {
            var types = GetTypes();

            var result = types.Where(a => a.StartsWith(query, StringComparison.InvariantCultureIgnoreCase)).OrderBy(a => a.Length).ThenBy(a => a).Take(limit).ToList();

            if (result.Count < limit)
                result.AddRange(types.Where(a => a.Contains(query, StringComparison.InvariantCultureIgnoreCase)).OrderBy(a => a.Length).ThenBy(a => a).Take(result.Count - limit).ToList());

            return result;
        }

        public static List<string> AditionalTypes = new List<string>
        {
            "DateTime",
            "TimeSpan",
            "Guid",
        };

        public List<string> GetTypes()
        {
            var basic = CSharpRenderer.BasicTypeNames.Values;

            var entities =  TypeLogic.TypeToEntity.Keys.Select(a => a.Name);

            return basic.Concat(AditionalTypes).Concat(entities).ToList();
        }
    }
}
