using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Windows;
using System.Windows.Media.Animation;
using Signum.Utilities;
using System.Windows.Media;

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
                if (newValue == null)
                    Child = null;
                else
                {
                    EntitySettings setting = Navigator.GetEntitySettings(newValue.GetType());
                    if (setting == null ||setting.View == null)
                        Child = new TextBox
                        {
                            Text = "No EntitySettings.View for {0}".Formato(newValue.GetType()),
                            Foreground = Brushes.Red,
                            FontWeight = FontWeights.Bold
                        }; 
                    else
                        Child = setting.View(); 
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
}
