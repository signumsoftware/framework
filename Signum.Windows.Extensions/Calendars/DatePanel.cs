using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Windows;
using Signum.Utilities;
using Signum.Utilities.DataStructures;

namespace Signum.Windows.Calendars
{
    public class DatePanel:Panel
    {
        public static readonly DependencyProperty MinProperty =
            DependencyProperty.Register("Min", typeof(DateTime), typeof(DatePanel), new FrameworkPropertyMetadata(DateTime.Today.AddMonths(-1), (d,e)=>((DatePanel)d).InvalidateNotAutoMinMax()));

        public DateTime Min
        {
            get { return (DateTime)GetValue(MinProperty); }
            set { SetValue(MinProperty, value); }
        }

        public static readonly DependencyProperty MaxProperty =
          DependencyProperty.Register("Max", typeof(DateTime), typeof(DatePanel), new FrameworkPropertyMetadata(DateTime.Today.AddMonths(1), (d, e) => ((DatePanel)d).InvalidateNotAutoMinMax()));
        public DateTime Max
        {
            get { return (DateTime)GetValue(MaxProperty); }
            set { SetValue(MaxProperty, value); }
        }


        public static readonly RoutedEvent ElementsMinMaxChangedEvent = EventManager.RegisterRoutedEvent(
            "ElementsMinMaxChanged", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(DatePanel));
        public event RoutedEventHandler ElementsMinMaxChanged
        {
            add { AddHandler(ElementsMinMaxChangedEvent, value); }
            remove { RemoveHandler(ElementsMinMaxChangedEvent, value); }
        }

        private void InvalidateNotAutoMinMax()
        {
            if (!AutoMinMax)
            {
                InvalidateMeasure();
                InvalidateArrange(); 
            }
        }


        public static readonly DependencyProperty DayWidthProperty =
          DependencyProperty.Register("DayWidth", typeof(double), typeof(DatePanel), new FrameworkPropertyMetadata(32.0, (d, e) => ((DatePanel)d).InvalidateNotAutoDayWidth()));
        public double DayWidth
        {
            get { return (double)GetValue(DayWidthProperty); }
            set { SetValue(DayWidthProperty, value); }
        }


        private void InvalidateNotAutoDayWidth()
        {
            if (!AutoDayWidth)
            {
                InvalidateMeasure();
                InvalidateArrange();
            }
        }

        public static readonly DependencyProperty GridBehaviorProperty =
          DependencyProperty.Register("GridBehavior", typeof(bool), typeof(DatePanel), new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsArrange));
        public bool GridBehavior
        {
            get { return (bool)GetValue(GridBehaviorProperty); }
            set { SetValue(GridBehaviorProperty, value); }
        }

