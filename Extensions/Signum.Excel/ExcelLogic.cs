using Signum.API;
using Signum.Authorization.Rules;
using System.IO;

namespace Signum.Excel;

public static class ExcelLogic
{
    public static void Start(SchemaBuilder sb, bool excelReport)
    {
        if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
        {
            PermissionLogic.RegisterTypes(typeof(ExcelPermission));
            
            if (excelReport)
            {
                QueryLogic.Start(sb);

                sb.Include<ExcelReportEntity>()
                    .WithSave(ExcelReportOperation.Save)
                    .WithDelete(ExcelReportOperation.Delete)
                    .WithQuery(() => s => new
                    {
                        Entity = s,
                        s.Id,
                        s.Query,
                        s.File.FileName,
                        s.DisplayName,
                    });
            }

            if (sb.WebServerBuilder != null)
                ExcelServer.Start(sb.WebServerBuilder);
        }
    }
  

    public static List<Lite<ExcelReportEntity>> GetExcelReports(object queryName)
    {
        return (from er in Database.Query<ExcelReportEntity>()
                where er.Query.Key == QueryUtils.GetKey(queryName)
                select er.ToLite()).ToList();
    }

    public static async Task<byte[]> ExecuteExcelReportAsync(Lite<ExcelReportEntity> excelReport, QueryRequest request, CancellationToken token)
    {
        ResultTable resultTable = await QueryLogic.Queries.ExecuteQueryAsync(request, token);

        ExcelReportEntity report = excelReport.Retrieve();
        AsserExtension(report);

        return ExcelGenerator.WriteDataInExcelFile(resultTable, request, report.File.BinaryFile);
    }

    private static void AsserExtension(ExcelReportEntity report)
    {
        string extension = Path.GetExtension(report.File.FileName);
        if (extension != ".xlsx")
            throw new ApplicationException(ExcelMessage.ExcelTemplateMustHaveExtensionXLSXandCurrentOneHas0.NiceToString().FormatWith(extension));
    }

    public static byte[] ExecuteExcelReport(Lite<ExcelReportEntity> excelReport, QueryRequest request)
    {
        ResultTable queryResult = QueryLogic.Queries.ExecuteQuery(request);

        ExcelReportEntity report = excelReport.Retrieve();
        AsserExtension(report);

        return ExcelGenerator.WriteDataInExcelFile(queryResult, request, report.File.BinaryFile);
    }

    public static async Task<byte[]> ExecutePlainExcelAsync(QueryRequest request, string title, CancellationToken token)
    {
        ResultTable resultTable = await QueryLogic.Queries.ExecuteQueryAsync(request, token);

        return PlainExcelGenerator.WritePlainExcel(resultTable, request, title);
    }

    public static byte[] ExecutePlainExcel(QueryRequest request, string title)
    {
        ResultTable resultTable = QueryLogic.Queries.ExecuteQuery(request);

        return PlainExcelGenerator.WritePlainExcel(resultTable, request, title);
    }
}
