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

        public Control View { get; set; }

        public abstract ViewButtons ViewButtons { get; set; }
    }

    public class ViewOptions: ViewOptionsBase
    {
        public ViewOptions()
        {
            Clone = true;
            ViewButtons = Windows.ViewButtons.Ok;
        }

        public PropertyRoute TypeContext { get; set; }

        public AllowErrors AllowErrors { get; set; }

        public override ViewButtons ViewButtons { get; set; }
    }

    public class NavigateOptions: ViewOptionsBase
    {
        public EventHandler Closed { get; set; }

        public override ViewButtons ViewButtons
        {
            get { return ViewButtons.Save; }
            set { throw new InvalidOperationException("ViewButtons is always Save in NavigateOptions"); }
        }
    }
}
