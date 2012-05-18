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
            menuItem.Items.Cast<Control>().Where(a => a.Visibility == Visibility.Visible).BiSelect((first, second) =>
            {
                if (second is Separator && first is Separator)
                    return (Separator)second;
                if (second is Separator && first == null)
                    return (Separator)second;
                if (first is Separator && second == null)
                    return (Separator)first;
                return null;
            }, BiSelectOptions.InitialAndFinal).NotNull().ToList().ForEach(a => a.Visibility = Visibility.Collapsed);
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


        public static void MenuItemFactory(string fileName)
        {
            Dictionary<string, string> dic = new Dictionary<string, string>();

            List<object> queryNames =  Server.Return((IDynamicQueryServer s)=>s.GetQueryNames()); 

            string menuItems = queryNames.ToString(o => GetMenuItem(o, dic), "\r\n");
            string nameSpaces = dic.ToString(kvp => "xmlns:{0}=\"clr-namespace:{1};assembly={2}\"".Formato(kvp.Value, kvp.Key.Split(';')[0], kvp.Key.Split(';')[1]), "\r\n");
            
            File.WriteAllText(fileName, nameSpaces);
            File.AppendAllText(fileName, menuItems);

            Process.Start(fileName);
        }

        static string GetMenuItem(object queryName, Dictionary<string,string> dic)
        {
            string menuItem = "<MenuItem Tag=\"{{m:FindOptions QueryName={0}}}\"/>";
            Type type = queryName as Type;
            if (type != null)
                return menuItem.Formato("{{x:Type {0}:{1}}}".Formato(GetAlias(type, dic), type.Name));

            Enum enumValue = queryName as Enum;
            if (enumValue != null)
                return menuItem.Formato("{{x:Static {0}:{1}.{2}}}".Formato(GetAlias(enumValue.GetType(), dic), enumValue.GetType().Name, enumValue.ToString()));

            return menuItem.Formato(queryName.ToString());
        }

        static string GetAlias(Type exampleType, Dictionary<string,string> dic)
        {
            return dic.GetOrCreate(exampleType.Namespace + ";" + exampleType.Assembly.GetName().Name, () =>
            {
                string result = exampleType.Namespace.Split('.').ToString(str => str.Substring(0, 1), "").ToLower();

                if (!dic.Values.Contains(result))
                    return result;

                for (int i = 0; ; i++)
                    if (!dic.Values.Contains(result + i))
                        return result + i;
            }); 
        }

    }  
}
