using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DocumentFormat.OpenXml.Spreadsheet;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using Signum.Entities.DynamicQuery;
using System.IO;
using Signum.Utilities.DataStructures;
using Signum.Utilities;

namespace Signum.Windows.Reports
{
    public static class ExcelExtensions
    {
        static Dictionary<TypeCode, CellValues> TypesConverter = new Dictionary<TypeCode, CellValues> 
        {
            {TypeCode.Boolean, CellValues.Boolean},
            {TypeCode.Byte, CellValues.Number},
            {TypeCode.Char, CellValues.InlineString},
            {TypeCode.DateTime, CellValues.Date},
            {TypeCode.DBNull, CellValues.InlineString},
            {TypeCode.Decimal, CellValues.Number},
            {TypeCode.Double, CellValues.Number},
            {TypeCode.Empty, CellValues.InlineString},
            {TypeCode.Int16, CellValues.Number},
            {TypeCode.Int32, CellValues.Number},
            {TypeCode.Int64, CellValues.Number},
            {TypeCode.Object, CellValues.InlineString},
            {TypeCode.SByte, CellValues.Number},
            {TypeCode.Single, CellValues.Number},
            {TypeCode.String, CellValues.InlineString},
            {TypeCode.UInt16, CellValues.Number},
            {TypeCode.UInt32, CellValues.Number},
            {TypeCode.UInt64, CellValues.Number}
        };


        public static Cell Cell<T>(T value)
        {
            return Cell(typeof(T), value);
        }

        public static Cell Cell(Type type, object value)
        {
            var cellValue = TypesConverter.TryGetS(Type.GetTypeCode(type)) ?? CellValues.InlineString;
            if (cellValue == CellValues.InlineString)
                return new Cell(new InlineString(new Text { Text = value.TryToString() })) { DataType = CellValues.InlineString };
            else
                return new Cell(new CellValue { Text = value.TryToString() }) { DataType = cellValue };
        }

        public static SheetData ToSheetData(this IEnumerable<Row> rows)
        {
            return new SheetData(rows.Cast<OpenXmlElement>());
        }

        public static Row ToRow(this IEnumerable<Cell> rows)
        {
            return new Row(rows.Cast<OpenXmlElement>());
        }

        public static WorksheetPart AddWorksheet(this WorkbookPart workbookPart, string name, Worksheet sheet)
        {
            var result = workbookPart.AddNewPart<WorksheetPart>();
            result.Worksheet = sheet;
            sheet.Save();

            workbookPart.Workbook.Sheets.Append(
                new Sheet
                {
                    Name = name,
                    SheetId = (uint)workbookPart.Workbook.Sheets.Count() + 1,
                    Id = workbookPart.GetIdOfPart(result)
                });

            return result;
        }
    }

    public static class PlainExcelGenerator2
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
            using (SpreadsheetDocument s = SpreadsheetDocument.Create(stream, SpreadsheetDocumentType.Workbook))
            {
                WorkbookPart workbookPart = s.AddWorkbookPart();
                workbookPart.Workbook = new Workbook
                {
                    FileVersion = new FileVersion { ApplicationName = "Microsoft Office Excel" },
                    Sheets = new Sheets()
                };

                workbookPart.AddWorksheet("Datos",
                    new Worksheet(
                        new Sequence<Row>()
                        {
                            (from c in results.VisibleColumns
                            select ExcelExtensions.Cell(c.DisplayName)).ToRow(),
                            from r in results.Rows
                            select (from c in results.VisibleColumns
                                    select ExcelExtensions.Cell(r[c])).ToRow()
                        }.ToSheetData()));

                workbookPart.Workbook.Save();
                s.Close();
            }
        }
    }
}
