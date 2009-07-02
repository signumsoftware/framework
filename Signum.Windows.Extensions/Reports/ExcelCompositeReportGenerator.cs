using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities.Reports;
using Signum.Entities.DynamicQuery;
using Signum.Windows.Reports;
using Signum.Services;
using System.IO;
using Microsoft.Win32;
using Signum.Windows.Extensions.Properties;
using Signum.Entities;

namespace Signum.Windows.Reports
{
    public class ExcelCompositeReportGenerator
    {
       public  static void GenerateCompositeReport(CompositeReportDN cr)
        {


            // establecer la ruta a guardar
            //DefaultExt = extension, //".xls", //".xlsx",
            //Filter = (extension == ".xls") ? Prop.Resources.Excel97_2003Spreadsheet : Prop.Resources.Excel2007Spreadsheet, //.Excel2007Spreadsheet,

            SaveFileDialog sfd = new SaveFileDialog()
            {
                AddExtension = true,
                OverwritePrompt = true,
                Title = Resources.FindLocationFoExcelReport
            };

            sfd.ShowDialog();


            string ruta = Path.GetDirectoryName(sfd.FileName) + "\\" + DateTime.Now.ToShortDateString().Replace("/","") + DateTime.Now.ToShortTimeString().Replace (":","").Replace (" ","") ;
            DirectoryInfo di = System.IO.Directory.CreateDirectory(ruta);

            foreach (Lazy<ExcelReportDN> erl in cr.ExcelReports)
            {

                // 1º nos taremos la plantilla y la guardamos
                ExcelReportDN er = erl.RetrieveLazyThin();
                string filename = di.FullName + cr.Nombre + " - " + er.QueryName;
                File.WriteAllBytes(filename, er.File.BinaryFile);

                // pedimos la consulta y traemos los datos
                QueryResult queryResult = Server.Service<IQueryServer>().GetQueryResult(er.QueryName, null, null);
                ExcelReportPivotTablesGenerator.GenerarInforme(filename, queryResult);

            }
        }

    }
}
