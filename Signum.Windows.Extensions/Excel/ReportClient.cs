
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Services;
using System.Reflection;
using Signum.Entities.Excel;

namespace Signum.Windows.Excel
{
    public class ExcelClient
    {
        public static void Start(bool toExcel, bool excelReport)
        {
            if (Navigator.Manager.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                if (excelReport)
                {
                    if (toExcel)
                        SearchControl.GetMenuItems += sc => (sc.QueryName as Type) == typeof(ExcelReportEntity) ? null : ExcelMenuItemConstructor.Construct(sc, PlainExcelMenuItemConstructor.Construct(sc));
                    else
                        SearchControl.GetMenuItems += sc => (sc.QueryName as Type) == typeof(ExcelReportEntity) ? null : ExcelMenuItemConstructor.Construct(sc, null);

                    QueryClient.Start();

                    Navigator.AddSetting(new EntitySettings<ExcelReportEntity> { View = e => new ExcelReport() });
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
