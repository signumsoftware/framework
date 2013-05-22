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
using Signum.Entities.Reflection;
using Signum.Entities;
using Signum.Entities.Reports;
#endregion

namespace Signum.Engine.Reports
{
    public static class PlainExcelGenerator
    {
        public static byte[] Template { get; set; }
        public static CellBuilder CellBuilder { get; set; }

        static PlainExcelGenerator()
        {
            SetTemplate(typeof(PlainExcelGenerator).Assembly.GetManifestResourceStream("Signum.Engine.Extensions.Reports.plainExcelTemplate.xlsx"));
        }

        public static void SetTemplate(Stream templateStream)
        {
            Template = templateStream.ReadAllBytes();

            MemoryStream memoryStream = new MemoryStream();
            memoryStream.WriteAllBytes(Template);

            using (SpreadsheetDocument document = SpreadsheetDocument.Open(memoryStream, true))
            {
                document.PackageProperties.Creator = "";
                document.PackageProperties.LastModifiedBy = "";

                WorkbookPart workbookPart = document.WorkbookPart;

                WorksheetPart worksheetPart = document.GetWorksheetPartById("rId1");
                Worksheet worksheet = worksheetPart.Worksheet;

                CellBuilder = new CellBuilder()
                {
                    DefaultStyles = new Dictionary<TemplateCells, UInt32Value>
                    {
                        { TemplateCells.Header, worksheet.FindCell("A1").StyleIndex },

                        { TemplateCells.Date, worksheet.FindCell("B2").StyleIndex },
                        { TemplateCells.DateTime, worksheet.FindCell("C2").StyleIndex },
                        { TemplateCells.Text, worksheet.FindCell("D2").StyleIndex },
                        { TemplateCells.General, worksheet.FindCell("E2").StyleIndex },
                        { TemplateCells.Number, worksheet.FindCell("F2").StyleIndex },
                        { TemplateCells.Decimal, worksheet.FindCell("G2").StyleIndex },
                    }
                };
            }
        }
        
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
            stream.WriteAllBytes(Template);

            if (results == null)
                throw new ApplicationException(ExcelMessage.ThereAreNoResultsToWrite.NiceToString());

            using (SpreadsheetDocument document = SpreadsheetDocument.Open(stream, true))
            {
                document.PackageProperties.Creator = "";
                document.PackageProperties.LastModifiedBy = "";

                WorkbookPart workbookPart = document.WorkbookPart;

                WorksheetPart worksheetPart = document.GetWorksheetPartById("rId1");
                
                worksheetPart.Worksheet = new Worksheet();

                worksheetPart.Worksheet.Append(new Columns(results.Columns.Select((c, i) => new spreadsheet.Column()
                    {
                        Min = (uint)i + 1,
                        Max = (uint)i + 1,
                        Width = GetColumnWidth(c.Column.Type),
                        BestFit = true,
                        CustomWidth = true
                    }).ToArray()));

                worksheetPart.Worksheet.Append(new Sequence<Row>()
                {
                    (from c in results.Columns
                    select CellBuilder.Cell(c.Column.DisplayName, TemplateCells.Header)).ToRow(),

                    from r in results.Rows
                    select (from c in results.Columns
                            let template = c.Column.Format == "d" ? TemplateCells.Date : CellBuilder.GetTemplateCell(c.Column.Type)
                            select CellBuilder.Cell(r[c], template)).ToRow()
                }.ToSheetData());

                workbookPart.Workbook.Save();
                document.Close();
            }
        }

        static double GetColumnWidth(Type type)
        { 
            type = type.UnNullify();

            if (type == typeof(DateTime))
                return 20;
            if (type == typeof(string))
                return 50;
            if (type.IsLite())
                return 50;

            return 10;
        }
    }
}
