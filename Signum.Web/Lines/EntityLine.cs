#region usings
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using System.Linq.Expressions;
using Signum.Utilities;
using System.Web.Mvc.Html;
using Signum.Entities;
using System.Reflection;
using Signum.Entities.Reflection;
using System.Configuration;
using Signum.Engine;
using Signum.Web.Properties;
using Signum.Utilities.Reflection;
#endregion

namespace Signum.Web
{
    public class EntityLine : EntityBase
    {
        public bool Autocomplete { get; set; }
        public bool Navigate { get; set; }
        
       public EntityLine(Type type, object untypedValue, Context parent, string controlID, PropertyRoute propertyRoute)
            : base(type, untypedValue, parent, controlID, propertyRoute)
        {
            Navigate = true;
            Autocomplete = true;        
        }

        protected override void SetReadOnly()
        {
            Parent.ReadOnly = true;
            Find = false;
            Create = false;
            Remove = false;
            Autocomplete = false;
            //Implementations = null;
        }

        public override string ToJS()
        {
            return "new SF.ELine(" + this.OptionsJS() + ")";
        }

        protected override JsViewOptions DefaultJsViewOptions()
        {
            var voptions = base.DefaultJsViewOptions();
            voptions.ValidationControllerUrl = RouteHelper.New().SignumAction("ValidatePartial");
            return voptions;
        }

        protected override string DefaultView()
        {
            return EntityLine.JsView(this, DefaultJsViewOptions()).ToJS();
        }

        public static JsInstruction JsView(EntityLine eline, JsViewOptions viewOptions)
        {
            if (viewOptions.ControllerUrl == null)
                viewOptions.ControllerUrl = RouteHelper.New().SignumAction("PopupView");

            return new JsInstruction(() => "{0}.view({1})".Formato(
                    eline.ToJS(),
                    viewOptions.TryCC(v => v.ToJS()) ?? ""
                ));
        }

        protected override string DefaultCreate()
        {
            return EntityLine.JsCreate(this, DefaultJsViewOptions()).ToJS();
        }

        public static JsInstruction JsCreate(EntityLine eline, JsViewOptions viewOptions)
        {
            if (viewOptions.ControllerUrl == null)
                viewOptions.ControllerUrl = RouteHelper.New().SignumAction("PopupView");

            string createParams = ",".Combine(
                viewOptions.TryCC(v => v.ToJS()),
                eline.HasManyImplementations ? RouteHelper.New().SignumAction("GetTypeChooser").SingleQuote() : null);

            return new JsInstruction(() => "{0}.create({1})".Formato(eline.ToJS(), createParams));
        }

        protected override string DefaultFind()
        {
            return EntityLine.JsFind(this, DefaultJsfindOptions()).ToJS();
        }

        public static JsInstruction JsFind(EntityLine eline, JsFindOptions findOptions)
        {
            string findParams = ",".Combine(
                findOptions.TryCC(v => v.ToJS()),
                eline.HasManyImplementations ? RouteHelper.New().SignumAction("GetTypeChooser").SingleQuote() : null);

            return new JsInstruction(() => "{0}.find({1})".Formato(eline.ToJS(), findParams));
        }

        protected override string DefaultRemove()
        {
            return EntityLine.JsRemove(this).ToJS();
        }

        public static JsInstruction JsRemove(EntityLine eline)
        {
            return new JsInstruction(() => "{0}.remove()".Formato(eline.ToJS()));
        }
    }
}
