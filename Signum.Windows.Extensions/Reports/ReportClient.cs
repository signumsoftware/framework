using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Windows.Reports;
using Signum.Entities.Reports;

namespace Signum.Windows.Reports
{
    public class ReportClient
    {
        public static void Start(NavigationManager manager, bool toExcel, bool excelReport, bool compositeReport)
        {
            if (toExcel)
            {
                SearchControl.GetCustomMenuItems += qn => new ExcelReportMenuItem();
                manager.Settings.Add(typeof(ExcelReportDN), new EntitySettings(false) { View = () => new ExcelReport() });
            }
            if (excelReport)
            {
                SearchControl.GetCustomMenuItems += qn => qn == typeof(ExcelReportDN) ? null : new ExcelReportPivotTableMenuItem();
                
                if (compositeReport)
                {
                    manager.Settings.Add(typeof(CompositeReportDN), new EntitySettings(false) { View = () => new CompositeReport() });
                }
            }
            else
                if (compositeReport)
                    throw new InvalidOperationException(); 
        }
    }
}
