using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities.Excel;
using Signum.Engine.DynamicQuery;
using Signum.Entities;
using Signum.Engine.Maps;
using Signum.Engine.Linq;
using Signum.Entities.DynamicQuery;
using Signum.Engine.Basics;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using DocumentFormat.OpenXml;
using Signum.Utilities;
using System.IO;
using Signum.Engine.Operations;

namespace Signum.Engine.Excel
{
    public static class ExcelLogic
    {
        public static void Start(SchemaBuilder sb, DynamicQueryManager dqm, bool excelReport)
        {
            if (excelReport)
            {
                QueryLogic.Start(sb);

                sb.Include<ExcelReportDN>();
                dqm.RegisterQuery(typeof(ExcelReportDN), () =>
                    from s in Database.Query<ExcelReportDN>()
                    select new
                    {
                        Entity = s,
                        s.Id,
                        s.Query,
                        s.File.FileName,
                        s.DisplayName,
                    });

                new Graph<ExcelReportDN>.Execute(ExcelReportOperation.Save)
                {
                    AllowsNew = true,
                    Lite = false,
                    Execute = (er, _) => { }
                }.Register();

                new Graph<ExcelReportDN>.Delete(ExcelReportOperation.Delete)
                {
                    Lite = true,
                    Delete = (er, _) => { er.Delete(); }
                }.Register();
            }
        }

        public static List<Lite<ExcelReportDN>> GetExcelReports(object queryName)
        {
            return (from er in Database.Query<ExcelReportDN>()
                    where er.Query.Key == QueryUtils.GetQueryUniqueKey(queryName)
                    select er.ToLite()).ToList();
        }

        public static byte[] ExecuteExcelReport(Lite<ExcelReportDN> excelReport, QueryRequest request)
        {
            ResultTable queryResult = DynamicQueryManager.Current.ExecuteQuery(request);

            ExcelReportDN report = excelReport.RetrieveAndForget();
            string extension = Path.GetExtension(report.File.FileName);
            if (extension != ".xlsx")
                throw new ApplicationException(ExcelMessage.ExcelTemplateMustHaveExtensionXLSXandCurrentOneHas0.NiceToString().Formato(extension));

            return ExcelGenerator.WriteDataInExcelFile(queryResult, report.File.BinaryFile);
        }

        public static byte[] ExecutePlainExcel(QueryRequest request)
        {
            ResultTable queryResult = DynamicQueryManager.Current.ExecuteQuery(request);

            return PlainExcelGenerator.WritePlainExcel(queryResult);
        }
    }
}