        public static readonly DependencyProperty AutoDayWidthProperty =
          DependencyProperty.Register("AutoDayWidth", typeof(bool), typeof(DatePanel), new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsArrange));
        public bool AutoDayWidth
        {
            get { return (bool)GetValue(AutoDayWidthProperty); }
            set { SetValue(AutoDayWidthProperty, value); }
        }

        public static readonly DependencyProperty AutoMinMaxProperty =
          DependencyProperty.Register("AutoMinMax", typeof(bool), typeof(DatePanel), new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsArrange));
        public bool AutoMinMax
        {
            get { return (bool)GetValue(AutoMinMaxProperty); }
            set { SetValue(AutoMinMaxProperty, value); }
        }

        public static int GetRow(DependencyObject obj)
        {
            return (int)obj.GetValue(RowProperty);
        }
        public static void SetRow(DependencyObject obj, int value)
        {
            obj.SetValue(RowProperty, value);
        }
        // Using a DependencyProperty as the backing store for Row.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty RowProperty =
            DependencyProperty.RegisterAttached("Row", typeof(int), typeof(DatePanel), new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.AffectsParentArrange | FrameworkPropertyMetadataOptions.AffectsParentMeasure));



        public static DateTime? GetDate(DependencyObject obj)
        {
            return (DateTime?)obj.GetValue(DateProperty);
        }
        public static void SetDate(DependencyObject obj, DateTime? value)
        {
            obj.SetValue(DateProperty, value);
        }
        // Using a DependencyProperty as the backing store for Date.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty DateProperty =
            DependencyProperty.RegisterAttached("Date", typeof(DateTime?), typeof(DatePanel), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsParentArrange | FrameworkPropertyMetadataOptions.AffectsParentMeasure));



        public static DateTime? GetDateMin(DependencyObject obj)
        {
            return (DateTime?)obj.GetValue(DateMinProperty);
        }
        public static void SetDateMin(DependencyObject obj, DateTime? value)
        {
            obj.SetValue(DateMinProperty, value);
        }
        // Using a DependencyProperty as the backing store for DateMin.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty DateMinProperty =
            DependencyProperty.RegisterAttached("DateMin", typeof(DateTime?), typeof(DatePanel), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsParentArrange | FrameworkPropertyMetadataOptions.AffectsParentMeasure));



        public static DateTime? GetDateMax(DependencyObject obj)
        {
            return (DateTime?)obj.GetValue(DateMaxProperty);
        }
        public static void SetDateMax(DependencyObject obj, DateTime? value)
        {
            obj.SetValue(DateMaxProperty, value);
        }
        // Using a DependencyProperty as the backing store for DateMax.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty DateMaxProperty =
            DependencyProperty.RegisterAttached("DateMax", typeof(DateTime?), typeof(DatePanel), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsParentArrange | FrameworkPropertyMetadataOptions.AffectsParentMeasure));


        double[] rowsPosition;
        double[] rowsHeight;

        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            base.OnRenderSizeChanged(sizeInfo);
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            UpdateAutoMinMax();

            foreach (UIElement item in this.InternalChildren)
            {
                item.Measure(new Size(DateWidth(GetDateMin(item) ?? Min, GetDateMax(item) ?? Max), availableSize.Height));
            }

            double widthSum = DateWidth(Min, Max);  //InternalChildren.Cast<UIElement>().Sum(a => a.DesiredSize.Width);
            double width = AutoDayWidth ? availableSize.Width : widthSum;

            if (width == double.PositiveInfinity) width = 1000; 

            double height = GridBehavior ? InternalChildren.Cast<UIElement>().GroupBy(e => GetRow(e)).Select(g => g.Max(e => e.DesiredSize.Height)).Sum() :
                                           InternalChildren.Cast<UIElement>().Sum(a => a.DesiredSize.Height);

            return new Size(width, height);
        }

        double DateWidth(DateTime min, DateTime max)
        {
            int days = (max - min).Days;
            return DayWidth * Math.Max(days, 0);  
        }

        Interval<DateTime> lastDates; 

        private void UpdateAutoMinMax()
        {
            var dates = ElementsMinMax();

            if (!dates.Equals(lastDates))
            {
                lastDates = dates;
                RaiseEvent(new RoutedEventArgs(ElementsMinMaxChangedEvent));
            }

            if (AutoMinMax && dates.Min != DateTime.MinValue && dates.Max != DateTime.MinValue)
            {
                TimeSpan margin = new TimeSpan((dates.Max - dates.Min).Days / 10, 0, 0, 0);
                Min = dates.Min - margin;
                Max = dates.Max + margin;
            }
        }

        public Interval<DateTime> ElementsMinMax()
        {
            var dates = InternalChildren.Cast<UIElement>().SelectMany(ui => new[] { GetDate(ui), GetDateMin(ui), GetDateMax(ui) }).NotNull().ToInterval();
            return dates;
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            UpdateAutoMinMax();

            if (AutoDayWidth)
            {
                int days = (Max - Min).Days;
                DayWidth = days == 0 ? 0 : finalSize.Width/days;
            }

            if (GridBehavior)
            {
                var groups = InternalChildren.Cast<UIElement>().GroupBy(a => GetRow(a));
                
                rowsHeight = new double[groups.Max(gr => gr.Key)];
                foreach (var gr in groups)
                {
                    rowsHeight[gr.Key] = gr.Max(a => a.DesiredSize.Height);
                }
                rowsPosition = rowsHeight.SelectAggregate(0.0, (a, b) => a + b).ToArray();               
            }

            double acumY = 0; 
            foreach (UIElement item in InternalChildren)
            {
                double posX, posY, width, height;

                if (GetDate(item).HasValue)
                {
                    posX =  DateWidth(Min, GetDate(item).Value) + DayWidth / 2 - item.DesiredSize.Width / 2;
                    width = item.DesiredSize.Width;
                }
                else 
                {
                    posX = DateWidth(Min, GetDateMin(item) ?? Min);
                    width = DateWidth(GetDateMin(item) ?? Min,GetDateMax(item) ?? Max);
                }

                if (GridBehavior)
                {
                    posY = rowsPosition[GetRow(item)];
                    height = rowsHeight[GetRow(item)];
                }
                else
                {
                    posY = acumY;
                    height = item.DesiredSize.Height;
                    acumY += height;
                }

                item.Arrange(new Rect(posX, posY, width, height)); 
            }

            double totalWidth = AutoDayWidth ? finalSize.Width : DateWidth(Min, Max);

            if (GridBehavior)
                return new Size(totalWidth, rowsPosition[rowsPosition.Length - 1]);
            else
                return new Size(totalWidth, acumY); 
        }
    }
}