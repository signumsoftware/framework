using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Documents;
using System.Windows.Media;
using System.ComponentModel;
using System.Globalization;
using System.Windows.Controls;
using Signum.Entities.DynamicQuery;
using System.Windows;

namespace Signum.Windows.DynamicQuery
{
    internal class ColumnOrderInfo
    {
        public SortAdorner Adorner { get; private set; }
        public GridViewColumnHeader Header { get; set; }

        public void Clean()
        {
            AdornerLayer.GetAdornerLayer(Header).Remove(Adorner);
        }

        public void Flip()
        {
            Adorner.OrderType = Adorner.OrderType == OrderType.Ascending ? OrderType.Descending : OrderType.Ascending;
            AdornerLayer.GetAdornerLayer(Header).Update();
        }

        public ColumnOrderInfo(GridViewColumnHeader header,  OrderType orderType, int? order)
        {
            Header = header;
            Adorner = new SortAdorner(header, orderType, order);
            AdornerLayer.GetAdornerLayer(Header).Add(Adorner);
        }

        public Order ToOrder()
        {
            return new Order(((Column)Header.Tag).Name, Adorner.OrderType);
        }
    }

    internal class SortAdorner : Adorner
    {
        private readonly static Geometry Ascending = Geometry.Parse("M 0,5 L 8,5 L 4,0 Z");
        private readonly static Geometry Descending = Geometry.Parse("M 0,0 L 8,0 L 4,5 Z");
      
        public OrderType OrderType { get; set; }
        public int? Order { get; private set; }

        public SortAdorner(UIElement element, OrderType orderType, int? order)
            : base(element)
        {
            OrderType = orderType;
            this.Order = order;
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);

            if (AdornedElement.RenderSize.Width < 20)
                return;

            drawingContext.PushTransform(new TranslateTransform(AdornedElement.RenderSize.Width - 12, (AdornedElement.RenderSize.Height - 5) / 2));

            drawingContext.DrawGeometry(Brushes.DarkBlue, null, OrderType == OrderType.Ascending ? Ascending : Descending);
            if (Order.HasValue)
                drawingContext.DrawText(new FormattedText(Order.Value.ToString(), CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface("MS Sans Serif"), 9, Brushes.DarkBlue), new Point(-8, -5));
            drawingContext.Pop();
        }
    }

}
