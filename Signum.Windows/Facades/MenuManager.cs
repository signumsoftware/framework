using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Windows;
using Signum.Utilities;
using Signum.Entities.Reflection;
using System.Windows.Media;
using Signum.Services;
using System.Diagnostics;
using System.IO;
using Signum.Entities.DynamicQuery;

namespace Signum.Windows
{
    public static class MenuManager
    {
        public static event Action<MenuItem> Tasks;

        static MenuManager()
        {
            Tasks += TaskCollapseSubMenuParent;
            Tasks += TaskSetHeader;
            Tasks += TaskSetIcon;
            Tasks += TaskKeyboardShortcut;
            Tasks += TaskCleanSeparators;
        }

       
     
        public static void TaskKeyboardShortcut(MenuItem menuItem)
        { 
            ShortcutHelper.SetMenuItemShortcuts(menuItem);
        }

        public static void TaskCollapseSubMenuParent(MenuItem menuItem)
        {
            if (menuItem.Items.Count > 0 && menuItem.Items.OfType<MenuItem>().All(mi => mi.Visibility == Visibility.Collapsed))
                menuItem.Visibility = Visibility.Collapsed;
        }

        static void TaskCleanSeparators(MenuItem menuItem)
        {
            var visibles = menuItem.Items.Cast<Control>().Where(a => a.Visibility == Visibility.Visible).ToList();

            int i, j;
            for (i = 0; i < visibles.Count && visibles[i] is Separator; i++)
                visibles[i].Visibility = Visibility.Collapsed;

            for (j = visibles.Count - 1; j >= i && visibles[j] is Separator; j--)
                visibles[j].Visibility = Visibility.Collapsed;

            for (int z = i; z <= j; z++)
            {
                if (visibles[z] is Separator && z > 0 && visibles[z - 1] is Separator)
                    visibles[z].Visibility = Visibility.Collapsed;
            }
        }

        public static void TaskSetHeader(MenuItem menuItem)
        {
            if (menuItem.NotSet(MenuItem.HeaderProperty))
            {
                object o = menuItem.Tag;

                if (o == null)
                    return;

                if (o is FindOptionsBase)
                    menuItem.Header = QueryUtils.GetNiceName(((FindOptionsBase)o).QueryName);
                else if (o is Type)
                    menuItem.Header = ((Type)o).NicePluralName();
                else if (o is Enum)
                    menuItem.Header = ((Enum)o).NiceToString();
                else
                    menuItem.Header = o.ToString();
            }
        }

        static void TaskSetIcon(MenuItem menuItem)
        {
            if (menuItem.NotSet(MenuItem.IconProperty))
            {
                object o = menuItem.Tag;

                if (o == null)
                    return;

                ImageSource source = 
                    o is FindOptionsBase ? Navigator.Manager.GetFindIcon(((FindOptionsBase)o).QueryName, false) : null;

                menuItem.Icon = new Image { Source = source, Stretch = Stretch.None }; 
            }
        }

        public static void ProcessMenu(Menu menu)
        {
            menu.AddHandler(MenuItem.ClickEvent, new RoutedEventHandler(GenericMenuItem_Click));

            if (Tasks == null)
                return;

            foreach (MenuItem menuItem in menu.Items)
            {
                ProcessMenuItem(menuItem);
            }
        }

        static void GenericMenuItem_Click(object sender, RoutedEventArgs e)
        {
            object o = ((MenuItem)e.OriginalSource).Tag;

            if (o == null)
                return;

            if (o is ExploreOptions)
                Navigator.Explore((ExploreOptions)o);
        }

        static void ProcessMenuItem(MenuItem menuItem)
        {
            foreach (MenuItem item in menuItem.Items.OfType<MenuItem>())
            {
                ProcessMenuItem(item);
            }

            foreach (Action<MenuItem> action in Tasks.GetInvocationList())
            {
                action(menuItem);
            }
        }
    }  
}
