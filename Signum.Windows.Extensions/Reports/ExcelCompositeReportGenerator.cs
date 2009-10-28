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
using System.Windows;
using System.Diagnostics;

namespace Signum.Windows.Reports
{
    public class ExcelCompositeReportGenerator
    {
        public static void GenerateCompositeReport(CompositeReportDN cr)
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


            string ruta = Path.GetDirectoryName(sfd.FileName) + "\\" + DateTime.Now.ToString("yyyyMMddhhmmss");
            DirectoryInfo di = System.IO.Directory.CreateDirectory(ruta);


            int errores = 0;
            string mensajes = "";
            string filename = "";
            foreach (Lite<ExcelReportDN> erl in cr.ExcelReports)
            {

                try
                {
                    // 1º nos taremos la plantilla y la guardamos
                    ExcelReportDN er = erl.RetrieveAndForget();
                    filename = di.FullName + "\\" + cr.Name + " - " + er.QueryName + ".xlsx";
                    File.WriteAllBytes(filename, er.File.BinaryFile);

                    // pedimos la consulta y traemos los datos
                    QueryResult queryResult = Server.Service<IQueryServer>().GetQueryResult(ReportClient.QueryNames[er.QueryName], null, null);
                    ExcelReportPivotTablesGenerator.GenerarInforme(filename, queryResult);
                    filename = "";

                }
                catch (Exception ex)
                {
                    errores += 1;
                    mensajes += filename + " mensaje:" + ex.Message + "\n\r";
                }

            }

            if (errores > 0)
                MessageBox.Show( mensajes,"Errores:" + errores.ToString());
            else
                MessageBox.Show("Generación de informe finalizada");

            Process.Start(ruta);

        }

    }
}
