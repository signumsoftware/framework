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
using System.Linq;

namespace Signum.Windows.Calendars
{
	public partial class CalendarStripTreeView
	{

        public static readonly DependencyProperty ItemTemplateLeftProperty =
            DependencyProperty.Register("ItemTemplateLeft", typeof(HierarchicalDataTemplate), typeof(CalendarStripTreeView), new UIPropertyMetadata(null));
        public HierarchicalDataTemplate ItemTemplateLeft
        {
            get { return (HierarchicalDataTemplate)GetValue(ItemTemplateLeftProperty); }
            set { SetValue(ItemTemplateLeftProperty, value); }
        }


        public static readonly DependencyProperty ItemTemplateRightProperty =
            DependencyProperty.Register("ItemTemplateRight", typeof(HierarchicalDataTemplate), typeof(CalendarStripTreeView), new UIPropertyMetadata(null));
        public HierarchicalDataTemplate ItemTemplateRight
        {
            get { return (HierarchicalDataTemplate)GetValue(ItemTemplateRightProperty); }
            set { SetValue(ItemTemplateRightProperty, value); }
        }

        public CalendarStripTreeView()
		{
			this.InitializeComponent();
		}

        ScrollViewer _swLeft;
        ScrollViewer swLeft
        {
            get
            {
                return _swLeft ?? (_swLeft = treeLeft.Child<ScrollViewer>(WhereFlags.VisualTree));
            }
        }

        ScrollViewer _swRight;
        ScrollViewer swRight
        {
            get
            {
                return _swRight ?? (_swRight = treeRight.Child<ScrollViewer>(WhereFlags.VisualTree));
            }
        }

        public override void RecalculateDayWidth()
        {
            if (AutoDayWidth)
                DayWidth = swRight.ActualWidth / (Max - Min).Days;
        }

        private void btRecalcular_Click(object sender, RoutedEventArgs e)
        {
            RecalculateMinMax();
        }


        private void treeRight_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            swTop.ScrollToHorizontalOffset(e.HorizontalOffset);
            swLeft.ScrollToVerticalOffset(e.VerticalOffset);
        }

        private void treeLeft_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            swRight.ScrollToVerticalOffset(e.VerticalOffset);
        }
	}
}