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
    public static class ExcelGenerator
    {
        public static byte[] WriteDataInExcelFile(ResultTable results)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                WriteDataInExcelFile(results, ms);
                return ms.ToArray(); 
            }
        }

        public static void WriteDataInExcelFile(ResultTable results, string fileName)
        {
            using (FileStream fs = File.Open(fileName, FileMode.Open, FileAccess.ReadWrite))
                WriteDataInExcelFile(results, fs);
        }

        static void WriteDataInExcelFile(ResultTable results, Stream stream)
        {
            //typeof(PlainExcelGenerator).Assembly.GetManifestResourceStream("Signum.Entities.Extensions.Reports.Generator.plainExcelTemplate.xlsx").CopyTo(stream); 

            if (results == null)
                throw new ApplicationException(Resources.ThereAreNoResultsToWrite);

            using (SpreadsheetDocument document = SpreadsheetDocument.Open(stream, true))
            {
                document.PackageProperties.Creator = "";
                document.PackageProperties.LastModifiedBy = "";

                WorkbookPart workbookPart = document.WorkbookPart;

                WorksheetPart worksheetPart = document.GetWorksheetPartByName(Resources.Data);
                
                CellBuilder cb = new CellBuilder();
                
                //Dictionary<ColumnAddress, Tuple<ResultTableColumnIndex, ColumnData>>
                Dictionary<string, Tuple<int, ColumnData>> templateColumnInfo = GetTemplateColumnsResultsTableEquivalent(document, worksheetPart, results);

                UInt32Value headerStyleIndex = worksheetPart.Worksheet.FindCell("A1").StyleIndex;

                //Clear sheetData from the template sample data
                worksheetPart.Worksheet.Descendants<SheetData>().FirstOrDefault().InnerXml = "";

                worksheetPart.Worksheet.Descendants<SheetData>().FirstOrDefault().Append(new Sequence<Row>()
                {
                    (from c in results.VisibleColumns
                        select cb.Cell(c.DisplayName, headerStyleIndex)).ToRow(),

                    from r in results.Rows
                        select (from kvp in templateColumnInfo
                                select cb.Cell(r[kvp.Value.First], kvp.Value.Second.StyleIndex)).ToRow()
                }.Cast<OpenXmlElement>());

                var pivotTableParts = workbookPart.PivotTableCacheDefinitionParts
                    .Where(ptpart => ptpart.PivotCacheDefinition.Descendants<WorksheetSource>()
                                                                .Any(wss => wss.Sheet.Value == Resources.Data));

                foreach (PivotTableCacheDefinitionPart ptpart in pivotTableParts)
                {
                    PivotCacheDefinition pcd = ptpart.PivotCacheDefinition;
                    WorksheetSource wss = pcd.Descendants<WorksheetSource>().First();
                    wss.Reference.Value = "A1:" + GetExcelColumn(results.VisibleColumns.Count() - 1) + (results.Rows.Count() + 1).ToString();
                    //foreach (CacheField cf in pcd.CacheFields.Descendants<CacheField>())
                    //    cf.InnerXml = "";
                    //ptpart.PivotCacheDefinition = pcd;
                    pcd.RefreshOnLoad = true;
                    pcd.SaveData = false;
                    pcd.Save();
                }

                workbookPart.Workbook.Save();
                document.Close();
            }
        }

        private static Dictionary<string, Tuple<int, ColumnData>> GetTemplateColumnsResultsTableEquivalent(SpreadsheetDocument document, WorksheetPart worksheetPart, ResultTable results)
        {
            Dictionary<int, ColumnData> templateColumnData = document.GetTemplateColumnData(worksheetPart, results);

            //Find Window can have more columns than the Excel template, and they will be appended
            AddNewColumnsAtTheEnd(results, templateColumnData);

            Dictionary<string, Tuple<int, ColumnData>> templateColumnInfo = templateColumnData.ToDictionary(kvp => kvp.Value.TemplateColumnAddress, kvp => new Tuple<int, ColumnData>(kvp.Key, kvp.Value));

            //Template cannot have more columns than the Find Window
            CheckAllTemplateColumnsAreInResultTable(document, worksheetPart, templateColumnInfo, results);

            return templateColumnInfo;
        }

        private static Dictionary<int, ColumnData> GetTemplateColumnData(this SpreadsheetDocument document, WorksheetPart worksheetPart, ResultTable results)
        {
            Dictionary<int, ColumnData> templateColumnData = new Dictionary<int,ColumnData>();
                            
            foreach (var c in results.VisibleColumns)
            {
                string columnAddress;
                bool found = document.FindColumnByHeaderContent(worksheetPart, c.DisplayName, out columnAddress);
                templateColumnData.Add(c.Index, new ColumnData
                {
                    TemplateColumnAddress = columnAddress,
                    IsNew = !found,
                    StyleIndex = found ? worksheetPart.Worksheet.FindCell(columnAddress + "2").StyleIndex : (UInt32Value)0
                });
            }

            return templateColumnData;
        }

        private static bool FindColumnByHeaderContent(this SpreadsheetDocument document, WorksheetPart worksheetPart, string content, out string columnAddress)
        {
            bool found = false;
            columnAddress = "";
            string cellContent = "not found";
            int i = 0;
            while (!found && cellContent.HasText())
            {
                columnAddress = GetExcelColumn(i);
                cellContent = document.GetCellValue(worksheetPart.Worksheet, columnAddress + "1");
                found = cellContent == content;
                i++;
            }
            return found;
        }

        private static void AddNewColumnsAtTheEnd(ResultTable results, Dictionary<int, ColumnData> templateColumnData)
        {
            int templateColumnsCount = templateColumnData.Count(kvp => !kvp.Value.TemplateColumnAddress.HasText());
            
            foreach (var c in results.VisibleColumns.Where(c => !templateColumnData.Keys.Contains(c.Index)))
            {
                templateColumnData.Add(c.Index, new ColumnData 
                { 
                    IsNew = true, 
                    TemplateColumnAddress = GetExcelColumn(templateColumnsCount)
                });

                templateColumnsCount++;
            }
        }

        private static void CheckAllTemplateColumnsAreInResultTable(SpreadsheetDocument document, WorksheetPart worksheetPart, Dictionary<string, Tuple<int, ColumnData>> templateColumnInfo, ResultTable results)
        {
            //All the columns in the template must be in the ResultTable, otherwise the pivot tables or formulas could be broken
            string cellValue = "start";
            for (int i = 0; cellValue.HasText(); i++)
            {
                string columnAddress = GetExcelColumn(i);
                cellValue = document.GetCellValue(worksheetPart.Worksheet, columnAddress + "1");
                if (cellValue.HasText() && !templateColumnInfo.ContainsKey(columnAddress))
                    throw new ApplicationException(Resources.TheExcelTemplateHasAColumn0NotPresentInTheFindWindow.Formato(cellValue));
            }
        }

        private static string GetExcelColumn(int columnNumberBase0)
        {
            string result = "";
            int numAlphabetCharacters = 26;
            int numAlphabetRounds;
            int numAlphabetCharacter;
            numAlphabetRounds = Math.DivRem(columnNumberBase0, numAlphabetCharacters, out numAlphabetCharacter);

            if (numAlphabetRounds > 0)
                result = ((char)('A' + (char)(numAlphabetRounds - 1))).ToString();

            result = result + ((char)('A' + (char)numAlphabetCharacter)).ToString();
            return result;
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
            public UInt32Value StyleIndex { get; set; }
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
