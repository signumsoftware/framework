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
            Icon = GetImage(ExtensionsImageLoader.GetImageSortName("excel.png"));
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
                        Icon = GetImage(ExtensionsImageLoader.GetImageSortName("excelDoc.png")),
                        Tag = report,
                    };
                    Items.Add(mi);
                }
            }          

            MenuItem miAdmin = new MenuItem()
            {
                Header = "Administrar",
                Icon = GetImage( ExtensionsImageLoader.GetImageSortName("folderedit.png"))
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

                ExcelReportDN report = reportLite.RetrieveAndForget();
                string extension = Path.GetExtension(report.File.FileName);
                if (extension != ".xlsx")
                    throw new ApplicationException("El template de los ficheros Excel personalizados debe tener la extensión .xlsx, y el fichero seleccionado tiene " + extension);

                SaveFileDialog sfd = new SaveFileDialog()
                {
                    AddExtension = true,
                    DefaultExt = extension, //".xlsx",
                    Filter = Prop.Resources.Excel2007Spreadsheet,
                    FileName = report.DisplayName + " - " + DateTime.Now.ToString("yyyyMMddhhmmss") + extension,
                    OverwritePrompt = true,
                    Title = Prop.Resources.FindLocationFoExcelReport
                };

                if (sfd.ShowDialog(this.FindCurrentWindow()) == true)
                {
                    File.WriteAllBytes(sfd.FileName, report.File.BinaryFile);

                    ExcelGenerator.WriteDataInExcelFile(SearchControl.ResultTable, sfd.FileName);

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
