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
using Signum.Web.Properties;
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
        public string AddElementLinkText { get; set; }
        public string RemoveElementLinkText{get;set;}

        public int? MaxElements { get; set; }

        public EntityRepeater(Type type, object untypedValue, Context parent, string controlID, PropertyRoute propertyRoute)
            : base(type, untypedValue, parent, controlID, propertyRoute)
        {
            RemoveElementLinkText = Resources.Remove;
            AddElementLinkText = Resources.New;
            Find = false;
        }

        public override string ToJS()
        {
            return "new ERep(" + this.OptionsJS() + ")";
        }

        protected override JsOptionsBuilder OptionsJSInternal()
        {
            var result = base.OptionsJSInternal();
            result.Add("maxElements", MaxElements.TryToString());
            if (RemoveElementLinkText.HasText())
                result.Add("removeItemLinkText", RemoveElementLinkText.Quote());
            return result;
        }

        protected override string DefaultViewing()
        {
            return null;
        }

        protected override string DefaultCreating()
        {
            return EntityRepeater.JsCreating(this, DefaultJsViewOptions()).ToJS();
        }

        public static JsInstruction JsCreating(EntityRepeater erep, JsViewOptions viewOptions)
        {
            return new JsInstruction(() => "ERepOnCreating({0})".Formato(",".Combine(
                erep.ToJS(),
                viewOptions.TryCC(v => v.ToJS()))));
        }

        protected override string DefaultFinding()
        {
            return EntityRepeater.JsFinding(this, DefaultJsfindOptions(), DefaultJsViewOptions()).ToJS();
        }

        public static JsInstruction JsFinding(EntityRepeater erep, JsFindOptions findOptions, JsViewOptions viewOptions)
        {
            return new JsInstruction(() => "ERepOnFinding({0})".Formato(",".Combine(
                erep.ToJS(),
                findOptions.TryCC(f => f.ToJS())),
                viewOptions.TryCC(v => v.ToJS())));
        }

        protected override string DefaultRemoving()
        {
            return null;
        }
    }
}
