using DocumentFormat.OpenXml.Spreadsheet;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using System.Globalization;
using System.Drawing;

namespace Signum.Excel;

public static class ExcelExtensions
{
    public static string ToExcelDate(DateTime datetime)
    {
        return datetime.ToUserInterface().ToOADate().ToString(CultureInfo.InvariantCulture); //Convert to Julean Format
    }

    public static DateTime FromExcelDate(string datetime, DateTimeKind kind)
    {
        var date = DateTime.FromOADate(double.Parse(datetime, CultureInfo.InvariantCulture));

        return date.ToKind(kind);
    }

    public static string ToExcelTime(TimeOnly timeOnly)
    {
        return timeOnly.ToTimeSpan().TotalDays.ToString(CultureInfo.InvariantCulture);
    }

    public static TimeOnly FromExcelTime(string time)
    {
        var value = double.Parse(time, CultureInfo.InvariantCulture);

        return TimeSpan.FromDays(value).ToTimeOnly();
    }


    public static string ToExcelNumber(decimal number)
    {
        return number.ToString(CultureInfo.InvariantCulture);
    }

    public static decimal FromExcelNumber(string number)
    {
        return decimal.Parse(number, NumberStyles.Number | NumberStyles.AllowExponent, CultureInfo.InvariantCulture);
    }

    public static SheetData ToSheetDataWithIndexes(this IEnumerable<Row> rows)
    {
        var rowsList = rows.ToList();

        uint rowIndex = 1;
        foreach (var r in rowsList)
        {
            r.RowIndex = rowIndex++;

            uint colIndex = 1;
            foreach (Cell c in r.ChildElements)
            {
                c.CellReference = GetExcelColumnName(colIndex++) + r.RowIndex;
            }
        }

        return new SheetData(rowsList);
    }

    public static string GetExcelColumnName(uint columnNumber)
    {
        string result = "";

        while (columnNumber > 0)
        {
            uint mod = (columnNumber - 1) % 26;
            result = Convert.ToChar('A' + mod) + result;
            columnNumber = (columnNumber - mod) / 26;
        }

        return result;
    }


    public static int? GetExcelColumnIndex(this CellType cell) => cell.CellReference == null ? null : GetExcelColumnIndex(cell.CellReference!);
    public static int GetExcelColumnIndex(string reference)
    {
        int ci = 0;
        reference = reference.ToUpper();
        for (int ix = 0; ix < reference.Length && reference[ix] >= 'A'; ix++)
            ci = (ci * 26) + ((int)reference[ix] - 64);
        return ci;
    }

    public static Row ToRow(this IEnumerable<Cell> rows)
    {
        return new Row(rows.Cast<OpenXmlElement>());
    }


    public static WorksheetPart AddWorksheet(this WorkbookPart workbookPart, string name, Worksheet sheet)
    {
        sheet.AddNamespaceDeclaration("r", "http://schemas.openxmlformats.org/officeDocument/2006/relationships");
        
        var result = workbookPart.AddNewPart<WorksheetPart>();
        result.Worksheet = sheet;
        sheet.Save();

        workbookPart.Workbook.Sheets!.Append(
            new Sheet
            {
                Name = name,
                SheetId = (uint)workbookPart.Workbook.Sheets.Count() + 1,
                Id = workbookPart.GetIdOfPart(result)
            });

        return result;
    }

    public static void XLDeleteSheet(this SpreadsheetDocument document, string sheetId)
    {
        WorkbookPart wbPart = document.WorkbookPart!;

        Sheet? theSheet = wbPart.Workbook.Descendants<Sheet>().
          Where(s => s.Id == sheetId).FirstOrDefault();

        if (theSheet == null)
            return;

        // Remove the sheet reference from the workbook.
        WorksheetPart worksheetPart = (WorksheetPart)(wbPart.GetPartById(theSheet.Id!));
        theSheet.Remove();

        // Delete the worksheet part.
        wbPart.DeletePart(worksheetPart);
    }

    public static Cell FindCell(this Worksheet worksheet, string addressName)
    {
        return worksheet.Descendants<Cell>().FirstEx(c => c.CellReference == addressName);
    }

    public static Cell FindCell(this SheetData sheetData, string addressName)
    {
        return sheetData.Descendants<Cell>().FirstEx(c => c.CellReference == addressName);
    }

    public static string? GetCellValue(this SpreadsheetDocument document, Worksheet worksheet, string addressName)
    {
        Cell? theCell = worksheet.Descendants<Cell>().
          Where(c => c.CellReference == addressName).FirstOrDefault();

        // If the cell doesn't exist, return an empty string:
        if (theCell == null)
            return null;

        return GetCellValue(document, theCell);
    }

    public static string? GetCellValue(this SpreadsheetDocument document, Row row, string columnName)
    {
        var address = columnName + row.RowIndex;

        Cell? theCell = row.Descendants<Cell>()
            .FirstOrDefault(c => c.CellReference == columnName || c.CellReference == address);

        // If the cell doesn't exist, return an empty string:
        if (theCell == null)
            return null;

        return GetCellValue(document, theCell);
    }

