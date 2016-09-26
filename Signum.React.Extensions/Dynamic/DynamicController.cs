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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Web.Http;

namespace Signum.React.Dynamic
{
    public class DynamicController : ApiController
    {
        [Route("api/dynamic/view/{typeName}"), HttpGet]
        public DynamicViewEntity GetDynamicView(string typeName, string viewName = null)
        {
            Type type = TypeLogic.GetType(typeName);
            var res = DynamicViewLogic.DynamicViews.Value.TryGetC(type)?.SingleOrDefaultEx(a => viewName == null || a.ViewName == viewName);
            return res;
        }

        [Route("api/dynamic/viewNames/{typeName}"), HttpGet]
        public List<string> GetDynamicViewNames(string typeName)
        {
            Type type = TypeLogic.GetType(typeName);
            var res = DynamicViewLogic.DynamicViews.Value.TryGetC(type).EmptyIfNull().Select(a => a.ViewName).ToList();
            return res;
        }

        [Route("api/dynamic/suggestedFindOptions/{typeName}"), HttpGet]
        public List<SuggestedFindOptions> GetSuggestedFindOptions(string typeName)
        {
            Type type = TypeLogic.GetType(typeName);

            return DynamicViewLogic.GetSuggestedFindOptions(type);
        }

        [Route("api/dynamic/selector/{typeName}"), HttpGet]
        public DynamicViewSelectorEntity GetDynamicViewSelector(string typeName)
        {
            return Database.Query<DynamicViewSelectorEntity>().SingleOrDefaultEx(a => a.EntityType.CleanName == typeName);
        }
    }
}
