using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows.Controls;
using Signum.Entities.Basics;

namespace Signum.Windows.DiffLog
{
    public static class DiffLogClient
    {
        public static void Start()
        {
            if (Navigator.Manager.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                Navigator.EntitySettings<OperationLogEntity>().OverrideView += (e, c)=>
                {
                    c.Child<StackPanel>().Children.Add(new DiffLogTabs());
                    return c;
                }; 
            }
        }
    }
}
