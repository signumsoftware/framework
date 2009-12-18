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
        private string defaultDetailDiv;
        public string DefaultDetailDiv
        {
            get { return defaultDetailDiv; }
        }

        public string DetailDiv { get; set; }

        public EntityListDetail(string prefix)
        {
            Prefix = prefix;
            defaultDetailDiv = TypeContext.Compose(prefix, EntityBaseKeys.Detail);
            DetailDiv = defaultDetailDiv;
        }

        public override string ToJS()
        {
            return "new EDList(" + this.OptionsJS() + ")";
        }

        public override string OptionsJS()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("{");

            sb.Append("prefix:'{0}'".Formato(Prefix));

            if (OnChangedTotal.HasText())
                sb.Append(",onEntityChanged:{0}".Formato(OnChangedTotal));

            sb.Append(",detailDiv:'{0}'".Formato(DetailDiv));

            sb.Append("}");
            return sb.ToString();
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
