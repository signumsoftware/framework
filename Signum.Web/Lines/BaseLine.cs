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
#endregion

namespace Signum.Web
{
    public abstract class BaseLine : StyleContext
    {
        public abstract void SetReadOnly();

        public string LabelText { get; set; }
        public readonly Dictionary<string, object> LabelHtmlProps = new Dictionary<string, object>(0);

        public bool visible = true;
        public bool Visible
        {
            get { return visible; }
            set { visible = value; }
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
    }
}
