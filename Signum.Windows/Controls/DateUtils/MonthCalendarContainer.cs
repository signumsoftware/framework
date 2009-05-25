using System.Windows;
using System.Windows.Controls;

namespace Signum.Windows.DateUtils
{
    public class MonthCalendarContainer : ListBox
    {
        /// <summary>
        /// Return true if the item is (or is eligible to be) its own ItemContainer
        /// </summary>
        protected override bool IsItemItsOwnContainerOverride(object item)
        {
            return (item is MonthCalendarItem);
        }

        /// <summary> Create or identify the element used to display the given item. </summary>
        protected override DependencyObject GetContainerForItemOverride()
        {
            return new MonthCalendarItem();
        }
    }
}