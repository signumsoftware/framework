using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Windows;
using System.Windows.Media.Animation;

namespace Signum.Windows
{
    public class DataBorder : Border
    {
        public DataBorder()
        {
            this.DataContextChanged += new DependencyPropertyChangedEventHandler(DataBorder_DataContextChanged);
            this.Loaded += new RoutedEventHandler(DataBorder_Loaded);
        }

        void DataBorder_Loaded(object sender, RoutedEventArgs e)
        {

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
