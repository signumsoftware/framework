using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Windows;
using System.Windows.Input;
using System.Windows.Data;
using System.Diagnostics;
using System.Reflection;

namespace Signum.Windows
{
    public class WatermarkTextBox: TextBox
    {
        public string HelpText
        {
            get { return (string)GetValue(HelpTextProperty); }
            set { SetValue(HelpTextProperty, value); }
        }

        public static readonly DependencyProperty HelpTextProperty =
            DependencyProperty.Register("HelpText",
                 typeof(string),
                 typeof(WatermarkTextBox),
                 new PropertyMetadata(String.Empty));
    }
}
