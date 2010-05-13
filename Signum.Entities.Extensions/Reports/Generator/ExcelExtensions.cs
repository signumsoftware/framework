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
#endregion

namespace Signum.Entities.Reports
{
    public static class ExcelExtensions
    {
        public static string ToStringExcel(this DateTime datetime)
        {
            return (datetime.ToOADate()).ToString(CultureInfo.InvariantCulture); //Convert to Julean Format
        }

        public static string ToStringExcel(this decimal number)
        {
            return number.ToString(CultureInfo.InvariantCulture);
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
            sheet.AddNamespaceDeclaration("r", "http://schemas.openxmlformats.org/officeDocument/2006/relationships");
            
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

        public static void XLDeleteSheet(this SpreadsheetDocument document, string sheetId)
        {
            WorkbookPart wbPart = document.WorkbookPart;

            Sheet theSheet = wbPart.Workbook.Descendants<Sheet>().
              Where(s => s.Id == sheetId).FirstOrDefault();

            if (theSheet == null)
                return;

            // Remove the sheet reference from the workbook.
            WorksheetPart worksheetPart = (WorksheetPart)(wbPart.GetPartById(theSheet.Id));
            theSheet.Remove();

            // Delete the worksheet part.
            wbPart.DeletePart(worksheetPart);
        }

        public static Cell FindCell(this Worksheet worksheet, string addressName)
        {
            return worksheet.Descendants<Cell>().
              Where(c => c.CellReference == addressName).FirstOrDefault();
        }

        //public static uint XLGetCellStyleId(this SpreadsheetDocument document, string sheetId, string addressName)
        //{
        //    WorkbookPart wbPart = document.WorkbookPart;

        //    // Find the sheet with the supplied name, and then use that Sheet object
        //    // to retrieve a reference to the appropriate worksheet.
        //    WorksheetPart wsPart = GetWorksheetPart(sheetId, wbPart);
        //    Cell theCell = wsPart.Worksheet.Descendants<Cell>().
        //      Where(c => c.CellReference == addressName).FirstOrDefault();

        //    return (uint)theCell.XLGetCellStyleId(document);
        //}

        public static WorksheetPart GetWorksheetPart(this SpreadsheetDocument document, string sheetId)
        {
            WorkbookPart wbPart = document.WorkbookPart;

            Sheet theSheet = wbPart.Workbook.Descendants<Sheet>().
              Where(s => s.Id == sheetId).FirstOrDefault();

            if (theSheet == null)
                throw new ArgumentException("sheetName");

            // Retrieve a reference to the worksheet part, and then use its Worksheet property to get 
            // a reference to the cell whose address matches the address you've supplied:
            WorksheetPart wsPart = (WorksheetPart)(wbPart.GetPartById(theSheet.Id));
            return wsPart;
        }

        public static uint XLGetCellStyleId(this Cell cell, SpreadsheetDocument document)
        {
            WorkbookPart wbPart = document.WorkbookPart;

            // It the cell doesn't exist, simply return a null reference:
            if (cell == null)
                return 0;
            
            // Go get the styles information.
            var styles = wbPart.GetPartsOfType<WorkbookStylesPart>().FirstOrDefault();
            if (styles == null)
                return 0;

            if (cell.StyleIndex == null)
                return 0;

            return (uint)System.Convert.ToInt32(cell.StyleIndex.Value);
        }
    }
}
