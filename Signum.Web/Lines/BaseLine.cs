#region usings
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using System.Web.Mvc.Html;
using Signum.Utilities;
using Signum.Entities;
using Signum.Web.Properties;
using System.Web.Routing;
#endregion

namespace Signum.Web
{
    public abstract class BaseLine : TypeContext
    {
  

        protected BaseLine(Type type, object untypedValue, Context parent, string controlID, PropertyRoute propertyRoute)
            : base(parent, controlID, propertyRoute)
        {
            this.type = type;
            this.untypedValue = untypedValue; 
        }

        public string LabelText { get; set; }

        public readonly RouteValueDictionary LabelHtmlProps = new RouteValueDictionary();

        public bool visible = true;
        public bool Visible
        {
            get { return visible; }
            set { visible = value; }
        }

        public bool hideIfNull = false;
        public bool HideIfNull
        {
            get { return hideIfNull; }
            set { hideIfNull = value; }
        }

        bool reloadOnChange = false;
        public bool ReloadOnChange
        {
            get { return reloadOnChange; }
            set { reloadOnChange = value; }
        }

        string reloadControllerUrl = "Signum/ReloadEntity";
        public string ReloadControllerUrl 
        {
            get { return reloadControllerUrl; }
            set { reloadControllerUrl = value; } 
        }

        public string ReloadFunction { get; set; }

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
