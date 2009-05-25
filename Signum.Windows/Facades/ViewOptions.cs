using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Markup;

namespace Signum.Windows
{
    public class ViewOptions : MarkupExtension
    {
        ViewButtons buttons = ViewButtons.OkCancel;
        public ViewButtons Buttons
        {
            get { return buttons; }
            set { buttons = value; }
        }

        bool? clone;
        public bool Clone
        {
            get { return clone ?? Buttons == ViewButtons.OkCancel; }
            set { clone = value; }
        }

        bool? modal;
        public bool Modal
        {
            get { return modal ?? Buttons == ViewButtons.OkCancel; }
            set { modal = value; }
        }

        public bool Admin { get; set; }

        public TypeContext TypeContext { get; set; }


        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return this;
        }
    }

    public enum ViewButtons
    {
        OkCancel,
        //OkCancelSaving, //Accepts with save behaviour
        Save,
    }


}
