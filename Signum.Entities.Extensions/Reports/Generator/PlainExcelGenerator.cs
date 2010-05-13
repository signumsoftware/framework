#region usings
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using spreadsheet = DocumentFormat.OpenXml.Spreadsheet;
using DocumentFormat.OpenXml.Spreadsheet;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using Signum.Entities.DynamicQuery;
using System.IO;
using Signum.Utilities.DataStructures;
using Signum.Utilities;
using System.Globalization;
using Signum.Entities.Extensions.Properties;
using Signum.Entities.Reflection;
#endregion

namespace Signum.Entities.Reports
{
    public static class PlainExcelGenerator
    {
        public static byte[] WritePlainExcel(ResultTable results)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                WritePlainExcel(results, ms);
                return ms.ToArray(); 
            }
        }

        public static void WritePlainExcel(ResultTable results, string fileName)
        {
            using (FileStream fs = File.Create(fileName))
                WritePlainExcel(results, fs);
        }

        static void WritePlainExcel(ResultTable results, Stream stream)
        {
            typeof(PlainExcelGenerator).Assembly.GetManifestResourceStream("Signum.Entities.Extensions.Reports.Generator.plainExcelTemplate.xlsx").CopyTo(stream); 

            if (results == null)
                throw new ApplicationException(Resources.ThereAreNoResultsToWrite);

            using (SpreadsheetDocument document = SpreadsheetDocument.Open(stream, true))
            {
                WorkbookPart workbookPart = document.WorkbookPart;

                WorksheetPart worksheetPart = document.GetWorksheetPart("rId1");
                Worksheet worksheet = worksheetPart.Worksheet;

                CellBuilder cb = new CellBuilder()
                {
                    DefaultStyles = new Dictionary<TemplateCells, UInt32Value>
                    {
                        { TemplateCells.Date, worksheet.FindCell("B2").StyleIndex },
                        { TemplateCells.DateTime, worksheet.FindCell("C2").StyleIndex },
                        { TemplateCells.Text, worksheet.FindCell("D2").StyleIndex },
                        { TemplateCells.General, worksheet.FindCell("E2").StyleIndex },
                        { TemplateCells.Number, worksheet.FindCell("F2").StyleIndex },
                        { TemplateCells.Decimal, worksheet.FindCell("G2").StyleIndex },
                    }
                };

                UInt32Value headerStyleIndex = worksheet.FindCell("A1").StyleIndex;

                worksheetPart.Worksheet = new Worksheet(
                        new Sequence<Row>()
                        {
                            (from c in results.VisibleColumns
                            select cb.Cell(c.DisplayName, headerStyleIndex)).ToRow(),

                            from r in results.Rows
                            select (from c in results.VisibleColumns
                                    let template = c.Format == "d"?TemplateCells.Date : cb.GetTemplateCell(c.Type)
                                    select cb.Cell(r[c], template)).ToRow()
                        }.ToSheetData());
                
                //Columns columns1 = new Columns();

                //for (uint i = 1; i <= results.VisibleColumns.Count(); i++)
                //    columns1.Append(
                //        new spreadsheet.Column() { Min = i, Max = i, Width=31, BestFit = true, CustomWidth = true });

                //worksheet.Append(columns1);

                //workbookPart.AddWorksheet(Resources.Data, worksheet);

                //document.XLDeleteSheet("rId1");
              
                workbookPart.Workbook.Save();
                document.Close();
            }
        }
    }
}
