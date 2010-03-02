
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
        public static void Start(bool toExcel, bool excelReport, bool compositeReport)
        {
            if (Navigator.Manager.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                if (excelReport)
                {
                    if (toExcel)
                        SearchControl.GetCustomMenuItems += (qn, type) => qn == typeof(ExcelReportDN) ? null : new ReportMenuItem() { PlainExcelMenuItem = new PlainExcelMenuItem() };
                    else
                        SearchControl.GetCustomMenuItems += (qn, type) => new PlainExcelMenuItem();

                    QueryClient.Start();

                    Navigator.Manager.Settings.Add(typeof(ExcelReportDN), new EntitySettings(EntityType.Default) { View = e => new ExcelReport() });

                    if (compositeReport)
                    {
                        Navigator.Manager.Settings.Add(typeof(CompositeReportDN), new EntitySettings(EntityType.Default) { View = e => new CompositeReport() });
                    }
                }
                else
                {
                    if (toExcel)
                    {
                        SearchControl.GetCustomMenuItems += (qn, type) => new PlainExcelMenuItem();
                    }

                    if (compositeReport)
                        throw new InvalidOperationException();

                }
            }
        }
    }
}
