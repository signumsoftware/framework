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
using Signum.Web.Properties;
#endregion

namespace Signum.Web
{
    public class EntityLineDetail : EntityBase
    {
        public string DefaultDetailDiv { get; private set; }
        public string DetailDiv { get; set; }

        public EntityLineDetail(Type type, object untypedValue, Context parent, string controlID, PropertyRoute propertyRoute)
            : base(type, untypedValue, parent, controlID, propertyRoute)
        {
            DefaultDetailDiv = DetailDiv = this.Compose(EntityBaseKeys.Detail);
            View = false;
            LabelClass = "sf-label-detail-line";
        }

        protected override void SetReadOnly()
        {
            Parent.ReadOnly = true;
            Find = false;
            Create = false;
            Remove = false;
        }

        public override string ToJS()
        {
            return "new SF.EDLine(" + this.OptionsJS() + ")";
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
            return null;
        }

        protected override string DefaultCreate()
        {
            return EntityLineDetail.JsCreate(this, DefaultJsViewOptions()).ToJS();
        }

        public static JsInstruction JsCreate(EntityLineDetail edline, JsViewOptions viewOptions)
        {
            if (viewOptions.ControllerUrl == null)
                viewOptions.ControllerUrl = RouteHelper.New().SignumAction("PartialView");

            string createParams = ",".Combine(
                viewOptions.TryCC(v => v.ToJS()),
                edline.HasManyImplementations ? RouteHelper.New().SignumAction("GetTypeChooser").SingleQuote() : null);

            return new JsInstruction(() => "{0}.create({1})".Formato(edline.ToJS(), createParams));
        }

        protected override string DefaultFind()
        {
            return EntityLineDetail.JsFind(this, DefaultJsfindOptions(), DefaultJsViewOptions()).ToJS();
        }

        public static JsInstruction JsFind(EntityLineDetail edline, JsFindOptions findOptions, JsViewOptions viewOptions)
        {
            if (viewOptions.ControllerUrl == null)
                viewOptions.ControllerUrl = RouteHelper.New().SignumAction("PartialView");

            string findParams = ",".Combine(
                findOptions.TryCC(v => v.ToJS()),
                viewOptions.TryCC(v => v.ToJS()),
                edline.HasManyImplementations ? RouteHelper.New().SignumAction("GetTypeChooser").SingleQuote() : null);

            return new JsInstruction(() => "{0}.find({1})".Formato(edline.ToJS(), findParams));
        }

        protected override string DefaultRemove()
        {
            return EntityLineDetail.JsRemove(this).ToJS();
        }

        public static JsInstruction JsRemove(EntityLineDetail edline)
        {
            return new JsInstruction(() => "{0}.remove()".Formato(edline.ToJS()));
        }
    }
}
