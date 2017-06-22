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
using Signum.Engine.Mailing;
using Signum.Entities.Mailing;
using Signum.Engine.UserQueries;
using Signum.Entities.UserAssets;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Signum.Engine.Excel
{
    public static class ExcelLogic
    {
        public static void Start(SchemaBuilder sb, DynamicQueryManager dqm, bool excelReport)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                if (excelReport)
                {
                    QueryLogic.Start(sb, dqm);

                    sb.Include<ExcelReportEntity>()
                        .WithSave(ExcelReportOperation.Save)
                        .WithDelete(ExcelReportOperation.Delete)
                        .WithQuery(dqm, () => s => new
                        {
                            Entity = s,
                            s.Id,
                            s.Query,
                            s.File.FileName,
                            s.DisplayName,
                        });
                }
            }
        }
      

        public static List<Lite<ExcelReportEntity>> GetExcelReports(object queryName)
        {
            return (from er in Database.Query<ExcelReportEntity>()
                    where er.Query.Key == QueryUtils.GetKey(queryName)
                    select er.ToLite()).ToList();
        }

        public static async Task<byte[]> ExecuteExcelReport(Lite<ExcelReportEntity> excelReport, QueryRequest request, CancellationToken token)
        {
            ResultTable queryResult = await DynamicQueryManager.Current.ExecuteQueryAsync(request, token);

            ExcelReportEntity report = excelReport.RetrieveAndForget();
            string extension = Path.GetExtension(report.File.FileName);
            if (extension != ".xlsx")
                throw new ApplicationException(ExcelMessage.ExcelTemplateMustHaveExtensionXLSXandCurrentOneHas0.NiceToString().FormatWith(extension));

            return ExcelGenerator.WriteDataInExcelFile(queryResult, report.File.BinaryFile);
        }

        public static async Task<byte[]> ExecutePlainExcel(QueryRequest request, string title, CancellationToken token)
        {
            ResultTable queryResult = await DynamicQueryManager.Current.ExecuteQueryAsync(request, token);

            return PlainExcelGenerator.WritePlainExcel(queryResult, title);
        }
    }
}
