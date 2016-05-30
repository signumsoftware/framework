using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Signum.Entities;

namespace Signum.Web
{
    public class NavigateOptions
    {
        public NavigateOptions()
        {
            ShowOperations = true;
        }

        public bool? ReadOnly { get; set; }

        public string PartialViewName { get; set; }

        public bool ShowOperations { get; set; }

        public bool WriteEntityState { get; set; }
    }

    public abstract class PopupOptionsBase
    {
        public PopupOptionsBase(string prefix)
        {
            this.Prefix = prefix;
            ShowOperations = true;
        }

        public string Prefix { get; set; }

        public bool? ReadOnly { get; set; }

        public string PartialViewName { get; set; }

        public bool ShowOperations { get; set; }

        public abstract ViewMode ViewMode { get; }
    }

    public class PopupViewOptions : PopupOptionsBase
    {
        public PopupViewOptions(string prefix)
            : base(prefix)
        { }

        public bool? RequiresSaveOperation { get; set; }

        public PropertyRoute PropertyRoute { get; set; }

        public override ViewMode ViewMode 
        {
            get { return Web.ViewMode.View; } 
        }
    }

    public class PopupNavigateOptions : PopupOptionsBase
    {
        public PopupNavigateOptions(string prefix)
            : base(prefix)
        { }

        public override ViewMode ViewMode
        {
            get { return Web.ViewMode.Navigate; }
        }
    }

    public enum ViewMode
    {
        View,
        Navigate
    }
}