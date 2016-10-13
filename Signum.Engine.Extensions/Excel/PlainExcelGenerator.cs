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
using Signum.Utilities.Reflection;
using Signum.Entities.Excel;
using System.Reflection;

namespace Signum.Engine.Excel
{
    public static class PlainExcelGenerator
    {
        public static byte[] Template { get; set; }
        public static CellBuilder CellBuilder { get; set; }

        static PlainExcelGenerator()
        {
            SetTemplate(typeof(PlainExcelGenerator).Assembly.GetManifestResourceStream("Signum.Engine.Excel.plainExcelTemplate.xlsx"));
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
                        { TemplateCells.Title, worksheet.FindCell("A1").StyleIndex },
                        { TemplateCells.Header, worksheet.FindCell("A2").StyleIndex },
                        { TemplateCells.Date, worksheet.FindCell("B3").StyleIndex },
                        { TemplateCells.DateTime, worksheet.FindCell("C3").StyleIndex },
                        { TemplateCells.Text, worksheet.FindCell("D3").StyleIndex },
                        { TemplateCells.General, worksheet.FindCell("E3").StyleIndex },
                        { TemplateCells.Boolean, worksheet.FindCell("E3").StyleIndex },
                        { TemplateCells.Enum, worksheet.FindCell("E3").StyleIndex },
                        { TemplateCells.Number, worksheet.FindCell("F3").StyleIndex },
                        { TemplateCells.Decimal, worksheet.FindCell("G3").StyleIndex },
                        { TemplateCells.DecimalEuro, worksheet.FindCell("H3").StyleIndex },
                        { TemplateCells.DecimalDollar, worksheet.FindCell("I3").StyleIndex },
                        { TemplateCells.DecimalPound, worksheet.FindCell("J3").StyleIndex },
                        { TemplateCells.DecimalYuan, worksheet.FindCell("K3").StyleIndex },
                    }
                };
            }
        }
        
        public static byte[] WritePlainExcel(ResultTable results,string title)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                WritePlainExcel(results, ms,title);
                return ms.ToArray(); 
            }
        }

        public static void WritePlainExcel(ResultTable results, string fileName,string title)
        {
            using (FileStream fs = File.Create(fileName))
                WritePlainExcel(results, fs,title);
        }

        static void WritePlainExcel(ResultTable results, Stream stream, string title)
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
                   new [] { CellBuilder.Cell(title,TemplateCells.Title) }.ToRow(),

                    (from c in results.Columns
                    select CellBuilder.Cell(c.Column.DisplayName, TemplateCells.Header)).ToRow(),

                    from r in results.Rows
                    select (from c in results.Columns
                            let template = CellBuilder.GetTemplateCell(c)
                            select CellBuilder.Cell(r[c], template)).ToRow()
                }.ToSheetData());

                workbookPart.Workbook.Save();
                document.Close();
            }
        }

        public static byte[] WritePlainExcel<T>(IEnumerable<T> results)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                WritePlainExcel(results, ms);
                return ms.ToArray();
            }
        }

        public static void WritePlainExcel<T>(IEnumerable<T> results, string fileName)
        {
            using (FileStream fs = File.Create(fileName))
                WritePlainExcel(results, fs);
        }

        static void WritePlainExcel<T>(IEnumerable<T> results, Stream stream)
        {
            stream.WriteAllBytes(Template);

            if (results == null)
                throw new ApplicationException(ExcelMessage.ThereAreNoResultsToWrite.NiceToString());
            
            var members = MemberEntryFactory.GenerateList<T>(MemberOptions.Fields | MemberOptions.Properties | MemberOptions.Typed | MemberOptions.Getter);
            var formats = members.ToDictionary(a => a.Name, a => a.MemberInfo.GetCustomAttribute<FormatAttribute>()?.Format);

            using (SpreadsheetDocument document = SpreadsheetDocument.Open(stream, true))
            {
                document.PackageProperties.Creator = "";
                document.PackageProperties.LastModifiedBy = "";

                WorkbookPart workbookPart = document.WorkbookPart;

                WorksheetPart worksheetPart = document.GetWorksheetPartById("rId1");

                worksheetPart.Worksheet = new Worksheet();

                worksheetPart.Worksheet.Append(new Columns(members.Select((c, i) => new spreadsheet.Column()
                {
                    Min = (uint)i + 1,
                    Max = (uint)i + 1,
                    Width = GetColumnWidth(c.MemberInfo.ReturningType()),
                    BestFit = true,
                    CustomWidth = true
                }).ToArray()));

                worksheetPart.Worksheet.Append(new Sequence<Row>()
                {
                    (from c in members
                    select CellBuilder.Cell(c.Name, TemplateCells.Header)).ToRow(),

                    from r in results
                    select (from c in members
                            let template = formats.TryGetC(c.Name) == "d" ? TemplateCells.Date : CellBuilder.GetTemplateCell(c.MemberInfo.ReturningType())
                            select CellBuilder.Cell(c.Getter(r), template)).ToRow()
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

        public static List<T> ReadPlainExcel<T>(Stream stream, Func<string[], T> selector)
        {
            using (SpreadsheetDocument document = SpreadsheetDocument.Open(stream, false))
            {
                WorkbookPart workbookPart = document.WorkbookPart;

                WorksheetPart worksheetPart = document.GetWorksheetPartById("rId1");

                var data = worksheetPart.Worksheet.Descendants<SheetData>().Single();

                return data.Descendants<Row>().Skip(1).Select(r => selector(r.Descendants<Cell>().Select(c => document.GetCellValue(c)).ToArray())).ToList();
            }
        }
    }
}
