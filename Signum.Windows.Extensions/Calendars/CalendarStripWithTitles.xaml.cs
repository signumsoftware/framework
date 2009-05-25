using System;
using System.IO;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Navigation;
using System.Collections;

namespace Signum.Windows.Calendars
{
	public partial class CalendarStripWithTitles: CalendarStrip
	{
        public static readonly DependencyProperty ItemContainerStyleProperty =
                  DependencyProperty.Register("ItemContainerStyle", typeof(Style), typeof(CalendarStripWithTitles), new UIPropertyMetadata(null));
        public Style ItemContainerStyle
        {
            get { return (Style)GetValue(ItemContainerStyleProperty); }
            set { SetValue(ItemContainerStyleProperty, value); }
        }

        public static readonly DependencyProperty ItemTemplateProperty =
          DependencyProperty.Register("ItemTemplate", typeof(DataTemplate), typeof(CalendarStripWithTitles), new UIPropertyMetadata(null));
        public DataTemplate ItemTemplate
        {
            get { return (DataTemplate)GetValue(ItemTemplateProperty); }
            set { SetValue(ItemTemplateProperty, value); }
        }


        public static readonly DependencyProperty TitleItemContainerStyleProperty = 
          DependencyProperty.Register("TitleItemContainerStyle", typeof(Style), typeof(CalendarStripWithTitles), new UIPropertyMetadata(null));
        public Style TitleItemContainerStyle
        {
            get { return (Style)GetValue(TitleItemContainerStyleProperty); }
            set { SetValue(TitleItemContainerStyleProperty, value); }
        }

        public static readonly DependencyProperty TitleItemTemplateProperty =
          DependencyProperty.Register("TitleItemTemplate", typeof(DataTemplate), typeof(CalendarStripWithTitles), new UIPropertyMetadata(null));
        public DataTemplate TitleItemTemplate
        {
            get { return (DataTemplate)GetValue(TitleItemTemplateProperty); }
            set { SetValue(TitleItemTemplateProperty, value); }
        }

        public CalendarStripWithTitles()
		{
			this.InitializeComponent();

            listBox.AddHandler(DatePanel.ElementsMinMaxChangedEvent, new RoutedEventHandler((o, a) => TryRecalculateMinMax()));
		}

        public override void RecalculateDayWidth()
        {
            if (AutoDayWidth)
                DayWidth = swPanel.ActualWidth/ (Max - Min).Days;
        }

        private void btRecalcular_Click(object sender, RoutedEventArgs e)
        {
            RecalculateMinMax(); 
        }

        private void swPanel_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            swTop.ScrollToHorizontalOffset(e.HorizontalOffset);
            swLeft.ScrollToVerticalOffset(e.VerticalOffset);
        }

        private void swLeft_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            swPanel.ScrollToVerticalOffset(e.VerticalOffset); 
        }
	}
}