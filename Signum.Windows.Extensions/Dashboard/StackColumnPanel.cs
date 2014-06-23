using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Windows;

namespace Signum.Windows.Dashboard
{

    public class StackColumnPanel : Panel
    {
        public static readonly DependencyProperty StartColumnProperty =
            DependencyProperty.RegisterAttached("StartColumn", typeof(int), typeof(StackColumnPanel), new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.AffectsParentArrange));
        public static int GetStartColumn(DependencyObject obj)
        {
            return (int)obj.GetValue(StartColumnProperty);
        }
        public static void SetStartColumn(DependencyObject obj, int value)
        {
            obj.SetValue(StartColumnProperty, value);
        }

        public static readonly DependencyProperty ColumnsProperty =
            DependencyProperty.RegisterAttached("Columns", typeof(int), typeof(StackColumnPanel), new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.AffectsParentArrange));
        public static int GetColumns(DependencyObject obj)
        {
            return (int)obj.GetValue(ColumnsProperty);
        }
        public static void SetColumns(DependencyObject obj, int value)
        {
            obj.SetValue(ColumnsProperty, value);
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
            foreach (UIElement item in this.InternalChildren.Cast<UIElement>())
            {
                var cols = GetColumns(item);

                item.Measure(new Size((availableSize.Width / 12) * cols, availableSize.Height));
            }

            Size maxSize = this.InternalChildren.Cast<UIElement>()
                .GroupBy(e => GetRow(e))
                .Select(gr => new Size(gr.Max(a => a.DesiredSize.Width / Math.Max(1, GetColumns(a)) * 12), gr.Sum(a => a.DesiredSize.Height)))
                .Aggregate(new Size(), (ac, size) => new Size(Math.Max(ac.Width, size.Width), ac.Height + size.Height));

            return maxSize;
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            var groups = this.InternalChildren.Cast<UIElement>()
               .GroupBy(a => GetRow(a))
               .OrderBy(a => a.Key).ToList();

            double yPos = 0;
            foreach (var gr in groups)
            {
                double maxRowHeight = 0;
                foreach (var item in gr)
                {
                    var cols = GetColumns(item);
                    var startCol = GetStartColumn(item);

                    item.Arrange(new Rect(
                        new Point((finalSize.Width / 12) * startCol, yPos),
                        new Size((finalSize.Width / 12) * cols, item.DesiredSize.Height)));

                    maxRowHeight = Math.Max(maxRowHeight, item.DesiredSize.Height);
                }

                yPos += maxRowHeight;
            }

            return finalSize;
        }
    }
}
