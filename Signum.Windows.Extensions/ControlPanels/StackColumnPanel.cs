using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Windows;

namespace Signum.Windows.ControlPanels
{
    
    public class StackColumnPanel : Panel
    {
        public static readonly DependencyProperty ColumnProperty =
            DependencyProperty.RegisterAttached("Column", typeof(int), typeof(StackColumnPanel), new FrameworkPropertyMetadata(0,FrameworkPropertyMetadataOptions.AffectsParentArrange));
        public static int GetColumn(DependencyObject obj)
        {
            return (int)obj.GetValue(ColumnProperty);
        }

        public static void SetColumn(DependencyObject obj, int value)
        {
            obj.SetValue(ColumnProperty, value);
        }


        public static readonly DependencyProperty RowProperty =
            DependencyProperty.RegisterAttached("Row", typeof(int), typeof(StackColumnPanel), new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.AffectsParentArrange));
        public static int GetRow(DependencyObject obj)
        {
            return (int)obj.GetValue(RowProperty);
        }

        public static void SetRow(DependencyObject obj, int value)
        {
            obj.SetValue(RowProperty, value);
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            foreach (UIElement item in this.InternalChildren)
            {
                item.Measure(availableSize);
            }

            Size maxSize = this.InternalChildren.Cast<UIElement>()
                .GroupBy(a => GetColumn(a))
                .Select(gr => new Size(gr.Max(a => a.DesiredSize.Width), gr.Sum(a => a.DesiredSize.Height)))
                .Aggregate(new Size(), (ac, size) => new Size(ac.Width + size.Width, Math.Max(ac.Height, size.Height)));

            return maxSize;
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            var groups = this.InternalChildren.Cast<UIElement>()
               .GroupBy(a => GetColumn(a))
               .OrderBy(a => a.Key).ToList();

            var colWidh = finalSize.Width / groups.Count;

            double xPos = 0;
            foreach (var gr in groups)
            {
                double yPos = 0;

                foreach (var item in gr.OrderBy(a=>GetRow(a)))
                {
                    item.Arrange(new Rect(
                        new Point(xPos, yPos), 
                        new Size(colWidh, item.DesiredSize.Height)));

                    yPos += item.DesiredSize.Height;
                }

                xPos += colWidh;
            }

            return finalSize;
        }
    }
}