    public static string? GetCellValue(this SpreadsheetDocument document, Cell theCell)
    {
        var cellValue = theCell.GetFirstChild<CellValue>();

        string value = cellValue?.InnerText ?? theCell.InnerText;

        // If the cell represents an integer number, you're done. 
        // For dates, this code returns the serialized value that 
        // represents the date. The code handles strings and booleans
        // individually. For shared strings, the code looks up the corresponding
        // value in the shared string table. For booleans, the code converts 
        // the value into the words TRUE or FALSE.
        if (theCell.DataType == null)
            return value;

        if(theCell.DataType.Value == CellValues.SharedString)
        {
            // For shared strings, look up the value in the shared strings table.
            var stringTable = document.WorkbookPart!.GetPartsOfType<SharedStringTablePart>().FirstOrDefault();
            // If the shared string table is missing, something's wrong.
            // Just return the index that you found in the cell.
            // Otherwise, look up the correct text in the table.
            if (stringTable != null)
                return stringTable.SharedStringTable.ElementAt(int.Parse(value)).InnerText;
        }
        else if (theCell.DataType.Value == CellValues.Boolean)
        {
            switch (value)
            {
                case "0":
                    return "FALSE";
                default:
                    return "TRUE";
            }
        }
        return value;
    }

    public static void SetCellValue(this Cell cell, object? value, Type type)
    {
        if(type == typeof(string))
        {
            cell.RemoveAllChildren();
            cell.Append(new InlineString(new Text((string?)value!)));
            cell.DataType = CellValues.InlineString;
        }
        else
        {
            string excelValue = value == null ? "" :
                        type.UnNullify() == typeof(DateTime) ? ExcelExtensions.ToExcelDate(((DateTime)value)) :
                        type.UnNullify() == typeof(DateTime) ? ExcelExtensions.ToExcelDate(((DateTime)value)) :
                        type.UnNullify() == typeof(bool) ? (((bool)value) ? "TRUE": "FALSE") :
                        IsNumber(type.UnNullify()) ? ExcelExtensions.ToExcelNumber(Convert.ToDecimal(value)) :
                        value.ToString()!;

            cell.CellValue = new CellValue(excelValue);
        }
    }

    public static System.Drawing.Color? GetCellFillColor(this SpreadsheetDocument document, Row row, string columnName)
    {
        var address = columnName + row.RowIndex;

        Cell? cell = row.Descendants<Cell>()
            .FirstOrDefault(c => c.CellReference == columnName || c.CellReference == address);

        // If the cell doesn't exist, return an empty string:
        if (cell == null)
            return null;

        if (cell.StyleIndex == null)
            return null;

        var stylesPart = document.WorkbookPart!.WorkbookStylesPart;

        if (stylesPart == null)
            return null;

        var cellFormat = stylesPart.Stylesheet.CellFormats?.ElementAt((int)cell.StyleIndex.Value) as CellFormat;

        if (cellFormat?.FillId != null)
        {
            var fill = stylesPart.Stylesheet.Fills?.ElementAt((int)cellFormat.FillId.Value) as Fill;
            if (fill == null)
                return null;

            var patternFill = fill.PatternFill;

            if (patternFill?.ForegroundColor != null)
            {
                return GetColorFromOpenXml(patternFill.ForegroundColor, stylesPart);
            }
            else if (patternFill?.BackgroundColor != null)
            {
                return GetColorFromOpenXml(patternFill.BackgroundColor, stylesPart);
            }
        }

        return null;
    }

    static System.Drawing.Color? GetColorFromOpenXml(ColorType color, WorkbookStylesPart stylesPart)
    {
        if (color.Rgb != null)
        {
            // Color is in Rgb format
            return ColorTranslator.FromHtml("#" + color.Rgb);
        }
        else if (color.Indexed != null)
        {
            // Color is an indexed color
            var indexedColors = stylesPart.Stylesheet.Colors!.IndexedColors!;
            var rgbColor = indexedColors.ElementAt((int)color.Indexed.Value) as RgbColor;
            return ColorTranslator.FromHtml("#" + rgbColor!.Rgb);
        }

        return null;
    }

    public static bool IsNumber(Type type)
    {
        switch (Type.GetTypeCode(type))
        {
            case TypeCode.Byte:
            case TypeCode.Decimal:
            case TypeCode.Double:
            case TypeCode.Int16:
            case TypeCode.Int32:
            case TypeCode.Int64:
            case TypeCode.SByte:
            case TypeCode.Single:
            case TypeCode.UInt16:
            case TypeCode.UInt32:
            case TypeCode.UInt64:
                return true;
            default:
                return false;
        }
    }

    public static bool IsDate(Type type)
    {
        return type == typeof(DateTime) || type == typeof(DateOnly) || type == typeof(DateTimeOffset);
    }

