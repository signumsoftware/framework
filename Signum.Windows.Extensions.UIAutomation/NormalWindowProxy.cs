using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Automation;
using Signum.Utilities;

namespace Signum.Windows.UIAutomation
{
    public class NormalWindowProxy : WindowProxy
    {
        public NormalWindowProxy(AutomationElement owner)
            : base(owner)
        {
            
        }
    }
}
