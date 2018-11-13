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
using Microsoft.AspNetCore.Mvc;
using Signum.React.ApiControllers;

namespace Signum.React.Dynamic
{
    public class DynamicViewController : ControllerBase
    {
        [HttpGet("api/dynamic/view/{typeName}")]
        public DynamicViewEntity GetDynamicView(string typeName, string viewName)
        {
            Type type = TypeLogic.GetType(typeName);
            var res = DynamicViewLogic.DynamicViews.Value.GetOrThrow(type).GetOrThrow(viewName);
            return res;
        }

        [HttpGet("api/dynamic/viewNames/{typeName}")]
        public List<string> GetDynamicViewNames(string typeName)
        {
            Type type = TypeLogic.GetType(typeName);
            var res = DynamicViewLogic.DynamicViews.Value.TryGetC(type).EmptyIfNull().Select(a => a.Key).ToList();
            return res;
        }

        [HttpGet("api/dynamic/suggestedFindOptions/{typeName}")]
        public List<SuggestedFindOptions> GetSuggestedFindOptions(string typeName)
        {
            Type type = TypeLogic.GetType(typeName);

            return DynamicViewLogic.GetSuggestedFindOptions(type);
        }

        [HttpGet("api/dynamic/selector/{typeName}")]
        public DynamicViewSelectorEntity GetDynamicViewSelector(string typeName)
        {
            Type type = TypeLogic.GetType(typeName);
            return DynamicViewLogic.DynamicViewSelectors.Value.TryGetC(type);
        }

        [HttpGet("api/dynamic/override/{typeName}")]
        public List<DynamicViewOverrideEntity> GetDynamicViewOverride(string typeName)
        {
            Type type = TypeLogic.GetType(typeName);
            return DynamicViewLogic.DynamicViewOverrides.Value.TryGetC(type) ?? new List<DynamicViewOverrideEntity>();
        }
    }
}
