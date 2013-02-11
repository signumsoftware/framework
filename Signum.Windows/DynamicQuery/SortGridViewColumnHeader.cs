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
using System.Windows.Input;
using System.Windows.Automation.Peers;

namespace Signum.Windows.DynamicQuery
{
    public class SortGridViewColumnHeader : GridViewColumnHeader
    {
        public Column RequestColumn;
        public ResultColumn ResultColumn;

        internal SortAdorner sortAdorner;

        void CleanAdorner()
        {
            var layer = AdornerLayer.GetAdornerLayer(this);

            if (layer != null)
                layer.Remove(sortAdorner);

            sortAdorner = null;
        }

        void FlipAdorner()
        {
            sortAdorner.OrderType = sortAdorner.OrderType == OrderType.Ascending ? OrderType.Descending : OrderType.Ascending;
            AdornerLayer.GetAdornerLayer(this).Update();
        }

        void CreateAdorner(OrderType orderType, int order)
        {
            sortAdorner = new SortAdorner(this, orderType, order) { IsHitTestVisible = false };
            if (this.IsVisible)
                CreateAdorner(null, new DependencyPropertyChangedEventArgs());
            else
                this.IsVisibleChanged += CreateAdorner;
        }

        void CreateAdorner(object sender, DependencyPropertyChangedEventArgs e)
        {
            this.IsVisibleChanged -= CreateAdorner;
            
            AdornerLayer layer = AdornerLayer.GetAdornerLayer(this);
            layer.Add(sortAdorner);
        }

        public static void SetColumnAdorners(GridView gvResults, IList<OrderOption> orderOptions)
        {
            for (int i = 0; i < orderOptions.Count; i++)
            {
                OrderOption oo = orderOptions[i];

                var fullKey = oo.Token.FullKey();

                SortGridViewColumnHeader header = gvResults.Columns
                    .Select(c => (SortGridViewColumnHeader)c.Header)
                    .FirstOrDefault(c => c.RequestColumn.Name == fullKey);

                if (header != null)
                {
                    header.CreateAdorner(oo.OrderType, i);
                    oo.Header = header;
                }
            }
        }

        public void ChangeOrders(IList<OrderOption> orderOptions)
        {
            if ((Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift || (orderOptions.Count == 1 && orderOptions[0].Header == this))
            {

            }
            else
            {
                foreach (var oo in orderOptions)
                {
                    if (oo.Header != null)
                        oo.Header.CleanAdorner();
                }

                orderOptions.Clear();
            }

            OrderOption order = orderOptions.SingleOrDefaultEx(oo => oo.Header == this);
            if (order != null)
            {
                order.Header.FlipAdorner();
                order.OrderType = order.Header.sortAdorner.OrderType;
            }
            else
            {
                this.CreateAdorner(OrderType.Ascending, orderOptions.Count);

                orderOptions.Add(new OrderOption()
                {
                    Token = this.RequestColumn.Token,
                    OrderType = OrderType.Ascending,
                    Header = this,
                });
            }
        }

        protected override AutomationPeer OnCreateAutomationPeer()
        {
            return new SortGridViewColumnHeaderAutomationPeer(this);
        }
    }

    public class SortGridViewColumnHeaderAutomationPeer : GridViewColumnHeaderAutomationPeer
    {
        public SortGridViewColumnHeaderAutomationPeer(SortGridViewColumnHeader header)
            : base(header)
        {
        }

        protected override string GetNameCore()
        {
            return ((SortGridViewColumnHeader)Owner).RequestColumn.Token.FullKey();
        }

        protected override string GetItemStatusCore()
        {
            var adorner = ((SortGridViewColumnHeader)Owner).sortAdorner;

            if (adorner == null)
                return "";

            return adorner.OrderType.ToString();
        }
    }

    public class SortAdorner : Adorner
    {
        private readonly static Geometry Ascending = Geometry.Parse("M 0,5 L 10,5 L 5,0 Z");
        private readonly static Geometry Descending = Geometry.Parse("M 0,0 L 10,0 L 5,5 Z");
      
        public OrderType OrderType { get; set; }
        public int Priority { get; private set; }

        static Brush[] brushes = new[] { Brushes.Navy, Brushes.RoyalBlue, Brushes.DeepSkyBlue, Brushes.LightSkyBlue };

        public SortAdorner(UIElement element, OrderType orderType, int order)
            : base(element)
        {
            OrderType = orderType;
            this.Priority = order;
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);

            drawingContext.PushTransform(new TranslateTransform(AdornedElement.RenderSize.Width / 2 - 5, 1));

            drawingContext.DrawGeometry(brushes[Priority % brushes.Length], null, OrderType == OrderType.Ascending ? Ascending : Descending);

            drawingContext.Pop();
        }
    }
}
