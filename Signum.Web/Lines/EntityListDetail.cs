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
            return "new SF.EDList(" + this.OptionsJS() + ")";
        }

        protected override JsOptionsBuilder OptionsJSInternal()
        {
            var result = base.OptionsJSInternal();
            if (DetailDiv.HasText())
                result.Add("detailDiv", DetailDiv.SingleQuote());
            return result;
        }

        protected override string DefaultView()
        {
            return EntityListDetail.JsView(this, DefaultJsViewOptions()).ToJS();
        }

        public static JsInstruction JsView(EntityListDetail edlist, JsViewOptions viewOptions)
        {
            if (viewOptions.ControllerUrl == null)
                viewOptions.ControllerUrl = RouteHelper.New().SignumAction("PartialView");

            return new JsInstruction(() => "{0}.view({1})".Formato(
                    edlist.ToJS(),
                    viewOptions.TryCC(v => v.ToJS()) ?? ""
                ));
        }

        protected override string DefaultCreate()
        {
            return EntityListDetail.JsCreate(this, DefaultJsViewOptions()).ToJS();
        }

        private static JsInstruction JsCreate(EntityListDetail edlist, JsViewOptions viewOptions)
        {
            if (viewOptions.ControllerUrl == null)
                viewOptions.ControllerUrl = RouteHelper.New().SignumAction("PartialView");

            string createParams = ",".Combine(
                viewOptions.TryCC(v => v.ToJS()),
                edlist.HasManyImplementations ? RouteHelper.New().SignumAction("GetTypeChooser").SingleQuote() : null);

            return new JsInstruction(() => "{0}.create({1})".Formato(edlist.ToJS(), createParams));
        }

        protected override string DefaultFind()
        {
            return EntityListDetail.JsFind(this, DefaultJsfindOptions(), DefaultJsViewOptions()).ToJS();
        }

        public static JsInstruction JsFind(EntityListDetail edlist, JsFindOptions findOptions, JsViewOptions viewOptions)
        {
            string findParams = ",".Combine(
                findOptions.TryCC(f => f.ToJS()),
                viewOptions.TryCC(v => v.ToJS()),
                edlist.HasManyImplementations ? RouteHelper.New().SignumAction("GetTypeChooser").SingleQuote() : null);

            return new JsInstruction(() => "{0}.find({1})".Formato(edlist.ToJS(), findParams));            
        }

        protected override string DefaultRemove()
        {
            return EntityListDetail.JsRemove(this).ToJS();
        }

        public static JsInstruction JsRemove(EntityListDetail edlist)
        {
            return new JsInstruction(() => "{0}.remove()".Formato(edlist.ToJS()));
        }
    }
}
