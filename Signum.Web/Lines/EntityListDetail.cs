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
            result.Add("detailDiv", DetailDiv.TrySingleQuote());
            return result;
        }

        protected override string DefaultViewing()
        {
            return EntityListDetail.JsViewing(this, DefaultJsViewOptions()).ToJS();
        }

        public static JsRenderer JsViewing(EntityListDetail edlist, JsViewOptions viewOptions)
        {
            return new JsRenderer(() => "EDListOnViewing({0})".Formato(",".Combine(
                edlist.ToJS(),
                viewOptions.TryCC(v => v.ToJS()))));
        }

        protected override string DefaultCreating()
        {
            return EntityListDetail.JsCreating(this, DefaultJsViewOptions()).ToJS();
        }

        private static JsRenderer JsCreating(EntityListDetail edlist, JsViewOptions viewOptions)
        {
            return new JsRenderer(() => "EDListOnCreating({0})".Formato(",".Combine(
                edlist.ToJS(),
                viewOptions.TryCC(v => v.ToJS()))));
        }

        protected override string DefaultFinding()
        {
            return EntityListDetail.JsFinding(this, DefaultJsfindOptions(), DefaultJsViewOptions()).ToJS();
        }

        public static JsRenderer JsFinding(EntityListDetail edlist, JsFindOptions findOptions, JsViewOptions viewOptions)
        {
            return new JsRenderer(() => "EDListOnFinding({0})".Formato(",".Combine(
                edlist.ToJS(),
                findOptions.TryCC(f => f.ToJS())),
                viewOptions.TryCC(v => v.ToJS())));
        }

        protected override string DefaultRemoving()
        {
            return EntityListDetail.JsRemoving(this).ToJS();
        }

        public static JsRenderer JsRemoving(EntityListDetail edlist)
        {
            return new JsRenderer(() => "EDListOnRemoving({0})".Formato(edlist.ToJS()));
        }
    }
}
