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

            return new SearchWindowProxy(SelectCapture(omniboxName, "Q:" + QueryUtils.GetQueryUniqueKey(queryName))); 
        }

        public NormalWindowProxy<T> SelectEntity<T>(Lite<T> lite) where T : IdentifiableEntity
        {
            var omniboxName = lite.EntityType.NicePluralName().ToOmniboxPascal() + " " + lite.Id;

            return new NormalWindowProxy<T>(SelectCapture(omniboxName, "E:" + lite.Key())); 
        }

        public SearchWindowProxy SelectUserQuery(Lite<UserQueryDN> userQuery)
        {
            var omniboxName = "'" + userQuery.ToString() + "'";

            return new SearchWindowProxy(SelectCapture(omniboxName, "UQ:" + userQuery.Key())); 
        }

        public AutomationElement SelectCapture(string autoCompleteText, string name, int? timeOut = null)
        {
            return Element.CaptureWindow(
                () =>
                {
                    Element.Value(autoCompleteText);

                    timeOut = timeOut ?? OmniboxTimeout;

                    var lb = Element.WaitChildById("lstBox", timeOut);

                    var item = lb.TryDescendant(e => e.Current.Name == name);

                    if (item == null)
                        throw new ElementNotFoundException("{0} not found after writing {1} on the Omnibox".Formato(name, autoCompleteText));

                    var listItem = item.Parent(a => a.Current.ControlType == ControlType.ListItem);

                    listItem.Pattern<SelectionItemPattern>().Select();
                },
                () => "window after selecting {0} on the omnibox".Formato(name), timeOut);
        }
    }
}
