using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Windows;
using System.Windows.Media.Animation;
using Signum.Utilities;
using System.Windows.Media;
using Signum.Entities;
using System.Windows.Automation.Peers;

namespace Signum.Windows
{
    public class DataBorder : Border
    {
        public static readonly DependencyProperty AutoChildProperty =
           DependencyProperty.Register("AutoChild", typeof(bool), typeof(DataBorder), new UIPropertyMetadata(false));
        public bool AutoChild
        {
            get { return (bool)GetValue(AutoChildProperty); }
            set { SetValue(AutoChildProperty, value); }
        }

        public DataBorder()
        {
            this.DataContextChanged += new DependencyPropertyChangedEventHandler(DataBorder_DataContextChanged);
            this.Loaded += new RoutedEventHandler(DataBorder_Loaded);
        }

        void DataBorder_Loaded(object sender, RoutedEventArgs e)
        {
            this.Loaded -= DataBorder_Loaded;
            RecalculateVisibility(null, DataContext);
        }

        void DataBorder_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            RecalculateVisibility(e.OldValue, e.NewValue);
        }

        DoubleAnimation animation = new DoubleAnimation()
        {
            Duration = new Duration(TimeSpan.FromSeconds(0.1)),
            From = 0,
            To = 1,
        };

        private void RecalculateVisibility(object oldValue, object newValue)
        {
            if(AutoChild)
            {
                // when datacontext change is fired but its not loaded, it's quite possible that some Common.Routes are not working yet
                if (newValue == null/* || !IsLoaded*/) 
                    Child = null;
                else
                {
                    EntitySettings es = Navigator.Manager.EntitySettings.TryGetC(newValue.GetType());
                    if (es == null)
                        Child = new TextBox
                        {
                            Text = "No EntitySettings for {0}".FormatWith(newValue.GetType()),
                            Foreground = Brushes.Red,
                            FontWeight = FontWeights.Bold
                        };
                    else
                        Child = es.CreateView((ModifiableEntity)newValue, Common.GetPropertyRoute(this)); 
                }
            }
            if (Child != null)
            {
                if (newValue == null)
                    Child.Visibility = Visibility.Hidden;
                else
                    Child.Visibility = Visibility.Visible;

                if (oldValue != null && newValue != null)
                    Child.BeginAnimation(UIElement.OpacityProperty, animation);
            }
        }
    }

    public class AutomationBorder : Border
    {
        protected override AutomationPeer OnCreateAutomationPeer()
        {
            return new AutomationBorderPeer(this);
        }
    }

    public class AutomationBorderPeer : FrameworkElementAutomationPeer
    {
        public AutomationBorderPeer(AutomationBorder automationBorder) : base(automationBorder)
        {
        }

        protected override string GetClassNameCore()
        {
            return "AutomationBorder";
        }
    }
}
