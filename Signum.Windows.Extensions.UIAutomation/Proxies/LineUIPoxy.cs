using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Automation;
using Signum.Entities;
using Signum.Windows.UIAutomation;
using Signum.Entities.DynamicQuery;

namespace Signum.Windows.UIAutomation.Proxies
{
    public class CountSeachControlPoxy
    {
        public AutomationElement Element { get; private set; }

        public CountSeachControlPoxy(AutomationElement element)
        {
            Element = element; 
        }

        public AutomationElement HiperLink
        {
            get { return Element.Descendant(e=>e.Current.ClassName == "Hyperlink"); }
        }

        public SearchWindowProxy NavigateSearchWindow()
        { 
           return new SearchWindowProxy(Element.CaptureWindow(
               ()=>HiperLink.ButtonInvoke(),
               () => "Waiting to capture window after Hyperlink",
               OperationTimeouts.ExecuteTimeout));
        }
    }

    public static class CountSearchControlExtensions
    {
        public static CountSeachControlPoxy GetCountControlSearchControl(this AutomationElement element)
        {
            var csc = element.Descendant(a => a.Current.ClassName == "CountSearchControl");

            return new CountSeachControlPoxy(csc);
        }

        //public static CountSeachControlPoxy GetCountControlSearchControl(this AutomationElement element, object queryName)
        //{
        //    var csc = element.Descendant(a => a.Current.ClassName == "CountSearchControl" && a.Current.Name == QueryUtils.GetQueryUniqueKey(queryName));

        //    return new CountSeachControlPoxy(csc);
        //}
    }
}
