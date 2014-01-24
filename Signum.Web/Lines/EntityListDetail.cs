#region usings
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
#endregion

namespace Signum.Web
{
    public class EntityListDetail : EntityListBase
    {
        public readonly RouteValueDictionary ListHtmlProps = new RouteValueDictionary();

        public string DefaultDetailDiv { get; private set; }
        public string DetailDiv { get; set; }

        public EntityListDetail(Type type, object untypedValue, Context parent, string controlID, PropertyRoute propertyRoute)
            : base(type, untypedValue, parent, controlID, propertyRoute)
        {
            DefaultDetailDiv = DetailDiv = this.Compose(EntityBaseKeys.Detail);
            Reorder = false;
        }

        protected override JsOptionsBuilder OptionsJSInternal()
        {
            var result = base.OptionsJSInternal();
            if (DetailDiv.HasText())
                result.Add("detailDiv", DetailDiv.SingleQuote());
            return result;
        }
    }
}
