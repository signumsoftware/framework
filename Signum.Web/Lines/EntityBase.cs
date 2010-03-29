using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using System.Web.Mvc.Html;
using Signum.Utilities;
using Signum.Entities;
using Signum.Web.Properties;

namespace Signum.Web
{
    public static class EntityBaseKeys
    { 
        public const string RuntimeInfo = "sfRuntimeInfo";
        public const string StaticInfo = "sfStaticInfo";
        public const string Implementations = "sfImplementations";
        public const string Entity = "sfEntity";
        public const string Template = "sfTemplate";
        public const string ToStr = "sfToStr";
        public const string ToStrLink = "sfLink";
        public const string IsNew = "sfIsNew";
        public const string Detail = "sfDetail";
    }

    public abstract class EntityBase : BaseLine, IJSRenderer
    {
        public EntityBase()
        { 
            View = true;
            Create = true;
            Find = true;
            Remove = true;
        }

        public string Prefix { get; set; }

        private string parentPrefix;
        public string ParentPrefix 
        {
            get 
            {
                if (parentPrefix.HasText())
                    return parentPrefix;

                int lastIndex = Prefix.LastIndexOf(TypeContext.Separator);
                parentPrefix = Prefix.Substring(0, lastIndex);
                return parentPrefix;
            }
        }

        public string LocalName(string sufix)
        {
            return TypeContext.Compose(Prefix, sufix); 
        }


        public string ParentName(string sufix)
        {
            return TypeContext.Compose(ParentPrefix, sufix);
        }


        public Implementations Implementations { get; set; }
        
        public bool View { get; set; }
        public bool Create { get; set; }
        public bool Find { get; set; }
        public bool Remove { get; set; }

        public string OnEntityChanged { get; set; }
        
        string onChangedTotal;
        protected internal string OnChangedTotal
        {
            get 
            {
                if (onChangedTotal.HasText())
                    return onChangedTotal;
                
                string doReload = (ReloadOnChange) ?
                    (ReloadFunction ?? "ReloadEntity('{0}','{1}');".Formato(ReloadControllerUrl, ParentPrefix)) :
                    "";
                string total = OnEntityChanged + doReload;
                
                if (total.HasText())
                    onChangedTotal = "function(){" + total + "}";

                return onChangedTotal;
            }
        }

        public string PartialViewName { get; set; }

        public abstract string ToJS();

        public virtual string OptionsJS()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("{");

            sb.Append("prefix:'{0}'".Formato(Prefix));

            if (OnChangedTotal.HasText())
                sb.Append(",onEntityChanged:{0}".Formato(OnChangedTotal));

            sb.Append("}");
            return sb.ToString();
        }

        protected JsViewOptions DefaultJsViewOptions()
        {
            return new JsViewOptions { PartialViewName = this.PartialViewName };
        }

        protected JsFindOptions DefaultJsfindOptions()
        {
            return new JsFindOptions();
        }

        public string Viewing { get; set; }
        protected abstract string DefaultViewing();
        internal string GetViewing()
        {
            if (!View)
                return "";
            return Viewing ?? DefaultViewing();
        }

        public string Creating { get; set; }
        protected abstract string DefaultCreating();
        internal string GetCreating()
        {
            if (!Create)
                return "";
            return Creating ?? DefaultCreating();
        }

        public string Finding { get; set; }
        protected abstract string DefaultFinding();
        internal string GetFinding()
        { 
            if (!Find)
                return "";
            return Finding ?? DefaultFinding();
        }

        public string Removing { get; set; }
        protected abstract string DefaultRemoving();
        internal string GetRemoving()
        {
            if (!Remove)
                return "";
            return Removing ?? DefaultRemoving();
        }
    }
}
