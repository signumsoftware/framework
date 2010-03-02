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
using Signum.Utilities;

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
                DefaultExt = ".xml",
                Filter = Prop.Resources.Excel2003XmlSpreadsheet,
                OverwritePrompt = true,
                FileName = "{0}.xml".Formato(QueryUtils.GetNiceQueryName(SearchControl.QueryName)),
                Title = Prop.Resources.FindLocationFoExcelReport
            };

            if (sfd.ShowDialog(this.FindCurrentWindow()) == true)
            {
                PlainExcelGenerator.GenerateReport(sfd.FileName, SearchControl.ResultTable);
            }
        }

        public override void QueryResultChanged()
        {
            ResultTable qr = SearchControl.ResultTable;
            IsEnabled = (qr != null && qr.Rows.Length > 0);
        }
    }

}
