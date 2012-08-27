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
        public static AutomationElement MenuItemOpenWindow(this WindowProxy window, params string[] menuNames)
        {
            var menuItem = MenuItemFind(window.Element, menuNames);

            AutomationElement newWindow = window.Element.CaptureWindow(
                () => menuItem.ButtonInvoke(),
                () => "New windows opened after menu " + menuNames.ToString(" -> "));

            return newWindow;
        }

        public static AutomationElement MenuItemFind(AutomationElement element, params string[] menuNames)
        {
            if (menuNames == null || menuNames.Length == 0)
                throw new ArgumentNullException("menuNames");

            var menuBar = element.Child(c => c.Current.ControlType == ControlType.Menu);

            var menuItem = menuBar.ChildByCondition(new PropertyCondition(AutomationElement.NameProperty, menuNames[0]));

            for (int i = 1; i < menuNames.Length; i++)
            {
                menuItem.Pattern<ExpandCollapsePattern>().Expand();
                menuItem = menuItem.WaitChildByCondition(new PropertyCondition(AutomationElement.NameProperty, menuNames[i]));
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
