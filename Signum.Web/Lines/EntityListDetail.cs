using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using System.Collections;
using System.Linq.Expressions;
using System.Web.Mvc.Html;
using Signum.Entities;
using Signum.Entities.Reflection;
using Signum.Utilities;
using System.Configuration;
using System.Web.Routing;
using Newtonsoft.Json.Linq;

namespace Signum.Web
{
    public class EntityListDetail : EntityListBase
    {
        public readonly RouteValueDictionary ListHtmlProps = new RouteValueDictionary();

        public string DetailDiv { get; set; }

        public EntityListDetail(Type type, object untypedValue, Context parent, string prefix, PropertyRoute propertyRoute)
            : base(type, untypedValue, parent, prefix, propertyRoute)
        {
            DetailDiv = this.Compose(EntityBaseKeys.Detail);
            Move = false;
        }

        protected override Dictionary<string, object> OptionsJSInternal()
        {
            var result = base.OptionsJSInternal();
            if (DetailDiv.HasText())
                result.Add("detailDiv", DetailDiv);
            return result;
        }
    }
}
