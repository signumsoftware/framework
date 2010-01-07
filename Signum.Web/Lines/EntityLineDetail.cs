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
        private string defaultDetailDiv;
        public string DefaultDetailDiv
        {
            get { return defaultDetailDiv; }
        }

        public string DetailDiv { get; set; }

        public EntityLineDetail(string prefix)
        {
            Prefix = prefix;
            defaultDetailDiv = TypeContext.Compose(prefix, EntityBaseKeys.Detail);
            DetailDiv = defaultDetailDiv;
            View = false;
        }

        public override void SetReadOnly()
        {
            ReadOnly = true;
            Find = false;
            Create = false;
            Remove = false;
            Implementations = null;
        }

        public override string ToJS()
        {
            return "new EDLine(" + this.OptionsJS() + ")";
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
            return null;
        }

        protected override string DefaultCreating()
        {
            return EntityLineDetail.JsCreating(this, DefaultJsViewOptions()).ToJS();
        }

        public static JsRenderer JsCreating(EntityLineDetail edline, JsViewOptions viewOptions)
        {
            return new JsRenderer(() => "EDLineOnCreating({0})".Formato(",".Combine(
                edline.ToJS(),
                viewOptions.TryCC(v => v.ToJS()))));
        }

        protected override string DefaultFinding()
        {
            return EntityLineDetail.JsFinding(this, DefaultJsfindOptions(), DefaultJsViewOptions()).ToJS();
        }

        public static JsRenderer JsFinding(EntityLineDetail edline, JsFindOptions findOptions, JsViewOptions viewOptions)
        {
            return new JsRenderer(() => "EDLineOnFinding({0})".Formato(",".Combine(
                edline.ToJS(),
                findOptions.TryCC(f => f.ToJS())),
                viewOptions.TryCC(v => v.ToJS())));
        }

        protected override string DefaultRemoving()
        {
            return EntityLineDetail.JsRemoving(this).ToJS();
        }

        public static JsRenderer JsRemoving(EntityLineDetail edline)
        {
            return new JsRenderer(() => "EDLineOnRemoving({0})".Formato(edline.ToJS()));
        }
    }
}
