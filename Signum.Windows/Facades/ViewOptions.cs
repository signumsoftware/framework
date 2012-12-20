using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Markup;
using System.Windows.Controls;
using System.Windows;
using Signum.Entities;

namespace Signum.Windows
{
    public abstract class ViewOptionsBase
    {
        public ViewOptionsBase()
        {
            ShowOperations = true;
        }

        public bool Clone {get; set;}

        public bool? ReadOnly { get; set; }

        public bool ShowOperations { get; set; }

        public Control View { get; set; }

        public abstract ViewMode ViewButtons { get; }
    }

    public class ViewOptions: ViewOptionsBase
    {
        public ViewOptions()
        {
            Clone = true;
        }

        public PropertyRoute TypeContext { get; set; }

        public bool? SaveProtected { get; set; }

        public AllowErrors AllowErrors { get; set; }

        public override ViewMode ViewButtons
        {
            get { return ViewMode.View; }
        }
    }

    public class NavigateOptions: ViewOptionsBase
    {
        public EventHandler Closed { get; set; }

        public override ViewMode ViewButtons
        {
            get { return ViewMode.Navigate; }
        }
    }
}
