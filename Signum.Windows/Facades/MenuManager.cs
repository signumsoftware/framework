using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Windows;
using Signum.Utilities;
using Signum.Entities.Reflection;

namespace Signum.Windows
{
    public static class MenuManager
    {
        public static event Action<MenuItem> Tasks;

        static MenuManager()
        {
            Tasks += TaskCollapseSubMenuParent;
            Tasks += TaskSetHeader;
            Tasks += TaskKeyboardShortcut;
        }

        public static void TaskKeyboardShortcut(MenuItem menuItem)
        {
            ShortcutHelper.SetMenuItemShortcuts(menuItem);
        }
      

        public static void TaskCollapseSubMenuParent(MenuItem menuItem)
        {
            if (menuItem.Items.Count > 0 && menuItem.Items.Cast<MenuItem>().All(mi => mi.Visibility == Visibility.Collapsed))
                menuItem.Visibility = Visibility.Collapsed;
        }

        public static void TaskSetHeader(MenuItem menuItem)
        {
            if (menuItem.NotSet(MenuItem.HeaderProperty))
            {
                object o = menuItem.Tag;

                if (o == null)
                    return;

                if (o is FindOptions)
                    o = ((FindOptions)o).QueryName;
                else if (o is AdminOptions)
                    o = ((AdminOptions)o).Type;

                if (o is Enum)
                    menuItem.Header = ((Enum)o).NiceToString();
                else if (o is Type)
                    menuItem.Header = ((Type)o).NiceName();
            }
        }

        public static void Process(Menu menu)
        {
            menu.AddHandler(MenuItem.ClickEvent, new RoutedEventHandler(GenericMenuItem_Click));

            if (Tasks == null)
                return;

            foreach (MenuItem menuItem in menu.Items)
            {
                Process(menuItem);
            }
        }

        static void GenericMenuItem_Click(object sender, RoutedEventArgs e)
        {
            object o = ((MenuItem)e.OriginalSource).Tag;

            if (o == null)
                return;

            if (o is FindOptions)
                Navigator.Find(((FindOptions)o).Do(fo => { fo.Buttons = SearchButtons.Close; }));
            else if (o is AdminOptions)
                Navigator.Admin(((AdminOptions)o));
        }

        static void Process(MenuItem menuItem)
        {
            foreach (MenuItem item in menuItem.Items)
            {
                Process(item);
            }

            foreach (Action<MenuItem> action in Tasks.GetInvocationList())
            {
                action(menuItem);
            }
        }
    }  
}
