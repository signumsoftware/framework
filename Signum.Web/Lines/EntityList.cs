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
    public class EntityList : EntityListBase
    {
        public EntityList(Type type, object untypedValue, Context parent, string controlID, PropertyRoute propertyRoute)
            : base(type, untypedValue, parent, controlID, propertyRoute)
        {
        }

        public override string ToJS()
        {
            return "new EList(" + this.OptionsJS() + ")";
        }

        protected override JsViewOptions DefaultJsViewOptions()
        {
            var voptions = base.DefaultJsViewOptions();
            voptions.ValidationControllerUrl = RouteHelper.New().SignumAction("ValidatePartial");
            return voptions;
        }

        protected override string DefaultViewing()
        {
            return EntityList.JsViewing(this, DefaultJsViewOptions()).ToJS();
        }

        public static JsInstruction JsViewing(EntityList elist, JsViewOptions viewOptions)
        {
            if (viewOptions.ControllerUrl == null)
                viewOptions.ControllerUrl = RouteHelper.New().SignumAction("PopupView");

            return new JsInstruction(() => "EListOnViewing({0})".Formato(",".Combine(
                elist.ToJS(),
                viewOptions.TryCC(v => v.ToJS()))));
        }

        protected override string DefaultCreating()
        {
            return EntityList.JsCreating(this, DefaultJsViewOptions()).ToJS();
        }

        private static JsInstruction JsCreating(EntityList elist, JsViewOptions viewOptions)
        {
            if (viewOptions.ControllerUrl == null)
                viewOptions.ControllerUrl = RouteHelper.New().SignumAction("PopupView");

            return new JsInstruction(() => "EListOnCreating({0})".Formato(",".Combine(
                elist.ToJS(),
                viewOptions.TryCC(v => v.ToJS()),
                elist.HasManyImplementations ? RouteHelper.New().SignumAction("GetTypeChooser").SingleQuote() : null)));
        }

        protected override string DefaultFinding()
        {
            return EntityList.JsFinding(this, DefaultJsfindOptions()).ToJS();
        }

        public static JsInstruction JsFinding(EntityList elist, JsFindOptions findOptions)
        {
            return new JsInstruction(() => "EListOnFinding({0})".Formato(",".Combine(
                elist.ToJS(),
                findOptions.TryCC(v => v.ToJS()),
                elist.HasManyImplementations ? RouteHelper.New().SignumAction("GetTypeChooser").SingleQuote() : null)));
        }

        protected override string DefaultRemoving()
        {
            return EntityList.JsRemoving(this).ToJS();
        }

        public static JsInstruction JsRemoving(EntityList elist)
        {
            return new JsInstruction(() => "EListOnRemoving({0})".Formato(elist.ToJS()));
        }
    }
}
