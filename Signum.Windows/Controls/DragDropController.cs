using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Input;

namespace Signum.Windows
{
    public class DragController
    {
        Point? startPoint;
        //bool isDragging = false; 
        Func<FrameworkElement, object> dataObjectFactory = null;
        DragDropEffects allowedEffects;

        public event Action<FrameworkElement, DragDropEffects> Dropped;

        public DragController(Func<FrameworkElement, object> dataObjectFactory, DragDropEffects allowedEffects)
        {
            this.dataObjectFactory = dataObjectFactory;
            this.allowedEffects = allowedEffects;
        }

        public DragController(FrameworkElement dragElement, object dataObject, DragDropEffects allowedEffects)
        {
            this.dataObjectFactory = _ => dataObject;
            this.allowedEffects = allowedEffects;
            Subscribe(dragElement);
        }

        private void Subscribe(FrameworkElement dragElement)
        {
            dragElement.MouseLeftButtonDown += new MouseButtonEventHandler(dragElement_MouseLeftButtonDown);
            dragElement.MouseMove += new MouseEventHandler(dragElement_MouseMove);
        }

        private void Unsubscribe(FrameworkElement dragElement)
        {
            dragElement.MouseLeftButtonDown -= new MouseButtonEventHandler(dragElement_MouseLeftButtonDown);
            dragElement.MouseMove -= new MouseEventHandler(dragElement_MouseMove);
        }

        void dragElement_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            startPoint = e.GetPosition(null);
        }

        void dragElement_MouseMove(object sender, MouseEventArgs e)
        {
            FrameworkElement dragElement = (FrameworkElement)sender;
            if (startPoint != null && Mouse.LeftButton == MouseButtonState.Pressed)
            {

                Point currentPos = e.GetPosition(null);
                if ((Math.Abs(currentPos.X - startPoint.Value.X) > SystemParameters.MinimumHorizontalDragDistance) ||
                    (Math.Abs(currentPos.Y - startPoint.Value.Y) > SystemParameters.MinimumVerticalDragDistance))
                {
                    object dataObject = dataObjectFactory(dragElement);

                    startPoint = null;

                    DragDropEffects de = DragDrop.DoDragDrop(dragElement, dataObject, allowedEffects); //Leap of faith

                    Dropped?.Invoke(dragElement, de);
                }
            }
            else
            {
                startPoint = null;
            }
        }


        public static readonly DependencyProperty DragControllerProperty =
            DependencyProperty.RegisterAttached("DragController", typeof(DragController), typeof(DragController), new UIPropertyMetadata(null,
                (d, e) =>
                {
                    var oldDrag = (DragController)e.OldValue;
                    if (oldDrag != null)
                        oldDrag.Unsubscribe((FrameworkElement)d);

                    var newDrag = (DragController)e.NewValue;
                    if (newDrag != null)
                        newDrag.Subscribe((FrameworkElement)d);
                }));
        public static DragController GetDragController(DependencyObject obj)
        {
            return (DragController)obj.GetValue(DragControllerProperty);
        }
        public static void SetDragController(DependencyObject obj, DragController value)
        {
            obj.SetValue(DragControllerProperty, value);
        }
    }
}
