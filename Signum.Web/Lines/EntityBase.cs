using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using System.Web.Mvc.Html;
using Signum.Utilities;
using Signum.Entities;
using Signum.Web.Properties;
using Signum.Entities.Reflection;

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
        public EntityBase(Type type, object untypedValue, Context parent, string controlID, PropertyRoute propertyRoute)
            : base(type, untypedValue, parent, controlID, propertyRoute)
        {
            View = true;
            Create = true;
            Find = true;
            Remove = true;
        }

        public Implementations Implementations { get; set; }
        
        public bool View { get; set; }
        public bool Create { get; set; }
        public bool Find { get; set; }
        public bool Remove { get; set; }
        public bool ReadOnlyEntity { get; set; }

        public string OnEntityChanged { get; set; }
        
        string onChangedTotal;
        protected internal string OnChangedTotal
        {
            get 
            {
                if (onChangedTotal.HasText())
                    return onChangedTotal;
                
                string doReload = (ReloadOnChange) ?
                    (ReloadFunction ?? "ReloadEntity('{0}','{1}');".Formato(ReloadControllerUrl, Parent.Parent.ControlID)) :
                    "";
                string total = OnEntityChanged + doReload;
                
                if (total.HasText())
                    onChangedTotal = "function(){" + total + "}";

                return onChangedTotal;
            }
        }

        public string PartialViewName { get; set; }

        public abstract string ToJS();

        public string OptionsJS()
        {
            return OptionsJSInternal().ToJS();
        }

        protected virtual JsOptionsBuilder OptionsJSInternal()
        {
            return new JsOptionsBuilder(false)
            {
                {"prefix", ControlID.TrySingleQuote()},
                {"onEntityChanged", OnChangedTotal}, 
            };
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

        internal Type CleanRuntimeType 
        { 
            get 
            {
                if (UntypedValue == null)
                    return null;

                return typeof(Lite).IsAssignableFrom(UntypedValue.GetType()) ? (UntypedValue as Lite).RuntimeType : UntypedValue.GetType();
            }
        }

        internal bool? IsNew
        {
            get 
            {
                return (UntypedValue as IIdentifiable).TryCS(i => i.IsNew) ??
                       (UntypedValue as Lite).TryCS(l => l.IdOrNull==null);
            }
        }

        internal int? IdOrNull
        {
            get
            {
                return (UntypedValue as IIdentifiable).TryCS(i => i.IdOrNull) ??
                       (UntypedValue as Lite).TryCS(l => l.IdOrNull);
            }
        }

        internal string ToStr
        {
            get 
            {
                return (UntypedValue as IIdentifiable).TryCC(i => i.ToStr) ??
                       (UntypedValue as Lite).TryCC(l => l.ToStr);
            }
        }
    }
}
