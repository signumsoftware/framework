using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Web.Mvc;
using Signum.Utilities;
using System.Web.UI;

namespace Signum.Web.Reports
{
    public class ReportClient
    {
        static bool ToExcel;
        static bool ExcelReport;
        static bool CompositeReport;

        public static string ToExcelControllerUrl = "Report/ToExcel";

        public static void Start(bool toExcel, bool excelReport, bool compositeReport)
        {
            if (Navigator.Manager.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                ToExcel = toExcel;
                ExcelReport = excelReport;
                CompositeReport = compositeReport;

                if (toExcel || excelReport || compositeReport)
                    ButtonBarQueryHelper.GetButtonBarForQueryName +=new GetToolBarButtonQueryDelegate(ButtonBarQueryHelper_GetButtonBarForQueryName); 
            }
        }

        static ToolBarButton[] ButtonBarQueryHelper_GetButtonBarForQueryName(ControllerContext controllerContext, object queryName, Type entityType, string prefix)
        {
            return new ToolBarButton[]
            {
                new ToolBarButton 
                { 
                    AltText = "Excel", 
                    //ImgSrc = new ScriptManager().ClientScript.GetWebResourceUrl(typeof(ReportClient), "excelPlain.png"), 
                    Text = "Exportar a Excel",
                    OnClick = "SubmitOnly('{0}', new FindNavigator({{prefix:'{1}'}}).requestData());".Formato(ToExcelControllerUrl, prefix), 
                    DivCssClass = ToolBarButton.DefaultQueryDivCssClass
                }
            };
        }
    }
}
