using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Markup;

namespace Signum.Windows
{
    public class ViewOptions : MarkupExtension
    {
        ViewButtons buttons = ViewButtons.Ok;
        public ViewButtons Buttons
        {
            get { return buttons; }
            set { buttons = value; }
        }

        bool? clone;
        public bool Clone
        {
            get { return clone ?? Buttons == ViewButtons.Ok; }
            set { clone = value; }
        }

        public bool Modal
        {
            get { return  Buttons == ViewButtons.Ok; }
        }

        public EventHandler Closed { get; set; }

        public bool Admin { get; set; }

        public TypeContext TypeContext { get; set; }


        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return this;
        }
    }

    public enum ViewButtons
    {
        Ok,
        Save,
    }


}
