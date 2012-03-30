using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Documents;
using System.Windows.Media;
using System.ComponentModel;
using System.Globalization;
using Signum.Entities.DynamicQuery;
using System.Windows.Controls;
using Signum.Utilities;
using System.Windows;

namespace Signum.Windows.DynamicQuery
{
    public class ColumnInfo
    {        
        public ColumnInfo(Column column)
        {
            Column = column;
        }

        public Column Column;
        public ResultColumn ResultColumn;
    }

    public class ColumnOrderInfo
    {
        public OrderType OrderType { get { return adorner.OrderType; } }
        SortAdorner adorner;
        public GridViewColumnHeader Header { get; set; }

        public void CleanAdorner()
        {
            var layer = AdornerLayer.GetAdornerLayer(Header);

            if (layer != null)
                layer.Remove(adorner);
        }

        public void FlipAdorner()
        {
            adorner.OrderType = adorner.OrderType == OrderType.Ascending ? OrderType.Descending : OrderType.Ascending;
            AdornerLayer.GetAdornerLayer(Header).Update();
        }

        public ColumnOrderInfo(GridViewColumnHeader header, OrderType orderType, int order)
        {
            Header = header;
            adorner = new SortAdorner(header, orderType, order) { IsHitTestVisible = false };
            if (Header.IsVisible)
                CreateAdorner(null, new DependencyPropertyChangedEventArgs());
            else
                Header.IsVisibleChanged += CreateAdorner;
        }

        void CreateAdorner(object sender, DependencyPropertyChangedEventArgs e)
        {
            Header.IsVisibleChanged -= CreateAdorner;
            
            AdornerLayer layer = AdornerLayer.GetAdornerLayer(Header);
            layer.Add(adorner);
        }
    }

    internal class SortAdorner : Adorner
    {
        private readonly static Geometry Ascending = Geometry.Parse("M 0,5 L 10,5 L 5,0 Z");
        private readonly static Geometry Descending = Geometry.Parse("M 0,0 L 10,0 L 5,5 Z");
      
        public OrderType OrderType { get; set; }
        public int Order { get; private set; }

        static Brush[] brushes = new[] { Brushes.Navy, Brushes.RoyalBlue, Brushes.DeepSkyBlue, Brushes.LightSkyBlue };

        public SortAdorner(UIElement element, OrderType orderType, int order)
            : base(element)
        {
            OrderType = orderType;
            this.Order = order;
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);

            drawingContext.PushTransform(new TranslateTransform(AdornedElement.RenderSize.Width / 2 - 5, 1));

            drawingContext.DrawGeometry(brushes[Order % brushes.Length], null, OrderType == OrderType.Ascending ? Ascending : Descending);

            drawingContext.Pop();
        }
    }

}
