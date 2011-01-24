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
            return "new SF.ERep(" + this.OptionsJS() + ")";
        }

        protected override JsOptionsBuilder OptionsJSInternal()
        {
            var result = base.OptionsJSInternal();
            result.Add("maxElements", MaxElements.TryToString());
            if (RemoveElementLinkText.HasText())
                result.Add("removeItemLinkText", RemoveElementLinkText.SingleQuote());
            return result;
        }

        protected override string DefaultView()
        {
            return null;
        }

        protected override string DefaultCreate()
        {
            return EntityRepeater.JsCreate(this, DefaultJsViewOptions()).ToJS();
        }

        public static JsInstruction JsCreate(EntityRepeater erep, JsViewOptions viewOptions)
        {
            if (viewOptions.ControllerUrl == null)
                viewOptions.ControllerUrl = RouteHelper.New().SignumAction("PartialView");

            string createParams = ",".Combine(
                viewOptions.TryCC(v => v.ToJS()),
                erep.HasManyImplementations ? RouteHelper.New().SignumAction("GetTypeChooser").SingleQuote() : null);

            return new JsInstruction(() => "{0}.create({1})".Formato(erep.ToJS(), createParams));
        }

        protected override string DefaultFind()
        {
            return EntityRepeater.JsFind(this, DefaultJsfindOptions(), DefaultJsViewOptions()).ToJS();
        }

        public static JsInstruction JsFind(EntityRepeater erep, JsFindOptions findOptions, JsViewOptions viewOptions)
        {
            string findParams = ",".Combine(
                findOptions.TryCC(v => v.ToJS()),
                viewOptions.TryCC(v => v.ToJS()),
                erep.HasManyImplementations ? RouteHelper.New().SignumAction("GetTypeChooser").SingleQuote() : null);

            return new JsInstruction(() => "{0}.find({1})".Formato(erep.ToJS(), findParams));
        }

        protected override string DefaultRemove()
        {
            return null;
        }
    }
}
