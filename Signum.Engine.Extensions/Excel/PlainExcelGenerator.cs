using DocumentFormat.OpenXml.Spreadsheet;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using System.IO;
using Signum.Utilities.DataStructures;
using Signum.Utilities.Reflection;
using Signum.Entities.Excel;

namespace Signum.Engine.Excel;

public static class PlainExcelGenerator
{
    public static byte[] Template { get; set; } = null!;
    public static CellBuilder CellBuilder { get; set; } = null!;

    static PlainExcelGenerator()
    {
        SetTemplate(typeof(PlainExcelGenerator).Assembly.GetManifestResourceStream("Signum.Engine.Excel.plainExcelTemplate.xlsx")!);
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

            WorkbookPart workbookPart = document.WorkbookPart!;
                                        
            WorksheetPart worksheetPart = document.GetWorksheetPartById("rId1");
            Worksheet worksheet = worksheetPart.Worksheet;

            CellBuilder = new CellBuilder()
            {
                CellFormatCount = document.WorkbookPart!.WorkbookStylesPart!.Stylesheet.CellFormats!.Count!,
                DefaultStyles = new Dictionary<DefaultStyle, UInt32Value>
                {
                    { DefaultStyle.Title, worksheet.FindCell("A1").StyleIndex! },
                    { DefaultStyle.Header, worksheet.FindCell("A2").StyleIndex! },
                    { DefaultStyle.Date, worksheet.FindCell("B3").StyleIndex! },
                    { DefaultStyle.DateTime, worksheet.FindCell("C3").StyleIndex! },
                    { DefaultStyle.Text, worksheet.FindCell("D3").StyleIndex! },
                    { DefaultStyle.General, worksheet.FindCell("E3").StyleIndex! },
                    { DefaultStyle.Boolean, worksheet.FindCell("J3").StyleIndex! },
                    { DefaultStyle.Enum, worksheet.FindCell("E3").StyleIndex! },
                    { DefaultStyle.Number, worksheet.FindCell("F3").StyleIndex! },
                    { DefaultStyle.Decimal, worksheet.FindCell("G3").StyleIndex! },
                    { DefaultStyle.Percentage, worksheet.FindCell("H3").StyleIndex! },
                    { DefaultStyle.Time, worksheet.FindCell("I3").StyleIndex! },
                }
            };
        }
    }
    
    public static byte[] WritePlainExcel(ResultTable results, string title, bool forImport = false)
    {
        using (MemoryStream ms = new MemoryStream())
        {
            WritePlainExcel(results, ms, title, forImport);
            return ms.ToArray(); 
        }
    }

    public static void WritePlainExcel(ResultTable results, string fileName, string title, bool forImport = false)
    {
        using (FileStream fs = File.Create(fileName))
            WritePlainExcel(results, fs,title, forImport);
    }

    static void WritePlainExcel(ResultTable results, Stream stream, string title, bool forImport)
    {
        stream.WriteAllBytes(Template);

        if (results == null)
            throw new ApplicationException(ExcelMessage.ThereAreNoResultsToWrite.NiceToString());

        using (SpreadsheetDocument document = SpreadsheetDocument.Open(stream, true))
        {
            document.PackageProperties.Creator = "";
            document.PackageProperties.LastModifiedBy = "";

            WorkbookPart workbookPart = document.WorkbookPart!;

            WorksheetPart worksheetPart = document.GetWorksheetPartById("rId1");

            worksheetPart.Worksheet = new Worksheet();

            worksheetPart.Worksheet.Append(new Columns(results.Columns.Select((c, i) => new DocumentFormat.OpenXml.Spreadsheet.Column()
            {
                Min = (uint)i + 1,
                Max = (uint)i + 1,
                Width = GetColumnWidth(c.Column.Type),
                BestFit = true,
                CustomWidth = true
            }).ToArray()));

            Dictionary<ResultColumn, (DefaultStyle defaultStyle, UInt32Value styleIndex)> indexes =
                results.Columns.ToDictionary(c => c, c => CellBuilder.GetDefaultStyleAndIndex(c));

            var ss = document.WorkbookPart!.WorkbookStylesPart!.Stylesheet;
            {
                var maxIndex = ss.NumberingFormats!.ChildElements.Cast<NumberingFormat>()
                    .Max(f => (uint)f.NumberFormatId!) + 1;

                var decimalCellFormat = ss.CellFormats!.ElementAt((int)(uint)CellBuilder.DefaultStyles[DefaultStyle.Decimal]);
                foreach (var kvp in CellBuilder.CustomDecimalStyles)
                {
                    var numberingFormat = new NumberingFormat
                    {
                        NumberFormatId = maxIndex++,
                        FormatCode = kvp.Key
                    };
                    ss.NumberingFormats.AppendChild(numberingFormat);
                    var cellFormat = (CellFormat)decimalCellFormat.CloneNode(false);
                    cellFormat.NumberFormatId = numberingFormat.NumberFormatId;
                    ss.CellFormats!.AppendChild(cellFormat);
                    ss.CellFormats.Count = (uint)ss.CellFormats.ChildElements.Count;
                    if (ss.CellFormats.Count != kvp.Value + 1)
                    {
                        throw new InvalidOperationException("Unexpected CellFormats count");
                    }
                }
            }


            worksheetPart.Worksheet.Append(new Sequence<Row>()
            {
                new [] { CellBuilder.Cell(title, DefaultStyle.Title, forImport) }.ToRow(),

                (from c in results.Columns
                select CellBuilder.Cell(c.Column.DisplayName, DefaultStyle.Header, forImport)).ToRow(),

                from r in results.Rows
                select (from c in results.Columns
                        let t = indexes.GetOrThrow(c)
                        select CellBuilder.Cell(r[c], t.defaultStyle, t.styleIndex, forImport)).ToRow()
            }.ToSheetDataWithIndexes());

            workbookPart.Workbook.Save();
            document.Close();
        }
    }

    public static byte[] WritePlainExcel<T>(IEnumerable<T> results, string? title = null)
    {
        using (MemoryStream ms = new MemoryStream())
        {
            WritePlainExcel(results, ms, title);
            return ms.ToArray();
        }
    }

    public static void WritePlainExcel<T>(IEnumerable<T> results, string fileName, string? title = null, bool forImport = false)
    {
        using (FileStream fs = File.Create(fileName))
            WritePlainExcel(results, fs, title);
    }

    public static void WritePlainExcel<T>(IEnumerable<T> results, Stream stream, string? title = null, bool forImport = false)
    {
        stream.WriteAllBytes(Template);

        if (results == null)
            throw new ApplicationException(ExcelMessage.ThereAreNoResultsToWrite.NiceToString());
        
        var members = MemberEntryFactory.GenerateList<T>(MemberOptions.Fields | MemberOptions.Properties | MemberOptions.Getter);
        var formats = members.ToDictionary(a => a.Name, a => a.MemberInfo.GetCustomAttribute<FormatAttribute>()?.Format);

        using (SpreadsheetDocument document = SpreadsheetDocument.Open(stream, true))
        {
            document.PackageProperties.Creator = "";
            document.PackageProperties.LastModifiedBy = "";

            WorkbookPart workbookPart = document.WorkbookPart!;

            WorksheetPart worksheetPart = document.GetWorksheetPartById("rId1");

            worksheetPart.Worksheet = new Worksheet();

            worksheetPart.Worksheet.Append(new Columns(members.Select((c, i) => new DocumentFormat.OpenXml.Spreadsheet.Column()
            {
                Min = (uint)i + 1,
                Max = (uint)i + 1,
                Width = GetColumnWidth(c.MemberInfo.ReturningType()),
                BestFit = true,
                CustomWidth = true
            }).ToArray()));

            if (title.HasText())
                worksheetPart.Worksheet.Append(new[] { CellBuilder.Cell(title, DefaultStyle.Title, forImport) }.ToRow());

            worksheetPart.Worksheet.Append(new Sequence<Row>()
            {
                (from c in members
                select CellBuilder.Cell(c.Name, DefaultStyle.Header, forImport)).ToRow(),

                from r in results
                select (from c in members
                        let template = formats.TryGetCN(c.Name) == "d" ? DefaultStyle.Date : CellBuilder.GetDefaultStyle(c.MemberInfo.ReturningType())
                        select CellBuilder.Cell(c.Getter!(r), template, forImport)).ToRow()
            }.ToSheetDataWithIndexes());

            workbookPart.Workbook.Save();
            document.Close();
        }
    }

    public static double GetColumnWidth(Type type)
    { 
        type = type.UnNullify();

        if (type == typeof(DateTime))
            return 20;
        if (type == typeof(DateOnly))
            return 15;
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
            WorkbookPart workbookPart = document.WorkbookPart!;

            WorksheetPart worksheetPart = document.GetWorksheetPartById("rId1");

            var data = worksheetPart.Worksheet.Descendants<SheetData>().Single();

            return data.Descendants<Row>().Skip(1).Select(r => selector(r.Descendants<Cell>().Select(c => document.GetCellValue(c)!).ToArray())).ToList();
        }
    }
}
