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
using System.ComponentModel;

#pragma warning disable 0067

namespace Signum.Windows.Calendars
{
	public partial class CalendarStripHeader
	{
        public static readonly DependencyProperty MinProperty =
          DependencyProperty.Register("Min", typeof(DateTime), typeof(CalendarStripHeader), new FrameworkPropertyMetadata(DateTime.Today.AddMonths(-1), FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsArrange, (d, e) => ((CalendarStripHeader)d).ReconstruirDatos(), (d, v) => ((DateTime)v).Date));
        public DateTime Min
        {
            get { return (DateTime)GetValue(MinProperty); }
            set { SetValue(MinProperty, value); }
        }

        public static readonly DependencyProperty MaxProperty =
          DependencyProperty.Register("Max", typeof(DateTime), typeof(CalendarStripHeader), new FrameworkPropertyMetadata(DateTime.Today.AddMonths(1), FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsArrange, (d, e) => ((CalendarStripHeader)d).ReconstruirDatos(), (d, v) => ((DateTime)v).Date));
        public DateTime Max
        {
            get { return (DateTime)GetValue(MaxProperty); }
            set { SetValue(MaxProperty, value); }
        }

        public static readonly DependencyProperty DayWidthProperty =
          DependencyProperty.Register("DayWidth", typeof(double), typeof(CalendarStripHeader), new FrameworkPropertyMetadata(32.0, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsArrange));
        public double DayWidth
        {
            get { return (double)GetValue(DayWidthProperty); }
            set { SetValue(DayWidthProperty, value); }
        }


		public CalendarStripHeader()
		{
			this.InitializeComponent();

            this.Loaded += new RoutedEventHandler(CalendarStripHeader_Loaded);
			// Insert code required on object creation below this point.

            todayLabel = (Style)FindResource("TodayLabel");
            darkLabel = (Style)FindResource("DarkLabel");
            weekLabel = (Style)FindResource("WeekLabel"); 
            normalLabel = (Style)FindResource(typeof(Label)); 
		}

        static Style todayLabel;
        static Style darkLabel;
        static Style weekLabel; 
        static Style normalLabel; 

        void CalendarStripHeader_Loaded(object sender, RoutedEventArgs e)
        {
            ConstruirTemplates();
            ReconstruirDatos();
        }

        static Dictionary<DayOfWeek, char> dayNames = new Dictionary<DayOfWeek, char>
        {
            {DayOfWeek.Monday, 'L' },
            {DayOfWeek.Tuesday, 'M' },
            {DayOfWeek.Wednesday, 'X' },
            {DayOfWeek.Thursday, 'J' },
            {DayOfWeek.Friday, 'V' },
            {DayOfWeek.Saturday, 'S' },
            {DayOfWeek.Sunday, 'D' },
        };

        static string[] monthNames = CultureInfo.CurrentCulture.DateTimeFormat.MonthGenitiveNames.Select(a => a.FirstUpper()).ToArray();

        [Flags]
        enum Part
        {
            YearsMode,
            YearsLabel,
            YearsBorder ,
            MonthsLabel ,
            MonthsYearsLabel,
            MonthsBorder ,
            WeeksShown ,
            WeeksLabel ,
            DaysBorder ,
            DayNumberLabel ,
            DayWeekLabel ,
        }

        static readonly Dictionary<Part, Func<double, bool>> isVisible = new Dictionary<Part, Func<double, bool>>()
        {
            {Part.YearsMode, d => d < 3},
            {Part.YearsLabel, d => 0.1 < d},
            {Part.YearsBorder, d => 0.01<d }, 
            {Part.MonthsLabel, d => 1 <= d },
            {Part.MonthsYearsLabel, d => 3 <= d },
            {Part.MonthsBorder, d => 0.1 < d },
            {Part.WeeksShown, d => 3 < d && d <= 25 },
            {Part.WeeksLabel, d => 4.5 < d && d <= 20 },
            {Part.DaysBorder, d => 3 < d},
            {Part.DayNumberLabel, d => 20 < d},
            {Part.DayWeekLabel, d => 28 < d},
        };

