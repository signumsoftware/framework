using System;
using System.IO;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Navigation;
using System.Linq;
using Signum.Utilities;
using System.Globalization;
using System.Collections.Generic;
using Signum.Windows;
using Signum.Utilities.DataStructures;
using System.Windows.Shapes;

namespace Signum.Windows.Calendars
{
	public partial class CalendarStripWallpaper
	{
        public static readonly DependencyProperty MinProperty =
          DependencyProperty.Register("Min", typeof(DateTime), typeof(CalendarStripWallpaper), new FrameworkPropertyMetadata(DateTime.Today.AddMonths(-1), FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsArrange, (d, e) => ((CalendarStripWallpaper)d).ReconstruirDatos(), (d, v) => ((DateTime)v).Date));
        public DateTime Min
        {
            get { return (DateTime)GetValue(MinProperty); }
            set { SetValue(MinProperty, value); }
        }

        public static readonly DependencyProperty MaxProperty =
          DependencyProperty.Register("Max", typeof(DateTime), typeof(CalendarStripWallpaper), new FrameworkPropertyMetadata(DateTime.Today.AddMonths(1), FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsArrange, (d, e) => ((CalendarStripWallpaper)d).ReconstruirDatos(), (d, v) => ((DateTime)v).Date));
        public DateTime Max
        {
            get { return (DateTime)GetValue(MaxProperty); }
            set { SetValue(MaxProperty, value); }
        }

        public static readonly DependencyProperty DayWidthProperty =
          DependencyProperty.Register("DayWidth", typeof(double), typeof(CalendarStripWallpaper), new FrameworkPropertyMetadata(32.0, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsArrange));
        public double DayWidth
        {
            get { return (double)GetValue(DayWidthProperty); }
            set { SetValue(DayWidthProperty, value); }
        }


        public CalendarStripWallpaper()
		{
			this.InitializeComponent();

            this.Loaded += new RoutedEventHandler(CalendarStripHeader_Loaded);
			// Insert code required on object creation below this point.

            day = (Style)FindResource("day");
            weekend = (Style)FindResource("weekend");
            month = (Style)FindResource("month");
            year = (Style)FindResource("year");
            today = (Style)FindResource("today");
		}

        static Style day;
        static Style weekend;
        static Style month;
        static Style year; 
        static Style today; 

        void CalendarStripHeader_Loaded(object sender, RoutedEventArgs e)
        {
            ConstruirTemplates();
            ReconstruirDatos();
        }

        [Flags]
        enum Part
        {
            YearsMode,
            YearsBorder ,
            MonthsBorder ,
            DaysBorder ,
        }

        static readonly Dictionary<Part, Func<double, bool>> isVisible = new Dictionary<Part, Func<double, bool>>()
        {
            {Part.YearsMode, d => d < 3},
            {Part.YearsBorder, d => true }, 
            {Part.MonthsBorder, d => 0.1 < d },
            {Part.DaysBorder, d => 3 < d},
        };

        public class MonthGroup
        {
            public int Month { get; set; }
            public int Year { get; set; }
            public int Count { get; set; }
        }

        public class YearGroup
        {
            public int Year { get; set; }
            public int 
                Count{ get; set; }
        }

        public class Day
        {
            public DateTime Value;

            public string DayFullToolTip
            {
                get { return Value.ToLongDateString(); }
            }

            public Style Style
            {
                get
                {
                    if (Value.DayOfWeek == DayOfWeek.Saturday || Value.DayOfWeek == DayOfWeek.Sunday)
                        return weekend;

                    if (Value == DateTime.Today)
                        return today;

                    return day;
                }
            }
        }

 

        private void ReconstruirDatos()
        {
            DateTime[] days = Min.For(d => d <= Max, d => d.AddDays(1)).ToArray();

            icYears.ItemsSource = days.GroupCount(a => a.Year).Select(p => new YearGroup { Year = p.Key, Count = p.Value }).ToList();
            icMonths.ItemsSource = days.GroupCount(a => new { a.Month, a.Year }).Select(p => new MonthGroup { Month = p.Key.Month, Year = p.Key.Year, Count = p.Value }).ToList(); ;
            icDays.ItemsSource = days.Select(a => new Day { Value = a }).ToList();
        }

        public Binding BindingToDayWidth<T>(Func<double,T> converter )
        {
            return new Binding
            {
                Source = this,
                Path = new PropertyPath(DayWidthProperty),
                Converter = ConverterFactory.New(converter)
            };
        }

        public MultiBinding MultiBindingToDayWidth<T,S>(Binding other, Func<double, T, S> converter)
        {
            MultiBinding mb = new MultiBinding
            {
                Converter = ConverterFactory.New(converter)
            };

            mb.Bindings.Add(new Binding
            {
                Source = this,
                Path = new PropertyPath(DayWidthProperty)
            });

            mb.Bindings.Add(other);
            return mb; 
        }

        private void ConstruirTemplates()
        {
            icYears.SetBinding(ItemsControl.VisibilityProperty, BindingToDayWidth(d => isVisible[Part.YearsMode](d) ? Visibility.Visible : Visibility.Collapsed));
            icMonths.SetBinding(ItemsControl.VisibilityProperty, BindingToDayWidth(d => isVisible[Part.MonthsBorder](d) ? Visibility.Visible : Visibility.Collapsed));
            icDays.SetBinding(ItemsControl.VisibilityProperty, BindingToDayWidth(d => isVisible[Part.YearsMode](d) ? Visibility.Collapsed : Visibility.Visible));

            icYears.ItemTemplate = new DataTemplate(typeof(YearGroup))
            {
                VisualTree = new FrameworkElementFactory(typeof(Rectangle))
                .Do(f =>
                {
                    f.SetValue(Rectangle.StyleProperty, year);

                    f.SetBinding(Rectangle.WidthProperty, MultiBindingToDayWidth(new Binding("Count"), (double d, int count) => d * count));
                })
            };


            icMonths.ItemTemplate = new DataTemplate(typeof(MonthGroup))
            {
                VisualTree = new FrameworkElementFactory(typeof(Rectangle))
                .Do(f =>
                {
                    f.SetValue(Rectangle.StyleProperty, month);

                    f.SetBinding(Rectangle.WidthProperty, MultiBindingToDayWidth(new Binding("Count"), (double d, int count) => d * count));
                })
            };


            icDays.ItemTemplate = new DataTemplate(typeof(Day))
            {
                VisualTree = new FrameworkElementFactory(typeof(Rectangle))
                .Do(f =>
                {
                    f.SetBinding(Rectangle.StyleProperty, new Binding("Style"));

                    f.SetValue(Rectangle.WidthProperty, new Binding { Source = this, Path = new PropertyPath(DayWidthProperty) });
                    f.SetBinding(Rectangle.ToolTipProperty, new Binding("DayFullToolTip"));

                    f.SetBinding(Rectangle.StrokeThicknessProperty, BindingToDayWidth(d => isVisible[Part.DaysBorder](d) ? new Thickness(1) : new Thickness(0)));
                })
            };


        }

        private Tuple<DateTime,DateTime> GetWeek(DateTime d)
        {
            int spanishDayOfWeek = ((int)d.DayOfWeek + 6) % 7;
            return new Tuple<DateTime, DateTime>(d.AddDays(-spanishDayOfWeek), d.AddDays(6 - spanishDayOfWeek)); 
        }
	}
}