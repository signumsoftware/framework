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
        public EntityList(string prefix)
        {
            Prefix = prefix;
        }

        public override string ToJS()
        {
            return "new EList(" + this.OptionsJS() + ")";
        }

        protected override string DefaultViewing()
        {
            return EntityList.JsViewing(this, DefaultJsViewOptions()).ToJS();
        }

        public static JsRenderer JsViewing(EntityList elist, JsViewOptions viewOptions)
        {
            return new JsRenderer(() => "EListOnViewing({0})".Formato(",".Combine(
                elist.ToJS(),
                viewOptions.TryCC(v => v.ToJS()))));
        }

        protected override string DefaultCreating()
        {
            return EntityList.JsCreating(this, DefaultJsViewOptions()).ToJS();
        }

        private static JsRenderer JsCreating(EntityList elist, JsViewOptions viewOptions)
        {
            return new JsRenderer(() => "EListOnCreating({0})".Formato(",".Combine(
                elist.ToJS(),
                viewOptions.TryCC(v => v.ToJS()))));
        }

        protected override string DefaultFinding()
        {
            return EntityList.JsFinding(this, DefaultJsfindOptions()).ToJS();
        }

        public static JsRenderer JsFinding(EntityList elist, JsFindOptions findOptions)
        {
            return new JsRenderer(() => "EListOnFinding({0})".Formato(",".Combine(
                elist.ToJS(),
                findOptions.TryCC(v => v.ToJS()))));
        }

        protected override string DefaultRemoving()
        {
            return EntityList.JsRemoving(this).ToJS();
        }

        public static JsRenderer JsRemoving(EntityList elist)
        {
            return new JsRenderer(() => "EListOnRemoving({0})".Formato(elist.ToJS()));
        }
    }
}