        public class MonthGroup : INotifyPropertyChanged
        {
            public int Month { get; set; }
            public int Year { get; set; }
            public int Count { get; set; }

            public string MonthNameYear
            {
                get { return "{0}, {1}".FormatWith(monthNames[Month-1], Year); }
            }

            public string MonthName
            {
                get { return monthNames[Month-1]; }
            }

            public event PropertyChangedEventHandler PropertyChanged;
        }

        public class YearGroup : INotifyPropertyChanged
        {
            public int Year { get; set; }
            public int  Count{ get; set; }

            public string YearName { get { return Year.ToString(); } }

            public event PropertyChangedEventHandler PropertyChanged;
        }

        public class Day : INotifyPropertyChanged
        {
            public DateTime Value;
            public string DayFullName
            {
                get { return "{0} {1}".FormatWith(dayNames[Value.DayOfWeek], Value.Day); }
            }
            public string DayFullToolTip
            {
                get { return Value.ToLongDateString(); }
            }

            public string DayNumber
            {
                get { return Value.Day.ToString(); }
            }

            public Style Style
            {
                get
                {
                    if (Value.DayOfWeek == DayOfWeek.Saturday || Value.DayOfWeek == DayOfWeek.Sunday)
                        return darkLabel;

                    if (Value == DateTime.Today)
                        return todayLabel;

                    return normalLabel;
                }
            }

            public event PropertyChangedEventHandler PropertyChanged;
        }

        public class WeekGroup : INotifyPropertyChanged
        {
            public int Min{get;set;}
            public int Max{get;set;}
            public int Count{ get; set; }
            public string WeekName
            {
                get { return "{0}-{1}".FormatWith(Min, Max); }
            }

            public event PropertyChangedEventHandler PropertyChanged;
        }

        private void ReconstruirDatos()
        {
            DateTime[] days = Min.For(d => d <= Max, d => d.AddDays(1)).ToArray();

            icYears.ItemsSource = days.GroupCount(a => a.Year).Select(p => new YearGroup { Year = p.Key, Count = p.Value }).ToObservableCollection();

            var months = days.GroupCount(a => new { a.Month, a.Year }).Select(p => new MonthGroup { Month = p.Key.Month, Year = p.Key.Year, Count = p.Value }).ToObservableCollection();
            icMonthsDown.ItemsSource = months;
            icMonthsUp.ItemsSource = months;

            icDays.ItemsSource = days.Select(a => new Day { Value = a }).ToObservableCollection();

            icWeeks.ItemsSource = days.GroupCount(d => GetWeek(d)).Select(p => new WeekGroup { Min = p.Key.Item1.Day, Max = p.Key.Item2.Day, Count = p.Value }).ToObservableCollection();
        }

        public Binding BindingToDayWidth<T>(Func<double,T> converter )
        {
            return new Binding
            {
                Source = this,
                Path = new PropertyPath(CalendarStripHeader.DayWidthProperty),
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
                Path = new PropertyPath(CalendarStripHeader.DayWidthProperty)
            });

            mb.Bindings.Add(other);
            return mb; 
        }

