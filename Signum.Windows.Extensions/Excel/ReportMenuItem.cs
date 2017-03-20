using System;
using System.Collections.Generic;
using Signum.Entities.DynamicQuery;
using Signum.Entities;
using System.Windows;
using System.Windows.Controls;
using System.IO;
using Microsoft.Win32;
using Signum.Services;
using System.Windows.Documents;
using Signum.Utilities;
using Signum.Entities.Excel;

namespace Signum.Windows.Excel
{
    public static class ExcelMenuItemConstructor
    {
        public static MenuItem Construct(SearchControl sc, MenuItem plainExcelMenuItem)
        {
            MenuItem miResult = new MenuItem
            {
                Header = ExcelMessage.Reports.NiceToString(),
                Icon = ExtensionsImageLoader.GetImageSortName("excel.png").ToSmallImage()

            };

            miResult.AddHandler(MenuItem.ClickEvent, new RoutedEventHandler((object sender, RoutedEventArgs e) =>
            {
                e.Handled = true;

                if (e.OriginalSource is MenuItem b) //Not to capture the mouseDown of the scrollbar buttons
                {
                    Lite<ExcelReportEntity> reportLite = (Lite<ExcelReportEntity>)b.Tag;

                    SaveFileDialog sfd = new SaveFileDialog()
                    {
                        AddExtension = true,
                        DefaultExt = ".xlsx",
                        Filter = ExcelMessage.Excel2007Spreadsheet.NiceToString(),
                        FileName = reportLite.ToString() + " - " + DateTime.Now.ToString("yyyyMMddHHmmss") + ".xlsx",
                        OverwritePrompt = true,
                        Title = ExcelMessage.FindLocationFoExcelReport.NiceToString()
                    };

                    if (sfd.ShowDialog(Window.GetWindow(sc)) == true)
                    {
                        byte[] result = Server.Return((IExcelReportServer r) => r.ExecuteExcelReport(reportLite, sc.GetQueryRequest(true)));

                        File.WriteAllBytes(sfd.FileName, result);

                        System.Diagnostics.Process.Start(sfd.FileName);
                    }
                }
            }));

            Action initialize = null;

            MenuItem miAdmin = new MenuItem()
            {
                Header = "Administrar",
                Icon = ExtensionsImageLoader.GetImageSortName("folderedit.png").ToSmallImage()
            };

            miAdmin.Click += (object sender, RoutedEventArgs e)=>
            {
                var query = QueryClient.GetQuery(sc.QueryName);

                Finder.Explore(new ExploreOptions(typeof(ExcelReportEntity))
                {
                    ShowFilters = false,
                    ShowFilterButton = false,
                    FilterOptions = new List<FilterOption>
                    {
                        new FilterOption 
                        { 
                            ColumnName = "Query", 
                            Operation = FilterOperation.EqualTo, 
                            Value = query.ToLite(query.IsNew),
                            Frozen = true
                        }
                    },
                    Closed = (_, __) => miAdmin.Dispatcher.Invoke(() => initialize()) //Refrescar lista de informes tras salir del administrador
                });

                e.Handled = true;
            };

            initialize = ()=>
            {
                miResult.Items.Clear();

                List<Lite<ExcelReportEntity>> reports = Server.Return((IExcelReportServer s)=>s.GetExcelReports(sc.QueryName));

                if (plainExcelMenuItem != null)
                {
                    miResult.Items.Add(plainExcelMenuItem);
                    miResult.Items.Add(new Separator());
                }

                miResult.Header = new TextBlock { Inlines = { new Run(ExcelMessage.Reports.NiceToString()), reports.Count == 0? (Inline)new Run(): new Bold(new Run(" (" + reports.Count + ")")) } };

                if (reports.Count > 0)
                {
                    foreach (Lite<ExcelReportEntity> report in reports)
                    {
                        MenuItem mi = new MenuItem()
                        {
                            Header = report.ToString(),
                            Icon = ExtensionsImageLoader.GetImageSortName("excelDoc.png").ToSmallImage(),
                            Tag = report,
                        };
                        miResult.Items.Add(mi);
                    }
                }          

          
                miResult.Items.Add(miAdmin);
            };

            initialize();

            return miResult;
        }
    }
}
