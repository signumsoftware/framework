using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Win32;
using System.Windows.Media.Imaging;
using Signum.Entities.DynamicQuery;
using System.Windows.Controls;
using System.Windows.Media;
using Prop = Signum.Windows.Extensions.Properties;

namespace Signum.Windows.Reports
{
    public class ExcelReportMenuItem : SearchControlMenuItem
    {
        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);
            Header = Prop.Resources.ExcelReport;
            Icon = new Image { Width = 16, Height = 16, Source = new BitmapImage(PackUriHelper.Reference("Images/excel.png", typeof(ExcelReportMenuItem))) };
        }

        protected override void OnClick()
        {
            SaveFileDialog sfd = new SaveFileDialog()
            {
                AddExtension = true,
                DefaultExt = ".xml",
                Filter = Prop.Resources.Excel2003XmlSpreadsheet,
                OverwritePrompt = true,
                Title = Prop.Resources.FindLocationFoExcelReport
            };

            if (sfd.ShowDialog(this.FindCurrentWindow()) == true)
            {
                ExcelReportGenerator.GenerateReport(sfd.FileName, SearchControl.QueryResult);
            }
        }

        protected override void QueryResultChanged()
        {
            QueryResult qr = SearchControl.QueryResult;
            IsEnabled = (qr != null && qr.Data.Length > 0);
        }
    }

}
