using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities.Reports;
using Signum.Engine.DynamicQuery;
using Signum.Entities;
using Signum.Engine.Maps;
using Signum.Engine.Linq;
using Signum.Entities.DynamicQuery;
using Signum.Engine.Extensions.Properties;
using Signum.Engine.Basics;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using DocumentFormat.OpenXml;

namespace Signum.Engine.Reports
{
    public static class ReportsLogic
    {
        public static void Start(SchemaBuilder sb, DynamicQueryManager dqm, bool excelReport, bool compositeReport)
        {
            if (excelReport)
            {
                QueryLogic.Start(sb);

                sb.Include<ExcelReportDN>();
                dqm[typeof(ExcelReportDN)] = (from s in Database.Query<ExcelReportDN>()
                                              select new
                                              {
                                                  Entity = s.ToLite(),
                                                  s.Id,
                                                  Query = s.Query.ToLite(),
                                                  s.File.FileName,
                                                  s.DisplayName,
                                                  s.Deleted,
                                              }).ToDynamic();
                if (compositeReport)
                {
                    sb.Include<CompositeReportDN>();
                    dqm[typeof(CompositeReportDN)] = (from e in Database.Query<CompositeReportDN>()
                                                      select new
                                                      {
                                                          Entity = e.ToLite(),
                                                          e.Id,
                                                          Nombre = e.Name,
                                                          Reports = e.ExcelReports.Count(),
                                                      }).ToDynamic();
                }

            }
            else if (compositeReport)
                throw new InvalidOperationException(Resources.ExcelReportArgumentIsNecessaryForCompositeReports);
        }


        public static List<Lite<ExcelReportDN>> GetExcelReports(object queryName)
        {
            return (from er in Database.Query<ExcelReportDN>()
                    where er.Query.Key == QueryUtils.GetQueryName(queryName)
                    select er.ToLite()).ToList();
        }


        //public static void BuildWorkbook(string fileName, ResultTable queryResult)
        //{
        //    try
        //    {
        //        using (SpreadsheetDocument s = SpreadsheetDocument.Create(fileName, SpreadsheetDocumentType.Workbook))
        //        {
        //            WorkbookPart workbookPart = s.AddWorkbookPart();
        //            workbookPart.Workbook = new Workbook
        //            {
        //                FileVersion = new FileVersion { ApplicationName = "Microsoft Office Excel" },
        //                Sheets = new Sheets()
        //            };

        //            workbookPart.AddWorksheet("Hoja 1", new Worksheet(queryResult.Rows.Select (r => new Row(r.Table.Rows.Select(c =>  Cell((string)r[c.Index])).ToArray())).ToSheetData())); 

        //            workbookPart.Workbook.Save();
        //            s.Close();
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine(ex.ToString());
        //        Console.ReadLine();
        //    }
        //}


        //public static Cell Cell(int value)
        //{
        //    return new Cell(new CellValue { Text = value.ToString() });
        //}

        //public static Cell Cell(string value)
        //{
        //    return new Cell(new InlineString(new Text { Text = value })) { DataType = CellValues.InlineString };
        //}

        //public static SheetData ToSheetData(this IEnumerable<Row> rows)
        //{
        //    return new SheetData(rows.Cast<OpenXmlElement>());
        //}

        //public static WorksheetPart AddWorksheet(this WorkbookPart workbookPart, string name, Worksheet sheet)
        //{
        //    var result = workbookPart.AddNewPart<WorksheetPart>();
        //    result.Worksheet = sheet;
        //    sheet.Save();

        //    workbookPart.Workbook.Sheets.Append(
        //        new Sheet
        //        {
        //            Name = name,
        //            SheetId = (uint)workbookPart.Workbook.Sheets.Count() + 1,
        //            Id = workbookPart.GetIdOfPart(result)
        //        });

        //    return result;
        //}

    }
}
