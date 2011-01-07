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
#endregion

namespace Signum.Web
{
    public class EntityListDetail : EntityListBase
    {
        public string DefaultDetailDiv{get; private set;}
        public string DetailDiv { get; set; }

        public EntityListDetail(Type type, object untypedValue, Context parent, string controlID, PropertyRoute propertyRoute)
            : base(type, untypedValue, parent, controlID, propertyRoute)
        {
            DefaultDetailDiv = DetailDiv = this.Compose(EntityBaseKeys.Detail);
        }

        public override string ToJS()
        {
            return "new EDList(" + this.OptionsJS() + ")";
        }

        protected override JsOptionsBuilder OptionsJSInternal()
        {
            var result = base.OptionsJSInternal();
            if (DetailDiv.HasText())
                result.Add("detailDiv", DetailDiv.SingleQuote());
            return result;
        }

        protected override string DefaultViewing()
        {
            return EntityListDetail.JsViewing(this, DefaultJsViewOptions()).ToJS();
        }

        public static JsInstruction JsViewing(EntityListDetail edlist, JsViewOptions viewOptions)
        {
            if (viewOptions.ControllerUrl == null)
                viewOptions.ControllerUrl = RouteHelper.New().SignumAction("PartialView");

            return new JsInstruction(() => "EDListOnViewing({0})".Formato(",".Combine(
                edlist.ToJS(),
                viewOptions.TryCC(v => v.ToJS()))));
        }

        protected override string DefaultCreating()
        {
            return EntityListDetail.JsCreating(this, DefaultJsViewOptions()).ToJS();
        }

        private static JsInstruction JsCreating(EntityListDetail edlist, JsViewOptions viewOptions)
        {
            if (viewOptions.ControllerUrl == null)
                viewOptions.ControllerUrl = RouteHelper.New().SignumAction("PartialView");

            return new JsInstruction(() => "EDListOnCreating({0})".Formato(",".Combine(
                edlist.ToJS(),
                viewOptions.TryCC(v => v.ToJS()),
                edlist.HasManyImplementations ? RouteHelper.New().SignumAction("GetTypeChooser").SingleQuote() : null)));
        }

        protected override string DefaultFinding()
        {
            return EntityListDetail.JsFinding(this, DefaultJsfindOptions(), DefaultJsViewOptions()).ToJS();
        }

        public static JsInstruction JsFinding(EntityListDetail edlist, JsFindOptions findOptions, JsViewOptions viewOptions)
        {
            return new JsInstruction(() => "EDListOnFinding({0})".Formato(",".Combine(
                edlist.ToJS(),
                findOptions.TryCC(f => f.ToJS()),
                viewOptions.TryCC(v => v.ToJS()),
                edlist.HasManyImplementations ? RouteHelper.New().SignumAction("GetTypeChooser").SingleQuote() : null)));
        }

        protected override string DefaultRemoving()
        {
            return EntityListDetail.JsRemoving(this).ToJS();
        }

        public static JsInstruction JsRemoving(EntityListDetail edlist)
        {
            return new JsInstruction(() => "EDListOnRemoving({0})".Formato(edlist.ToJS()));
        }
    }
}
