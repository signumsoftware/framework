using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Signum.Windows;
using Signum.Entities;
using Signum.Entities.Scheduler;

namespace Signum.Windows.Scheduler
{
    /// <summary>
    /// Interaction logic for Calendar.xaml
    /// </summary>
    public partial class HolidayCalendar : UserControl
    {
        public HolidayCalendar()
        {
            InitializeComponent();
        }

        private object EntityList_Creating()
        {
            HolidayCalendarEntity cal = ((HolidayCalendarEntity)DataContext);
            if (cal == null || cal.Holidays == null || cal.Holidays.Count == 0)
                return new HolidayEmbedded { Date = DateTime.Today };
            else
                return new HolidayEmbedded { Date = cal.Holidays.Max(a => a.Date) }; 
        }
    }
}
