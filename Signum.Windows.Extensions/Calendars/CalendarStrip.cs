using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using System.Windows;
using System.Windows.Controls;

namespace Signum.Windows.Calendars
{
    public class CalendarStrip : UserControl
    {
        public static readonly DependencyProperty ItemsSourceProperty =
          DependencyProperty.Register("ItemsSource", typeof(IEnumerable), typeof(CalendarStrip), new UIPropertyMetadata(null, (d,e)=>((CalendarStrip)d).RecalculateMinMax()));
        public IEnumerable ItemsSource
        {
            get { return (IEnumerable)GetValue(ItemsSourceProperty); }
            set { SetValue(ItemsSourceProperty, value); }
        }

        public static readonly DependencyProperty MinProperty =
        DependencyProperty.Register("Min", typeof(DateTime), typeof(CalendarStrip), new UIPropertyMetadata(DateTime.Today.AddMonths(-1)));
        public DateTime Min
        {
            get { return (DateTime)GetValue(MinProperty); }
            set { SetValue(MinProperty, value); }
        }

        public static readonly DependencyProperty MaxProperty =
          DependencyProperty.Register("Max", typeof(DateTime), typeof(CalendarStrip), new UIPropertyMetadata(DateTime.Today.AddMonths(1)));
        public DateTime Max
        {
            get { return (DateTime)GetValue(MaxProperty); }
            set { SetValue(MaxProperty, value); }
        }

        public static readonly DependencyProperty AutoMinMaxProperty =
            DependencyProperty.Register("AutoMinMax", typeof(bool), typeof(CalendarStrip), new UIPropertyMetadata(false, (d, e) => ((CalendarStrip)d).TryRecalculateMinMax()));
        public bool AutoMinMax
        {
            get { return (bool)GetValue(AutoMinMaxProperty); }
            set { SetValue(AutoMinMaxProperty, value); }
        }

        public static readonly DependencyProperty DayWidthProperty =
          DependencyProperty.Register("DayWidth", typeof(double), typeof(CalendarStrip), new FrameworkPropertyMetadata(32.0));
        public double DayWidth
        {
            get { return (double)GetValue(DayWidthProperty); }
            set { SetValue(DayWidthProperty, value); }
        }

        public static readonly DependencyProperty AutoDayWidthProperty =
          DependencyProperty.Register("AutoDayWidth", typeof(bool), typeof(CalendarStrip), new FrameworkPropertyMetadata(true, (d, e) => ((CalendarStrip)d).RecalculateDayWidth()));
        public bool AutoDayWidth
        {
            get { return (bool)GetValue(AutoDayWidthProperty); }
            set { SetValue(AutoDayWidthProperty, value); }
        }


        public static readonly RoutedEvent CalculateMinMaxEvent = EventManager.RegisterRoutedEvent(
            "CalculateMinMax", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(CalendarStrip));
        public event RoutedEventHandler CalculateMinMax
        {
            add { AddHandler(CalculateMinMaxEvent, value); }
            remove { RemoveHandler(CalculateMinMaxEvent, value); }
        }



        public CalendarStrip()
        {
            this.SizeChanged += new SizeChangedEventHandler(CalendarStrip_SizeChanged);
        }

        void CalendarStrip_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (e.WidthChanged)
            {
                RecalculateDayWidth();
            }
        }

        public virtual void RecalculateDayWidth() { }

        protected virtual void RecalculateMinMax()
        {
            RaiseEvent(new RoutedEventArgs(CalculateMinMaxEvent)); 
        }

        protected void TryRecalculateMinMax()
        {
            if (AutoMinMax)
            {
                RecalculateMinMax(); 
            }
        }
        
    }
}