        private void ConstruirTemplates()
        {
            icYears.SetBinding(ItemsControl.VisibilityProperty, BindingToDayWidth(d => isVisible[Part.YearsMode](d) ? Visibility.Visible : Visibility.Collapsed));
            icMonthsDown.SetBinding(ItemsControl.VisibilityProperty, BindingToDayWidth(d => isVisible[Part.YearsMode](d) ? Visibility.Visible : Visibility.Collapsed));

            icMonthsUp.SetBinding(ItemsControl.VisibilityProperty, BindingToDayWidth(d => isVisible[Part.YearsMode](d) ? Visibility.Collapsed : Visibility.Visible));
            icDays.SetBinding(ItemsControl.VisibilityProperty, BindingToDayWidth(d => isVisible[Part.YearsMode](d) ? Visibility.Collapsed : Visibility.Visible));

            icWeeks.SetBinding(ItemsControl.VisibilityProperty, BindingToDayWidth(d => isVisible[Part.WeeksShown](d) ? Visibility.Visible : Visibility.Collapsed));


            icYears.ItemTemplate = new DataTemplate(typeof(YearGroup))
            {
                VisualTree = new FrameworkElementFactory(typeof(Label))
                .Do(f =>
                {
                    f.SetValue(Label.StyleProperty, darkLabel);

                    f.SetBinding(Label.ContentProperty, MultiBindingToDayWidth(new Binding("YearName"), (double d, string str) => isVisible[Part.YearsLabel](d) ? str : string.Empty));

                    f.SetBinding(Label.WidthProperty, MultiBindingToDayWidth(new Binding("Count"), (double d, int count) => d * count));

                    f.SetBinding(Label.BorderThicknessProperty, BindingToDayWidth(d => isVisible[Part.YearsBorder](d) ? new Thickness(1) : new Thickness(0)));
                })
            };


            icMonthsDown.ItemTemplate = new DataTemplate(typeof(MonthGroup))
            {
                VisualTree = new FrameworkElementFactory(typeof(Label))
                .Do(f =>
                {
                    f.SetValue(Label.StyleProperty, darkLabel);

                    f.SetBinding(Label.ContentProperty, MultiBindingToDayWidth(new Binding("MonthName"), (double d, string str) => isVisible[Part.MonthsLabel](d) ? str : string.Empty));

                    f.SetBinding(Label.WidthProperty, MultiBindingToDayWidth(new Binding("Count"), (double d, int count) => d * count));
                    f.SetBinding(Label.BorderThicknessProperty, BindingToDayWidth(d => isVisible[Part.MonthsBorder](d) ? new Thickness(1) : new Thickness(0)));
                })
            };


            icMonthsUp.ItemTemplate = new DataTemplate(typeof(MonthGroup))
            {
                VisualTree = new FrameworkElementFactory(typeof(Label))
                .Do(f =>
                {
                    f.SetValue(Label.StyleProperty, darkLabel);

                    f.SetBinding(Label.ContentProperty, MultiBindingToDayWidth(new Binding("MonthNameYear"), (double d, string str) => isVisible[Part.MonthsYearsLabel](d) ? str : string.Empty));

                    f.SetBinding(Label.WidthProperty, MultiBindingToDayWidth(new Binding("Count"), (double d, int count) => d * count));
                    f.SetBinding(Label.BorderThicknessProperty, BindingToDayWidth(d => isVisible[Part.MonthsBorder](d) ? new Thickness(1) : new Thickness(0)));
                })
            };



            icDays.ItemTemplate = new DataTemplate(typeof(Day))
            {
                VisualTree = new FrameworkElementFactory(typeof(Label))
                .Do(f =>
                {
                    f.SetBinding(Label.StyleProperty, new Binding("Style"));

                    f.SetBinding(Label.ContentProperty, MultiBindingToDayWidth(new Binding(), (double d, Day day) => isVisible[Part.DayNumberLabel](d) ? (isVisible[Part.DayWeekLabel](d) ?  day.DayFullName: day.DayNumber):null));

                    f.SetValue(Label.WidthProperty, new Binding { Source = this, Path = new PropertyPath(CalendarStripHeader.DayWidthProperty) });
                    f.SetBinding(Label.ToolTipProperty, new Binding("DayFullToolTip"));

                    f.SetBinding(Label.BorderThicknessProperty, BindingToDayWidth(d => isVisible[Part.DaysBorder](d) ? new Thickness(1) : new Thickness(0)));
                })
            };


            icWeeks.ItemTemplate = new DataTemplate(typeof(WeekGroup))
            {
                VisualTree = new FrameworkElementFactory(typeof(Label))
                .Do(f =>
                {
                    f.SetValue(Label.StyleProperty, weekLabel);

                    f.SetBinding(Label.ContentProperty, MultiBindingToDayWidth(new Binding("WeekName"), (double d, string str) => isVisible[Part.WeeksLabel](d) ? str : string.Empty));

                    f.SetBinding(Label.WidthProperty, MultiBindingToDayWidth(new Binding("Count"), (double d, int count) => d * count));
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

#pragma warning restore 0067