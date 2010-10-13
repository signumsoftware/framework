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
using Signum.Engine;
using Signum.Entities.Reports;
using Signum.Entities;
using Signum.Engine.Extensions.Properties;
using Signum.Engine.DynamicQuery;
#endregion

namespace Signum.Engine.Reports
{
    public static class ExcelGenerator
    {
        public static byte[] WriteDataInExcelFile(ResultTable queryResult, byte[] template)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                ms.WriteAllBytes(template);
                ms.Seek(0, SeekOrigin.Begin);

                ExcelGenerator.WriteDataInExcelFile(queryResult, ms);

                return ms.ToArray();
            }
        }

        public static void WriteDataInExcelFile(ResultTable results, string fileName)
        {
            using (FileStream fs = File.Open(fileName, FileMode.Open, FileAccess.ReadWrite))
                WriteDataInExcelFile(results, fs);
        }

        public static void WriteDataInExcelFile(ResultTable results, Stream stream)
        {
            if (results == null)
                throw new ApplicationException(Resources.ThereAreNoResultsToWrite);

            using (SpreadsheetDocument document = SpreadsheetDocument.Open(stream, true))
            {
                document.PackageProperties.Creator = "";
                document.PackageProperties.LastModifiedBy = "";

                WorkbookPart workbookPart = document.WorkbookPart;

                WorksheetPart worksheetPart = document.GetWorksheetPartByName(Resources.Data);
                
                CellBuilder cb = PlainExcelGenerator.CellBuilder;
                
                SheetData sheetData = worksheetPart.Worksheet.Descendants<SheetData>().Single();

                List<ColumnData> columnEquivalences = GetColumnsEquivalences(document, sheetData, results);

                UInt32Value headerStyleIndex = worksheetPart.Worksheet.FindCell("A1").StyleIndex;

                //Clear sheetData from the template sample data
                sheetData.InnerXml = "";

                sheetData.Append(new Sequence<Row>()
                {
                    (from columnData in columnEquivalences
                        select cb.Cell(columnData.Column.Column.DisplayName, headerStyleIndex)).ToRow(),

                    from r in results.Rows
                        select (from columnData in columnEquivalences
                                select cb.Cell(r[columnData.Column], cb.GetTemplateCell(columnData.Column.Column.Type), columnData.StyleIndex)).ToRow()
                }.Cast<OpenXmlElement>());

                var pivotTableParts = workbookPart.PivotTableCacheDefinitionParts
                    .Where(ptpart => ptpart.PivotCacheDefinition.Descendants<WorksheetSource>()
                                                                .Any(wss => wss.Sheet.Value == Resources.Data));

                foreach (PivotTableCacheDefinitionPart ptpart in pivotTableParts)
                {
                    PivotCacheDefinition pcd = ptpart.PivotCacheDefinition;
                    WorksheetSource wss = pcd.Descendants<WorksheetSource>().First();
                    wss.Reference.Value = "A1:" + GetExcelColumn(columnEquivalences.Count(ce => !ce.IsNew) - 1) + (results.Rows.Count() + 1).ToString();
                    
                    pcd.RefreshOnLoad = true;
                    pcd.SaveData = false;
                    pcd.Save();
                }

                workbookPart.Workbook.Save();
                document.Close();
            }
        }

        private static List<ColumnData> GetColumnsEquivalences(this SpreadsheetDocument document, SheetData sheetData, ResultTable results)
        {
            var resultsCols = results.Columns.ToDictionary(c => c.Column.DisplayName);

            var headerCells = sheetData.Descendants<Row>().First().Descendants<Cell>().ToList();
            var templateCols = headerCells.ToDictionary(c => document.GetCellValue(c));

            var firstDataRowCells = sheetData.Descendants<Row>().First(r => r.RowIndex == 2).Descendants<Cell>().ToList();

            var dic = templateCols.OuterJoinDictionaryCC(resultsCols, (name, cell, resultCol) =>
            {
                if (resultCol == null)
                    throw new ApplicationException(Resources.TheExcelTemplateHasAColumn0NotPresentInTheFindWindow.Formato(name));
                
                if (cell != null)
                {
                    return new ColumnData
                    {
                        IsNew = false,
                        StyleIndex = firstDataRowCells[headerCells.IndexOf(cell)].StyleIndex,
                        Column = resultCol,
                    };
                }
                else
                {
                    CellBuilder cb = PlainExcelGenerator.CellBuilder;
                    return new ColumnData
                    {
                        IsNew = true,
                        StyleIndex = 0, //cb.DefaultStyles[resultCol.Format == "d" ? TemplateCells.Date : cb.GetTemplateCell(resultCol.Type)],
                        Column = resultCol,
                    };
                }
            });

            return dic.Values.ToList();
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
            /// Column Data
            /// </summary>
            public Signum.Entities.DynamicQuery.ResultColumn Column { get; set; }

            /// <summary>
            /// Indicates the column is not present in the template excel
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
