using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Win32;
using System.Windows.Media.Imaging;
using Signum.Entities.DynamicQuery;
using System.Windows.Controls;
using System.Windows.Media;
using Signum.Utilities;
using System.IO;
using Signum.Services;
using System.Windows;
using Signum.Entities.Excel;

namespace Signum.Windows.Excel
{
    public static class PlainExcelMenuItemConstructor
    {
        public static MenuItem Construct(SearchControl sc)
        {
            MenuItem miResult = new MenuItem()
            {
                Header = ExcelMessage.ExcelReport.NiceToString(),
                Icon = ExtensionsImageLoader.GetImageSortName("excelPlain.png").ToSmallImage(),
            };

            miResult.Click += (object sender, RoutedEventArgs e)=>
            {
                e.Handled = true;

                SaveFileDialog sfd = new SaveFileDialog()
                {
                    AddExtension = true,
                    DefaultExt = ".xlsx",
                    Filter = ExcelMessage.Excel2007Spreadsheet.NiceToString(),
                    OverwritePrompt = true,
                    FileName = "{0}.xlsx".FormatWith(QueryUtils.GetNiceName(sc.QueryName)),
                    Title = ExcelMessage.FindLocationFoExcelReport.NiceToString()
                };

                if (sfd.ShowDialog(Window.GetWindow(sc)) == true)
                {
                    var request = sc.GetQueryRequest(true);

                    byte[] file = Server.Return((IExcelReportServer s) => s.ExecutePlainExcel(request));

                    File.WriteAllBytes(sfd.FileName, file);

                    System.Diagnostics.Process.Start(sfd.FileName);
                }
            };

            sc.ResultChanged += (object sender, ResultChangedEventArgs e)=>
            {
                ResultTable qr = sc.ResultTable;
                miResult.IsEnabled = (qr != null && qr.Rows.Length > 0);
            };

            return miResult;
        }
    }

}
