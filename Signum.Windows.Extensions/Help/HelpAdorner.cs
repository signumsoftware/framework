using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using Signum.Utilities;

namespace Signum.Windows.Help
{
    public class HelpAdorner : Adorner
    {
        public HelpAdorner(UIElement element)
            : base(element)
        {
            this.MouseEnter += HelpAdorner_MouseEnter;
            this.MouseLeave += HelpAdorner_MouseLeave;
            this.MouseDown += HelpAdorner_MouseDown;
        }

        void HelpAdorner_MouseDown(object sender, MouseButtonEventArgs e)
        {
            var hi = HelpClient.GetHelpInfo(this.AdornedElement);

            if (hi != null && hi.Link != null)
                Process.Start(hi.Link);
        }

        void HelpAdorner_MouseLeave(object sender, MouseEventArgs e)
        {
            this.InvalidateVisual();
        }

        void HelpAdorner_MouseEnter(object sender, MouseEventArgs e)
        {
            this.InvalidateVisual();
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            var hi = HelpClient.GetHelpInfo(this.AdornedElement);

            var color = hi.Description.HasText() ? Colors.DarkOrange : Colors.Purple;

            // Some arbitrary drawing implements.
            SolidColorBrush renderBrush = new SolidColorBrush(color.Alpha(this.IsMouseOver ? 0.6f : 0.3f));
            Pen renderPen = new Pen(new SolidColorBrush(color.Alpha(this.IsMouseOver ? 1f : 0.5f)), 1);

            // Draw a circle at each corner.
            drawingContext.DrawRectangle(renderBrush, renderPen, new Rect(new Point(), this.AdornedElement.RenderSize));
        }
    }
}
