using System;
using System.Collections.Generic;
using Signum.Entities.DynamicQuery;
using Signum.Entities;
using System.Windows;
using System.Windows.Controls;
using System.IO;
using Microsoft.Win32;
using Signum.Entities.Reports;
using Prop = Signum.Windows.Extensions.Properties;
using Signum.Services;
using System.Windows.Documents;

namespace Signum.Windows.Reports
{
    public class ReportMenuItem : SearchControlMenuItem
    {
        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);
            Header = Prop.Resources.Reports;
            Icon = ExtensionsImageLoader.GetImageSortName("excel.png").ToSmallImage();
        }

        internal PlainExcelMenuItem PlainExcelMenuItem; 

        public override void Initialize()
        {
            Items.Clear();

            this.AddHandler(MenuItem.ClickEvent, new RoutedEventHandler(MenuItem_Clicked));

            List<Lite<ExcelReportDN>> reports = Server.Return((IExcelReportServer s)=>s.GetExcelReports(SearchControl.QueryName));

            if (PlainExcelMenuItem != null)
            {
                Items.Add(PlainExcelMenuItem);
                PlainExcelMenuItem.SearchControl = SearchControl;
                PlainExcelMenuItem.Initialize();
                Items.Add(new Separator());
            }

            Header = new TextBlock { Inlines = { new Run(Prop.Resources.Reports), reports.Count == 0? (Inline)new Run(): new Bold(new Run(" (" + reports.Count + ")")) } };

            if (reports.Count > 0)
            {
                foreach (Lite<ExcelReportDN> report in reports)
                {
                    MenuItem mi = new MenuItem()
                    {
                        Header = report.ToStr,
                        Icon = ExtensionsImageLoader.GetImageSortName("excelDoc.png").ToSmallImage(),
                        Tag = report,
                    };
                    Items.Add(mi);
                }
            }          

            MenuItem miAdmin = new MenuItem()
            {
                Header = "Administrar",
                Icon = ExtensionsImageLoader.GetImageSortName("folderedit.png").ToSmallImage()
            };
            miAdmin.Click += new RoutedEventHandler(MenuItemAdmin_Clicked);
            Items.Add(miAdmin);
        }

        private void MenuItem_Clicked(object sender, RoutedEventArgs e)
        {
            e.Handled = true;

            if (e.OriginalSource is MenuItem) //Not to capture the mouseDown of the scrollbar buttons
            {
                MenuItem b = (MenuItem)e.OriginalSource;
                Lite<ExcelReportDN> reportLite = (Lite<ExcelReportDN>)b.Tag;

                SaveFileDialog sfd = new SaveFileDialog()
                {
                    AddExtension = true,
                    DefaultExt = ".xlsx",
                    Filter = Prop.Resources.Excel2007Spreadsheet,
                    FileName = reportLite.ToStr + " - " + DateTime.Now.ToString("yyyyMMddhhmmss") + ".xlsx",
                    OverwritePrompt = true,
                    Title = Prop.Resources.FindLocationFoExcelReport
                };

                if (sfd.ShowDialog(this.FindCurrentWindow()) == true)
                {
                    byte[] result = Server.Return((IExcelReportServer r) => r.ExecuteExcelReport(reportLite, SearchControl.GetQueryRequest(true)));

                    File.WriteAllBytes(sfd.FileName, result);

                    System.Diagnostics.Process.Start(sfd.FileName);
                }
            }

        }

        private void MenuItemAdmin_Clicked(object sender, RoutedEventArgs e)
        {
            var query = QueryClient.GetQuery(SearchControl.QueryName);

            Navigator.Explore(new ExploreOptions(typeof(ExcelReportDN))
            {
                ShowFilters = false,
                ShowFilterButton = false,
                FilterOptions = new List<FilterOption>
                {
                    new FilterOption 
                    { 
                        Path = "Query", 
                        Operation = FilterOperation.EqualTo, 
                        Value = query.ToLite(query.IsNew),
                        Frozen = true
                    }
                },
                Closed = (_, __) => Initialize() //Refrescar lista de informes tras salir del administrador
            });

            e.Handled = true;
        }

        public override void QueryResultChanged()
        {
            if (PlainExcelMenuItem != null)
                PlainExcelMenuItem.QueryResultChanged(); 
        }

    }
}
