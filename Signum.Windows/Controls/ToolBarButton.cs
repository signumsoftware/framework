using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Windows;
using System.Windows.Media;

namespace Signum.Windows
{
    public class ToolBarButton : Button
    {
        public static readonly DependencyProperty ImageProperty =
            DependencyProperty.Register("Image", typeof(ImageSource), typeof(ToolBarButton), new UIPropertyMetadata(null));
        public ImageSource Image
        {
            get { return (ImageSource)GetValue(ImageProperty); }
            set { SetValue(ImageProperty, value); }
        }

        static ToolBarButton()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ToolBarButton), new FrameworkPropertyMetadata(typeof(ToolBarButton)));
        }
    }
}
