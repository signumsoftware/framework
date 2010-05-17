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
    public static class CompositeExcelGenerator
    {
        public static byte[] WriteCompositeExcel(ResultTable results)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                WriteCompositeExcel(results, ms);
                return ms.ToArray(); 
            }
        }

        public static void WriteCompositeExcel(ResultTable results, string fileName)
        {
            using (FileStream fs = File.Create(fileName))
                WriteCompositeExcel(results, fs);
        }

        static void WriteCompositeExcel(ResultTable results, Stream stream)
        {
            typeof(PlainExcelGenerator).Assembly.GetManifestResourceStream("Signum.Entities.Extensions.Reports.Generator.plainExcelTemplate.xlsx").CopyTo(stream); 

            if (results == null)
                throw new ApplicationException(Resources.ThereAreNoResultsToWrite);

            using (SpreadsheetDocument document = SpreadsheetDocument.Open(stream, true))
            {
                document.PackageProperties.Creator = "";
                document.PackageProperties.LastModifiedBy = "";

                WorkbookPart workbookPart = document.WorkbookPart;

                WorksheetPart worksheetPart = document.GetWorksheetPartByName(Resources.Data);
                Worksheet worksheet = worksheetPart.Worksheet;

                CellBuilder cb = new CellBuilder();
                
                UInt32Value headerStyleIndex = worksheet.FindCell("A1").StyleIndex;

                int columnsCount = document.CountNonEmptyColumns(worksheetPart);

                Dictionary<int, ColumnData> templateColumnData = document.GetTemplateColumnData(worksheetPart, results);

                document.AddNewColumns(worksheetPart, results, templateColumnData);

                foreach (var r in results.Rows)
                    foreach(var c in results.VisibleColumns)
                    {
                        ColumnData templateColumn = templateColumnData[c.Index];
                        Cell cell = worksheet.FindCell(templateColumn.TemplateColumnAddress + r.Index);
                        
                        //Set cell value

                        cell.StyleIndex = templateColumn.StyleIndex;
                    }

                document.ClearNonEmptyExtraRows(worksheetPart, results.Rows.Count());

                workbookPart.Workbook.Save();
                document.Close();
            }
        }

        private static int CountNonEmptyColumns(this SpreadsheetDocument document, WorksheetPart worksheetPart)
        {
            return 0;
        }

        private static void ClearNonEmptyExtraRows(this SpreadsheetDocument document, WorksheetPart worksheetPart, int usedRows)
        {

        }

        private static void AddNewColumns(this SpreadsheetDocument document, WorksheetPart worksheetPart, ResultTable results, Dictionary<int, ColumnData> templateColumnData)
        {
            //Add headers

            //Update ColumnAddress in dictionary
        }

        private static Dictionary<int, ColumnData> GetTemplateColumnData(this SpreadsheetDocument document, WorksheetPart worksheetPart, ResultTable results)
        {
            Dictionary<int, ColumnData> templateColumnData = null;

            Row headerRow = worksheetPart.FindRow("1");
            foreach (var c in results.VisibleColumns)
            {
                string cellAddress;
                bool found = headerRow.FindCellByContent(c.DisplayName, out cellAddress);
                templateColumnData.Add(c.Index, new ColumnData
                {
                    TemplateColumnAddress = cellAddress,
                    IsNew = !found,
                    StyleIndex = found ? worksheetPart.Worksheet.FindCell(cellAddress + "2").XLGetCellStyleId(document) : (uint)0
                });
            }

            return templateColumnData;
        }

        private static Row FindRow(this WorksheetPart worksheetPart, string rowAddress)
        {
            return null;
        }        

        private static bool FindCellByContent(this Row row, string content, out string cellAddress)
        {
            cellAddress = "";
            return false;
        }

        public class ColumnData
        {
            /// <summary>
            /// Column Address of the column in the template excel
            /// </summary>
            public string TemplateColumnAddress { get; set; }

            /// <summary>
            /// Indicates the column is not presen in the template excel
            /// </summary>
            public bool IsNew { get; set; }

            /// <summary>
            /// Style index of the column in the template excel
            /// </summary>
            public uint StyleIndex { get; set; }
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
