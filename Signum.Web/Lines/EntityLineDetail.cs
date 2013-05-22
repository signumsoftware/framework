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
            return "$('#{0}').data('SF-entityLineDetail')".Formato(ControlID);
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
            return JsCreate(DefaultJsViewOptions()).ToJS();
        }

        public JsInstruction JsCreate(JsViewOptions viewOptions)
        {
            return new JsInstruction(() => "{0}.create({1})".Formato(
                this.ToJS(),
                viewOptions.TryCC(v => v.ToJS()) ?? ""));
        }

        protected override string DefaultFind()
        {
            return JsFind(DefaultJsfindOptions(), DefaultJsViewOptions()).ToJS();
        }

        public JsInstruction JsFind(JsFindOptions findOptions, JsViewOptions viewOptions)
        {
            string findParams = ",".Combine(
                findOptions.TryCC(v => v.ToJS()),
                viewOptions.TryCC(v => v.ToJS()));

            return new JsInstruction(() => "{0}.find({1})".Formato(this.ToJS(), findParams));
        }

        protected override string DefaultRemove()
        {
            return JsRemove().ToJS();
        }

        public JsInstruction JsRemove()
        {
            return new JsInstruction(() => "{0}.remove()".Formato(this.ToJS()));
        }
    }
}
