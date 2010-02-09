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
        public bool Clone {get; set;}

        public bool? ReadOnly { get; set; }

        public bool Admin { get; set; }

        public Control View { get; set; }

        internal abstract ViewButtons GetViewButtons();
    }

    public class ViewOptions: ViewOptionsBase
    {
        public ViewOptions()
        {
            Clone = true;
        }

        public PropertyRoute TypeContext { get; set; }

        internal override ViewButtons GetViewButtons()
        {
            return ViewButtons.Ok;
        }
    }

    public class NavigateOptions: ViewOptionsBase
    {
        public EventHandler Closed { get; set; }

        internal override ViewButtons GetViewButtons()
        {
            return ViewButtons.Save;
        }
    }
}
