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
using Signum.Engine;
#endregion

namespace Signum.Web
{
    public static class EntityRepeaterKeys
    {
        public const string ItemsContainer = "sfItemsContainer";
        public const string RepeaterElement = "sfRepeaterItem";
    }

    public class EntityRepeater : EntityListBase
    {
        public int? MaxElements { get; set; }

        public EntityRepeater(Type type, object untypedValue, Context parent, string controlID, PropertyRoute propertyRoute)
            : base(type, untypedValue, parent, controlID, propertyRoute)
        {
            Find = false;
            LabelClass = "sf-label-repeater-line";
            Reorder = false;
        }

        protected override JsOptionsBuilder OptionsJSInternal()
        {
            var result = base.OptionsJSInternal();
            result.Add("maxElements", MaxElements.TryToString());
            return result;
        }
    }
}
