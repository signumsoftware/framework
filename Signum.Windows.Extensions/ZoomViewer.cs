using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace Signum.Windows.Extensions
{
    public class ZoomViewer : ScrollViewer
    {
        public static readonly DependencyProperty ZoomProperty =
            DependencyProperty.Register("Zoom", typeof(int), typeof(ZoomViewer), new UIPropertyMetadata(1));
        public int Zoom
        {
            get { return (int)GetValue(ZoomProperty); }
            set { SetValue(ZoomProperty, value); }
        }

        static ZoomViewer()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ZoomViewer), new FrameworkPropertyMetadata(typeof(ZoomViewer)));
            HorizontalScrollBarVisibilityProperty.OverrideMetadata(typeof(ZoomViewer), new FrameworkPropertyMetadata(ScrollBarVisibility.Auto));
        }

        protected override void OnMouseWheel(System.Windows.Input.MouseWheelEventArgs e)
        {
            if ((Keyboard.Modifiers & ModifierKeys.Control) != 0)
            {
                if (e.Delta < 0)
                {
                    if (Zoom < 10)
                        Zoom++;
                }
                else
                {
                    if (-10 < Zoom)
                        Zoom--;
                }
            }
            else if ((Keyboard.Modifiers & ModifierKeys.Shift) != 0)
            {
                if (e.Delta < 0)
                    LineRight();
                else
                    LineLeft();
            }
            else
            {
                if (e.Delta < 0)
                    LineDown();
                else
                    LineUp();
            }
        
            e.Handled = true;
        }


        public void ScrollToElement(UIElement element)
        {
            GeneralTransform gt = element.TransformToAncestor(this); 
        }
    }
}
