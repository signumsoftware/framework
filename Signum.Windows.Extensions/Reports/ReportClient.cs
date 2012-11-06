
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
        public static void Start(bool toExcel, bool excelReport)
        {
            if (Navigator.Manager.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                if (excelReport)
                {
                    if (toExcel)
                        SearchControl.GetMenuItems += sc => sc.QueryName == typeof(ExcelReportDN) ? null : ReportMenuItemConstructor.Construct(sc, PlainExcelMenuItemConstructor.Construct(sc));
                    else
                        SearchControl.GetMenuItems += sc => sc.QueryName == typeof(ExcelReportDN) ? null : ReportMenuItemConstructor.Construct(sc, null);

                    QueryClient.Start();

                    Navigator.AddSetting(new EntitySettings<ExcelReportDN>(EntityType.Main) { View = e => new ExcelReport() });
                }
                else
                {
                    if (toExcel)
                    {
                        SearchControl.GetMenuItems += sc => PlainExcelMenuItemConstructor.Construct(sc);
                    }
                }
            }
        }
    }
}
