using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.ComponentModel;
using Signum.Utilities;

namespace Signum.Windows
{
    public class LineBase : UserControl, IPreLoad
    {
        public event EventHandler PreLoad;

        public static readonly DependencyProperty LabelTextProperty =
            DependencyProperty.Register("LabelText", typeof(string), typeof(LineBase), new UIPropertyMetadata("Property"));
        public string LabelText
        {
            get { return (string)GetValue(LabelTextProperty); }
            set { SetValue(LabelTextProperty, value); }
        }

        public static readonly DependencyProperty TypeProperty =
            DependencyProperty.Register("Type", typeof(Type), typeof(LineBase), new UIPropertyMetadata(null));
        public Type Type
        {
            get { return (Type)GetValue(TypeProperty); }
            set { SetValue(TypeProperty, value); }
        }

        protected internal virtual DependencyProperty CommonRouteLabelText()
        {
            return LabelTextProperty;
        }

        protected internal virtual DependencyProperty CommonRouteValue()
        {
            return null;
        }

        protected internal virtual DependencyProperty CommonRouteType()
        {
            return TypeProperty;
        }

        public LineBase()
        {
            this.Loaded += new RoutedEventHandler(OnLoad);

        }
        public virtual void OnLoad(object sender, RoutedEventArgs e)
        {
            this.Loaded -= OnLoad;

            if (PreLoad != null)
                PreLoad(this, EventArgs.Empty);

            if (DesignerProperties.GetIsInDesignMode(this))
                return;

            if (this.Type == null)
            {
                throw new ApplicationException(Properties.Resources.TypePropertyItsNotDeterminedForControl0.Formato(LabelText));
            }
        }
    }

    public interface IPreLoad
    {
        event EventHandler PreLoad;
    }
}
