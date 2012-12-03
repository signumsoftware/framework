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

namespace Signum.Windows.UIAutomation
{
    public class OmniBoxProxy
    {
        public int OmniboxTimeout = 4000;

        public AutomationElement Element { get; private set; }

        public OmniBoxProxy(AutomationElement element)
        {
            this.Element = element; 
        }

        public SearchWindowProxy SelectQuery(object queryName)
        {
            var cleanName = queryName is Type ? Reflector.CleanTypeName((Type)queryName) : queryName.ToString();

            return new SearchWindowProxy(SelectCapture(cleanName, "Q:" + QueryUtils.GetQueryUniqueKey(queryName))); 
        }

        public NormalWindowProxy<T> SelectEntity<T>(Lite<T> lite) where T : IdentifiableEntity
        {
            var cleanName = TypeLogic.GetCleanName(lite.RuntimeType) + " " + lite.Id;

            return new NormalWindowProxy<T>(SelectCapture(cleanName, "E:" + lite.Key())); 
        }

        public SearchWindowProxy SelectUserQuery(Lite<UserQueryDN> userQuery)
        {
            var cleanName = "'" + userQuery.ToString() + "'";

            return new SearchWindowProxy(SelectCapture(cleanName, "UQ:" + userQuery.Key())); 
        }

        public AutomationElement SelectCapture(string autoCompleteText, string itemsStatus, int? timeOut = null)
        {
            return Element.CaptureWindow(
                () =>
                {
                    Element.Value(autoCompleteText);

                    timeOut = timeOut ?? OmniboxTimeout;

                    var lb = Element.WaitChildById("lstBox", timeOut);

                    var item = lb.TryDescendant(e => e.Current.ItemStatus == itemsStatus);

                    if (item == null)
                        throw new KeyNotFoundException("{0} not found after writing {1} on the Omnibox".Formato(itemsStatus, autoCompleteText));

                    var listItem = item.Normalize(a => a.Current.ControlType == ControlType.ListItem);

                    listItem.Pattern<SelectionItemPattern>().Select();
                },
                () => "window after selecting {0} on the omnibox".Formato(itemsStatus), timeOut);
        }
    }
}