    public static WorksheetPart? TryWorksheetPartById(this SpreadsheetDocument document, string sheetId)
    {
        WorkbookPart wbPart = document.WorkbookPart!;

        Sheet? theSheet = wbPart.Workbook.Descendants<Sheet>().
          Where(s => s.Id == sheetId).FirstOrDefault();

        if (theSheet == null)
            return null;

        // Retrieve a reference to the worksheet part, and then use its Worksheet property to get 
        // a reference to the cell whose address matches the address you've supplied:
        WorksheetPart wsPart = (WorksheetPart)(wbPart.GetPartById(theSheet.Id!));
        return wsPart;
    }

    public static WorksheetPart GetWorksheetPartById(this SpreadsheetDocument document, string sheetId)
    {
        WorkbookPart wbPart = document.WorkbookPart!;

        Sheet? theSheet = wbPart.Workbook.Descendants<Sheet>().
          Where(s => s.Id == sheetId).FirstOrDefault();

        if (theSheet == null)
            throw new ArgumentException("Sheet with id {0} not found".FormatWith(sheetId));

        // Retrieve a reference to the worksheet part, and then use its Worksheet property to get 
        // a reference to the cell whose address matches the address you've supplied:
        WorksheetPart wsPart = (WorksheetPart)(wbPart.GetPartById(theSheet.Id!));
        return wsPart;
    }

    public static WorksheetPart GetWorksheetPartBySheetName(this SpreadsheetDocument document, string sheetName)
    {
        WorkbookPart wbPart = document.WorkbookPart!;

        Sheet? sheet = wbPart.Workbook.Descendants<Sheet>().
          Where(s => s.Name == sheetName).FirstOrDefault();

        if (sheet == null)
            throw new ArgumentException("Sheet with name {0} not found.\nAvailable sheets:\n{1}".FormatWith(sheetName,
                wbPart.Workbook.Descendants<Sheet>().Select(a => a.Name).ToString("\n")));

        // Retrieve a reference to the worksheet part, and then use its Worksheet property to get 
        // a reference to the cell whose address matches the address you've supplied:
        WorksheetPart wsPart = (WorksheetPart)(wbPart.GetPartById(sheet.Id!));
        return wsPart;
    }

    public static bool IsDateCell(this Cell cell, WorkbookPart workbookPart)
    {
        if (cell.StyleIndex == null)
            return false;

        var stylesPart = workbookPart.WorkbookStylesPart;
        if (stylesPart == null)
            return false;

        var styleIndex = (int)cell.StyleIndex.Value;
        var cellFormat = stylesPart.Stylesheet.CellFormats?.ElementAt(styleIndex) as CellFormat;

        if (cellFormat == null)
            return false;

        // Excel built-in date number format IDs: 14–22, 45–47
        var dateFormatIds = new HashSet<uint> { 14, 15, 16, 17, 18, 19, 20, 21, 22, 45, 46, 47 };

        return dateFormatIds.Contains(cellFormat.NumberFormatId!.Value);
    }

    public static DateTime? GetDateCellValue(this Cell cell, WorkbookPart workbookPart)
    {
        if (cell == null || cell.CellValue == null)
            return null;

        // Excel stores dates as doubles (days since 1900-01-01)
        if (double.TryParse(cell.CellValue.InnerText, out double oaDate))
        {
            bool isDate = IsDateCell(cell, workbookPart);
            if (isDate)
            {
                // Excel uses OADate: 1.0 = Jan 1, 1900
                return DateTime.SpecifyKind(DateTime.FromOADate(oaDate), DateTimeKind.Utc);
            }
        }

        return null; // Not a valid date
    }

    static int ColumnNameToIndex(string columnName)
    {
        int index = 0;
        foreach (char c in columnName.ToUpper())
            index = index * 26 + (c - 'A' + 1);
        return index;
    }

    public static Cell? GetCell(this WorksheetPart worksheetPart, string address)
    {
        uint rowIndex = uint.Parse(new string(address.Where(char.IsDigit).ToArray()));
        var sheetData = worksheetPart.Worksheet.GetFirstChild<SheetData>()!;
        var row = sheetData.Elements<Row>().FirstOrDefault(r => r.RowIndex?.Value == rowIndex);
        if (row == null)
            return null;

        string columnName = new string(address.Where(char.IsLetter).ToArray());
        return row.GetCell(columnName);
    }

    public static Cell? GetCell(this Row row, string columnName)
    {
        int targetColIndex = ColumnNameToIndex(columnName);

        int currentColIndex = 1;
        foreach (var cell in row.Elements<Cell>())
        {
            int colIndex;
            if (cell.CellReference != null)
            {
                string cellCol = new string(cell.CellReference.Value!.Where(char.IsLetter).ToArray());
                colIndex = ColumnNameToIndex(cellCol);
            }
            else
            {
                colIndex = currentColIndex;
            }

            if (colIndex == targetColIndex)
                return cell;

            currentColIndex = colIndex + 1;
        }

        return null;
    }
}
