using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Signum.Windows.DateUtils
{
    public class MonthCalendarItem : ListBoxItem
    {
        /// <summary>
        /// Static Constructor
        /// </summary>
        static MonthCalendarItem()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(MonthCalendarItem), new FrameworkPropertyMetadata(typeof(MonthCalendarItem)));
        }

        /// <summary>
        /// This is the method that responds to the MouseButtonEvent event.
        /// </summary>
        /// <param name="e">Event arguments</param>
        protected override void OnMouseRightButtonDown(MouseButtonEventArgs e)
        {
            //In ListBox, right click will select the item, override this method to remove this feature
        }
    }
}