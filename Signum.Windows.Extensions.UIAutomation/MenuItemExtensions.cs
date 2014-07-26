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
            var window = menu.Parent(mi => mi.Current.ControlType == ControlType.Window);

            for (int i = 1; i < menuNames.Length; i++)
            {
                menuItem.Pattern<ExpandCollapsePattern>().Expand();

                AutomationElement result = null;
                window.Wait(() =>
                {
                    result = menuItem.TryChild(mi => mi.Current.Name == menuNames[i]);
                    if (result != null)
                        return true;

                    if (window != null)
                    {
                        result = window.TryChild(a => a.Current.ControlType == ControlType.Window);
                        if (result != null)
                            return true;
                    }

                    return false;
                }, actionDescription: () => "Popup window or MenuItem after expanding MenuItem {0}".Formato(menuNames[i - 1]));

                if (result.Current.ControlType == ControlType.Window)
                {
                    window = result;
                    menuItem = window.TryChild(mi => mi.Current.Name == menuNames[i]);
                }
                else
                {
                    window = null;
                    menuItem = result;
                }
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
