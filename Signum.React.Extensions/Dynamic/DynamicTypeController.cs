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

        [Route("api/dynamic/type/expressionNames/{typeName}"), HttpGet]
        public List<string> ExpressionNames(string typeName)
        {
            if (!Schema.Current.Tables.ContainsKey(typeof(DynamicExpressionEntity)))
                return new List<string>();

            return Database.Query<DynamicExpressionEntity>().Where(a => a.FromType == typeName).Select(a => a.Name).ToList();
        }
    }
}
