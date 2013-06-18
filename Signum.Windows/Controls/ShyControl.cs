using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Windows;

namespace Signum.Windows
{
    public class ShyBorder : Border
    {
        public static readonly DependencyProperty HorizontalProperty =
            DependencyProperty.Register("Horizontal", typeof(bool), typeof(ShyBorder), new UIPropertyMetadata(false));
        public bool Horizontal
        {
            get { return (bool)GetValue(HorizontalProperty); }
            set { SetValue(HorizontalProperty, value); }
        }


        public static readonly DependencyProperty VerticalProperty =
            DependencyProperty.Register("Vertical", typeof(bool), typeof(ShyBorder), new UIPropertyMetadata(false));
        public bool Vertical
        {
            get { return (bool)GetValue(VerticalProperty); }
            set { SetValue(VerticalProperty, value); }
        }

        protected override Size MeasureOverride(Size constraint)
        {
            if (Child == null)
                return new Size();
            
            Child.Measure(constraint);

            return new Size(
                Horizontal ? 0 : Child.DesiredSize.Width,
                Vertical ? 0 : Child.DesiredSize.Height); 
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            if (Child == null)
                return new Size();

            if (Horizontal)
                ((FrameworkElement)Child).Width = finalSize.Width; //HACK máximo!
            if (Vertical)
                ((FrameworkElement)Child).Height = finalSize.Height;
            Child.Arrange(new Rect(finalSize));
            return finalSize;
        }
    }
}
