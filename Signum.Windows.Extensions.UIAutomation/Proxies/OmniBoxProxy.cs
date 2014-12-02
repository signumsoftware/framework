using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Automation;
using Signum.Entities;
using Signum.Entities.UserQueries;
using Signum.Utilities;
using Signum.Engine.Basics;
using Signum.Entities.Reflection;
using Signum.Entities.DynamicQuery;
using Signum.Engine;
using Signum.Entities.Omnibox;

namespace Signum.Windows.UIAutomation
{
    public class OmniBoxProxy
    {
        public int OmniboxTimeout = 10000;

        public AutomationElement Element { get; private set; }

        public OmniBoxProxy(AutomationElement element)
        {
            this.Element = element; 
        }

        public SearchWindowProxy SelectQuery(object queryName)
        {
            var omniboxName = QueryUtils.GetNiceName(queryName).ToOmniboxPascal();

            return new SearchWindowProxy(SelectCapture(omniboxName, "Q:" + QueryUtils.GetQueryUniqueKey(queryName), className: "SearchWindow")); 
        }

        public NormalWindowProxy<T> SelectEntity<T>(Lite<T> lite) where T : Entity
        {
            var omniboxName = lite.EntityType.NicePluralName().ToOmniboxPascal() + " " + lite.Id;

            return new NormalWindowProxy<T>(SelectCapture(omniboxName, "E:" + lite.Key(), className: "NormalWindow")); 
        }

        public SearchWindowProxy SelectUserQuery(Lite<UserQueryEntity> userQuery)
        {
            var omniboxName = "'" + userQuery.ToString() + "'";

            return new SearchWindowProxy(SelectCapture(omniboxName, "UQ:" + userQuery.Key(), className: "SearchWindow")); 
        }

        public AutomationElement SelectCapture(string autoCompleteText, string name, int? timeOut = null, string className = null)
        {
            return Element.CaptureWindow(
                action : () =>
                {
                    Element.Value(autoCompleteText);

                    timeOut = timeOut ?? OmniboxTimeout;

                    var lb = Element.WaitChildById("lstBox", timeOut);

                    var item = lb.TryDescendant(e => e.Current.Name == name);

                    if (item == null)
                        throw new ElementNotFoundException("{0} not found after writing {1} on the Omnibox".FormatWith(name, autoCompleteText));

                    var listItem = item.Parent(a => a.Current.ControlType == ControlType.ListItem);

                    listItem.Pattern<SelectionItemPattern>().Select();
                },
                windowsCondition: ae => className == null || ae.Current.ClassName == className,
                actionDescription: () => "window after selecting {0} on the omnibox".FormatWith(name),
                timeOut: timeOut);
        }
    }
}
