using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Automation;
using Signum.Utilities;

namespace Signum.Windows.UIAutomation
{
    public static class MenuItemExtensions
    {
        public static AutomationElement MenuItemOpenWindow(AutomationElement menu, params string[] menuNames)
        {
            var menuItem = MenuItemFind(menu, menuNames);

            AutomationElement newWindow = menu.CaptureWindow(
                () => menuItem.ButtonInvoke(),
                () => "New windows opened after menu  " + menuNames.ToString(" -> "));

            return newWindow;
        }

        public static AutomationElement MenuItemFind(AutomationElement menu, params string[] menuNames)
        {
            if (menuNames == null || menuNames.Length == 0)
                throw new ArgumentNullException("menuNames");

            var menuItem = menu.Child(mi => mi.Current.Name == menuNames[0]);
            var window = menu.Normalize(mi => mi.Current.ControlType == ControlType.Window); 

            for (int i = 1; i < menuNames.Length; i++)
            {
                window = window.CaptureChildWindow(() =>
                    menuItem.Pattern<ExpandCollapsePattern>().Expand(),
                    actionDescription: () => "Popup window after expanding MenuItem {0}".Formato(menuNames[i - 1]));
                
                menuItem = window.Child(mi => mi.Current.Name == menuNames[i]);
            }
            return menuItem;
        }

        //static MenuItemCached[] cachedMenus;

        //public static void MenuItemExploreInvoke(this WindowProxy window, Condition condition)
        //{
        //    if (cachedMenus == null)
        //    {
        //        var menuBar = window.Owner.Child(c => c.Current.ControlType == ControlType.Menu);

        //        var menuItem = menuBar.ChildrenAll().Select(c=>new MenuItemCached(c));
        //    }
        //}
    }

    //public class MenuItemCached
    //{
    //    public string Name; 
    //    public bool Expandable; 
    //    public MenuItemCached[] childs;
    //    private AutomationElement c;

    //    public MenuItemCached(AutomationElement c)
    //    {
    //        Name = c.Current.Name;
    //    } 
    //}
}
