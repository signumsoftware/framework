using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Automation;
using Signum.Entities;
using Signum.Entities.UserQueries;
using Signum.Windows.UIAutomation;

namespace Signum.Windows.Extensions.Sample.Test
{
    public class MainWindowProxy: WindowProxy
    {
        public OmniBoxProxy OmniBox { get; private set; }
        public AutomationElement MenuBar { get; private set; }

        public MainWindowProxy(AutomationElement element)
            : base(element)
        {
            OmniBox = new OmniBoxProxy(element.Descendant(e => e.Current.AutomationId == "autoCompleteTb"));
            MenuBar = element.Child(c => c.Current.ControlType == ControlType.Menu);
        }

        public AutomationElement MenuItemOpenWindow(params string[] menuNames)
        {
            return MenuItemExtensions.MenuItemOpenWindow(MenuBar, menuNames);
        }

        public SearchWindowProxy SelectQuery(object queryName)
        {
            return OmniBox.SelectQuery(queryName);
        }

        public NormalWindowProxy<T> SelectEntity<T>(Lite<T> lite) where T : IdentifiableEntity
        {
            return OmniBox.SelectEntity<T>(lite);
        }

        public SearchWindowProxy SelectUserQuery(Lite<UserQueryDN> userQuery)
        {
            return OmniBox.SelectUserQuery(userQuery);
        }

        public AutomationElement SelectCapture(string autoCompleteText, string itemsStatus, int? timeOut = null)
        {
            return OmniBox.SelectCapture(autoCompleteText, itemsStatus, timeOut);
        }

    }
}
