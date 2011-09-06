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
        public EntityList(Type type, object untypedValue, Context parent, string controlID, FieldRoute propertyRoute)
            : base(type, untypedValue, parent, controlID, propertyRoute)
        {
        }

        public override string ToJS()
        {
            return "new SF.EList(" + this.OptionsJS() + ")";
        }

        public override JsViewOptions DefaultJsViewOptions()
        {
            var voptions = base.DefaultJsViewOptions();
            voptions.ValidationOptions = new JsValidatorOptions { ControllerUrl = RouteHelper.New().SignumAction("ValidatePartial") };
            return voptions;
        }

        protected override string DefaultView()
        {
            return EntityList.JsView(this, DefaultJsViewOptions()).ToJS();
        }

        public static JsInstruction JsView(EntityList elist, JsViewOptions viewOptions)
        {
            if (elist.ViewMode == ViewMode.Navigate)
            {
                viewOptions.Navigate = true;
                viewOptions.ControllerUrl = Navigator.ViewRoute(elist.ElementType.CleanType(), null);
            }
            else if (viewOptions.ControllerUrl == null)
                viewOptions.ControllerUrl = RouteHelper.New().SignumAction("PopupView");

            return new JsInstruction(() => "{0}.view({1})".Formato(
                     elist.ToJS(),
                     viewOptions.TryCC(v => v.ToJS()) ?? ""
                 ));
        }

        protected override string DefaultCreate()
        {
            return EntityList.JsCreate(this, DefaultJsViewOptions()).ToJS();
        }

        private static JsInstruction JsCreate(EntityList elist, JsViewOptions viewOptions)
        {
            if (elist.ViewMode == ViewMode.Navigate)
            {
                viewOptions.Navigate = true;
                viewOptions.ControllerUrl = Navigator.ViewRoute(elist.ElementType.CleanType(), null);
            }
            else if (viewOptions.ControllerUrl == null)
                viewOptions.ControllerUrl = RouteHelper.New().SignumAction("PopupView");

            string createParams = ",".Combine(
                viewOptions.TryCC(v => v.ToJS()),
                elist.HasManyImplementations ? RouteHelper.New().SignumAction("GetTypeChooser").SingleQuote() : null);

            return new JsInstruction(() => "{0}.create({1})".Formato(elist.ToJS(), createParams));
        }

        protected override string DefaultFind()
        {
            return EntityList.JsFind(this, DefaultJsfindOptions()).ToJS();
        }

        public static JsInstruction JsFind(EntityList elist, JsFindOptions findOptions)
        {
            string findParams = ",".Combine(
                findOptions.TryCC(v => v.ToJS()),
                elist.HasManyImplementations ? RouteHelper.New().SignumAction("GetTypeChooser").SingleQuote() : null);

            return new JsInstruction(() => "{0}.find({1})".Formato(elist.ToJS(), findParams));
        }

        protected override string DefaultRemove()
        {
            return EntityList.JsRemove(this).ToJS();
        }

        public static JsInstruction JsRemove(EntityList elist)
        {
            return new JsInstruction(() => "{0}.remove()".Formato(elist.ToJS()));
        }
    }
}
