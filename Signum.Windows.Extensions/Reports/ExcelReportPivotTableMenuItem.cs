using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities.Extensions;
using Signum.Entities.DynamicQuery;
using Signum.Entities;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Controls;
using System.IO;
using Microsoft.Win32;
using Signum.Entities.Reports;
using Prop = Signum.Windows.Extensions.Properties;
using Signum.Services;
using System.Windows.Documents;

namespace Signum.Windows.Reports
{
    public class ExcelReportPivotTableMenuItem : SearchControlMenuItem
    {
        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);
            Header = Prop.Resources.CustomReport;
            Icon = new Image { Width = 16, Height = 16, Source = new BitmapImage(PackUriHelper.Reference("Images/excelDoc.png", typeof(ExcelReportPivotTableMenuItem))) };
        }

        protected override void Initialize()
        {
            Items.Clear();

            this.AddHandler(MenuItem.ClickEvent, new RoutedEventHandler(MenuItem_Clicked));

            List<Lite<ExcelReportDN>> reports = Server.Service<IExcelReportServer>().GetExcelReports(SearchControl.QueryName.ToString());
            
            if (reports.Count > 0)
            {
                Header = new TextBlock { Inlines = { new Run(Prop.Resources.CustomReport), new Bold(new Run(" (" + reports.Count + ")")) } };

                foreach (Lite<ExcelReportDN> report in reports)
                {
                    MenuItem mi = new MenuItem()
                    {
                        Header = report.ToStr,
                        Tag = report,
                    };
                    Items.Add(mi);
                }
            }          
           

            Items.Add(new Separator());

            MenuItem miAdmin = new MenuItem()
            {
                Header = "Administrar",
                Icon = new Image { Source = BitmapFrame.Create(new Uri("pack://application:,,,/Signum.Windows.Extensions;component/Images/folderedit.png", UriKind.Absolute)) }
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
                if (extension != ".xls" && extension != ".xlsx")
                    throw new ApplicationException("El template de los ficheros Excel personalizados debe tener la extensión .xls o .xlsx, y el fichero seleccionado tiene " + extension);

                SaveFileDialog sfd = new SaveFileDialog()
                {
                    AddExtension = true,
                    DefaultExt = extension, //".xls", //".xlsx",
                    Filter = (extension == ".xls") ? Prop.Resources.Excel97_2003Spreadsheet : Prop.Resources.Excel2007Spreadsheet, //.Excel2007Spreadsheet,
                    OverwritePrompt = true,
                    Title = Prop.Resources.FindLocationFoExcelReport
                };

                if (sfd.ShowDialog(this.FindCurrentWindow()) == true)
                {
                    File.WriteAllBytes(sfd.FileName, report.File.BinaryFile);

                    ExcelReportPivotTablesGenerator.GenerarInforme(sfd.FileName, SearchControl.QueryResult);

                    System.Diagnostics.Process.Start(sfd.FileName);
                }
            }

        }

        private void MenuItemAdmin_Clicked(object sender, RoutedEventArgs e)
        {
            Navigator.Find(new FindOptions(typeof(ExcelReportDN))
            {
                OnLoadMode = OnLoadMode.Search,
                FilterMode = FilterMode.AlwaysHidden,
                FilterOptions = new List<FilterOptions>
                {
                    new FilterOptions 
                    { 
                        ColumnName = "QueryName", 
                        Operation = FilterOperation.EqualTo, 
                        Value = SearchControl.QueryName.ToString(),
                        Frozen = true
                    }
                },
                Buttons = SearchButtons.Close,
                Modal = true
            });

            //Refrescar lista de informes tras salir del administrador
            Initialize();

            e.Handled = true;
        }

    }

}
