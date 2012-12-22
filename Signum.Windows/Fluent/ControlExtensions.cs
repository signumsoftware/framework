using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace Signum.Windows
{
    public static class ControlExtensions
    {
        public static bool IsSet(this DependencyObject depObj, DependencyProperty prop)
        {
            return DependencyPropertyHelper.GetValueSource(depObj, prop).BaseValueSource == BaseValueSource.Local;
        }

        public static bool NotSet(this DependencyObject depObj, DependencyProperty prop)
        {
            return DependencyPropertyHelper.GetValueSource(depObj, prop).BaseValueSource != BaseValueSource.Local;
        }

        public static Visibility ToVisibility(this bool val)
        {
            return val ? Visibility.Visible : Visibility.Collapsed;
        }

        public static bool FromVisibility(this Visibility val)
        {
            return val == Visibility.Visible;
        }

        public static TabControl AddTab(this TabControl tabControl, string header, FrameworkElement content)
        {
            tabControl.Items.Add(new TabItem { Header = header, Content = content });
            return tabControl;
        }
    }
}
