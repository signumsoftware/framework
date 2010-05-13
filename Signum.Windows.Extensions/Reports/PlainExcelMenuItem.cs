using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Win32;
using System.Windows.Media.Imaging;
using Signum.Entities.DynamicQuery;
using Signum.Entities.Reports;
using System.Windows.Controls;
using System.Windows.Media;
using Prop = Signum.Windows.Extensions.Properties;
using Signum.Utilities;
using System.IO;

namespace Signum.Windows.Reports
{
    internal class PlainExcelMenuItem : SearchControlMenuItem
    {
        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);
            Header = Prop.Resources.ExcelReport;
            Icon = GetImage(ExtensionsImageLoader.GetImageSortName("excelPlain.png"));
        }

        protected override void OnClick()
        {
            SaveFileDialog sfd = new SaveFileDialog()
            {
                AddExtension = true,
                DefaultExt = ".xlsx",
                Filter = Prop.Resources.Excel2007Spreadsheet,
                OverwritePrompt = true,
                FileName = "{0}.xlsx".Formato(QueryUtils.GetNiceQueryName(SearchControl.QueryName)),
                Title = Prop.Resources.FindLocationFoExcelReport
            };

            if (sfd.ShowDialog(this.FindCurrentWindow()) == true)
            {
                PlainExcelGenerator.WritePlainExcel(SearchControl.ResultTable, sfd.FileName);
                System.Diagnostics.Process.Start(sfd.FileName);
            }
        }

        public override void QueryResultChanged()
        {
            ResultTable qr = SearchControl.ResultTable;
            IsEnabled = (qr != null && qr.Rows.Length > 0);
        }
    }

}
