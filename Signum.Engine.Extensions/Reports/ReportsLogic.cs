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
using Signum.Utilities;
using System.IO;

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
                throw new InvalidOperationException("excelReport is necessary for compositeReport");
        }

        public static List<Lite<ExcelReportDN>> GetExcelReports(object queryName)
        {
            return (from er in Database.Query<ExcelReportDN>()
                    where er.Query.Key == QueryUtils.GetQueryUniqueKey(queryName) && !er.Deleted
                    select er.ToLite()).ToList();
        }

        public static byte[] ExecuteExcelReport(Lite<ExcelReportDN> excelReport, QueryRequest request)
        {
            ResultTable queryResult = DynamicQueryManager.Current.ExecuteQuery(request);
            
            ExcelReportDN report = excelReport.RetrieveAndForget();
            string extension = Path.GetExtension(report.File.FileName);
            if (extension != ".xlsx")
                throw new ApplicationException(Resources.ExcelTemplateMustHaveExtensionXLSXandCurrentOneHas0.Formato(extension));

            return ExcelGenerator.WriteDataInExcelFile(queryResult, report.File.BinaryFile);
        }

        public static byte[] ExecutePlainExcel(QueryRequest request)
        {
            ResultTable queryResult = DynamicQueryManager.Current.ExecuteQuery(request);

            return PlainExcelGenerator.WritePlainExcel(queryResult);
        }
    }
}
