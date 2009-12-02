using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Windows.Reports;
using Signum.Entities.Reports;
using Signum.Services;
using System.Reflection;

namespace Signum.Windows.Reports
{
    public class ReportClient
    {
        public static Dictionary<string, object> QueryNames; 

        public static void Start(bool toExcel, bool excelReport, bool compositeReport)
        {
            if (Navigator.Manager.NotDefined(MethodInfo.GetCurrentMethod()))
            {

                if (toExcel)
                {
                    SearchControl.GetCustomMenuItems += (qn, type) => new ExcelReportMenuItem();
                }
                if (excelReport)
                {
                    QueryNames = Server.Return((IQueryServer s)=>s.GetQueryNames().ToDictionary(a => a.ToString())); 

                    SearchControl.GetCustomMenuItems += (qn, type) => qn == typeof(ExcelReportDN) ? null : new ExcelReportPivotTableMenuItem();

                    Navigator.Manager.Settings.Add(typeof(ExcelReportDN), new EntitySettings { View = () => new ExcelReport() });

                    if (compositeReport)
                    {
                        Navigator.Manager.Settings.Add(typeof(CompositeReportDN), new EntitySettings { View = () => new CompositeReport() });
                    }
                }
                else
                    if (compositeReport)
                        throw new InvalidOperationException();
            }
        }
    }
}
