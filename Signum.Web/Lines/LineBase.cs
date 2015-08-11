using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using System.Web.Mvc.Html;
using Signum.Utilities;
using Signum.Entities;
using System.Web.Routing;

namespace Signum.Web
{
    public abstract class LineBase : TypeContext
    {
        protected LineBase(Type type, object untypedValue, Context parent, string prefix, PropertyRoute propertyRoute)
            : base(parent, prefix, propertyRoute)
        {
            this.type = type;
            this.untypedValue = untypedValue; 
        }

        public MvcHtmlString LabelHtml { get; set; }
        public string LabelText { get; set; }

        public readonly RouteValueDictionary LabelHtmlProps = new RouteValueDictionary();
        public readonly RouteValueDictionary FormGroupHtmlProps = new RouteValueDictionary();

        bool visible = true;
        public bool Visible
        {
            get { return visible; }
            set { visible = value; }
        }

        bool hideIfNull = false;
        public bool HideIfNull
        {
            get { return hideIfNull; }
            set { hideIfNull = value; }
        }

        object untypedValue;
        public override object UntypedValue
        {
            get { return untypedValue; }
        }

        Type type;
        public override Type Type
        {
            get { return type; }
        }

        internal override TypeContext Clone(object newValue)
        {
            throw new InvalidOperationException();
        }
    }

    public static class RouteValueDictionaryExtensions
    {
        public static void AddCssClass(this RouteValueDictionary dic, string newClass)
        {
            object value;
            if (dic.TryGetValue("class", out value))
                dic["class"] = value + " " + newClass;
            else
                dic["class"] = newClass;
        }
    }
}
