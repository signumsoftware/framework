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
using Signum.Utilities;
using System.Linq;

namespace Signum.Windows.Calendars
{
	public partial class CalendarStripList : CalendarStrip
	{
        public static readonly DependencyProperty ItemContainerStyleProperty =
                  DependencyProperty.Register("ItemContainerStyle", typeof(Style), typeof(CalendarStrip), new UIPropertyMetadata(null));
        public Style ItemContainerStyle
        {
            get { return (Style)GetValue(ItemContainerStyleProperty); }
            set { SetValue(ItemContainerStyleProperty, value); }
        }

        public static readonly DependencyProperty ItemTemplateProperty =
          DependencyProperty.Register("ItemTemplate", typeof(DataTemplate), typeof(CalendarStrip), new UIPropertyMetadata(null));
        public DataTemplate ItemTemplate
        {
            get { return (DataTemplate)GetValue(ItemTemplateProperty); }
            set { SetValue(ItemTemplateProperty, value); }
        }

        public CalendarStripList()
		{
			this.InitializeComponent();

            listBox.AddHandler(DatePanel.ElementsMinMaxChangedEvent, new RoutedEventHandler((o, a) => TryRecalculateMinMax()));
		}

        private void swPanel_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            swTop.ScrollToHorizontalOffset(e.HorizontalOffset); 
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
	}
}