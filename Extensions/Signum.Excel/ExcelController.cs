using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using System.IO;
using System.Text.Json;
using Signum.API.Json;
using Signum.API.Filters;
using Signum.API;
using Signum.API.Controllers;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Http;

namespace Signum.Excel;

[ValidateModelFilter]
public class ExcelController : ControllerBase
{
    [HttpPost("api/excel/plain/{queryKey}"), ProfilerActionSplitter("queryKey")]
    public async Task<FileStreamResult> ToPlainExcel(string queryKey, [Required, FromBody]QueryRequestTS request, [FromQuery]bool forImport, CancellationToken token)
    {
        ExcelPermission.PlainExcel.AssertAuthorized();

        var queryRequest = request.ToQueryRequest(queryKey, SignumServer.JsonSerializerOptions, this.HttpContext.Request.Headers.Referer);

        ResultTable resultTable = await QueryLogic.Queries.ExecuteQueryAsync(queryRequest, token);
        byte[] binaryFile = PlainExcelGenerator.WritePlainExcel(resultTable, queryRequest, QueryUtils.GetNiceName(queryRequest.QueryName), forImport: forImport);

        var fileName = request.queryKey + Clock.Now.ToString("yyyyMMdd-HHmmss") + ".xlsx";

        return MimeMapping.GetFileStreamResult(new MemoryStream(binaryFile), fileName);
    }

    [HttpGet("api/excel/reportsFor/{queryKey}"), ProfilerActionSplitter("queryKey")]
    public IEnumerable<Lite<ExcelReportEntity>> GetExcelReports(string queryKey)
    {
        return ExcelLogic.GetExcelReports(QueryLogic.ToQueryName(queryKey));
    }

    [HttpPost("api/excel/excelReport/{queryKey}"), ProfilerActionSplitter("queryKey")]
    public FileStreamResult GenerateExcelReport(string queryKey, [Required, FromBody]ExcelReportRequest request)
    {
        byte[] file = ExcelLogic.ExecuteExcelReport(request.excelReport, request.queryRequest.ToQueryRequest(queryKey, SignumServer.JsonSerializerOptions, this.HttpContext.Request.Headers.Referer));

        var fileName = request.excelReport.ToString() + "-" + Clock.Now.ToString("yyyyMMdd-HHmmss") + ".xlsx";

        return MimeMapping.GetFileStreamResult(new MemoryStream(file),  fileName);
    }

    public class ExcelReportRequest
    {
        public QueryRequestTS queryRequest;
        public Lite<ExcelReportEntity> excelReport;
    }

    [HttpPost("api/excel/validateForImport/{queryKey}"), ProfilerActionSplitter("queryKey")]
    public QueryTokenTS? ValidateForImport(string queryKey, [Required, FromBody] QueryRequestTS queryRequest)
    {
        ExcelPermission.ImportFromExcel.AssertAuthorized();

        var result = ImporterFromExcel.ParseQueryRequest(queryRequest.ToQueryRequest(queryKey, SignumServer.JsonSerializerOptions, this.HttpContext.Request.Headers.Referer));

        return result.ElementTopToken == null ? null : new QueryTokenTS(result.ElementTopToken, true);
    }

    [HttpPost("api/excel/import/{queryKey}"), ProfilerActionSplitter("queryKey")]
    public async Task ImportFromExcel(string queryKey, [Required, FromBody] ImportFromExcelRequest request)
    {
        HttpContext.Features.Get<IHttpResponseBodyFeature>()?.DisableBuffering();

        ExcelPermission.ImportFromExcel.AssertAuthorized();

        var qr = request.QueryRequest.ToQueryRequest(queryKey, SignumServer.JsonSerializerOptions, this.HttpContext.Request.Headers.Referer);

        Type mainType = TypeLogic.GetType(request.ImportModel.TypeName);

        // The client reads this response with jsonObjectStream, which parses ONE JSON object PER LINE, so the
        // rows have to be written as NDJSON. Returning IAsyncEnumerable let MVC serialize them as a single
        // JSON array with the global WriteIndented = true (SignumServer), which spreads every object over
        // many lines: the client then parsed nothing, reported zero results, and a failed import looked
        // exactly like a successful one — no errors listed, no rows marked. Same fix as ForeachNDJson in
        // OperationController, which serializes its progress stream compactly for this very reason.
        var options = new JsonSerializerOptions
        {
            WriteIndented = false,
            IncludeFields = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };
        options.Converters.AddRange(SignumServer.JsonSerializerOptions.Converters);

        Response.ContentType = "application/x-ndjson";

        await foreach (var result in ImporterFromExcel.ImportExcel(qr, request.ImportModel, request.GetOperationSymbol(mainType)))
        {
            var json = JsonSerializer.Serialize(result, options);

            await Response.WriteAsync(json + "\n");
            await Response.Body.FlushAsync();
        }
    }
}

public class ImportFromExcelRequest
{
    public ImportExcelModel ImportModel { get; set; }
    public QueryRequestTS QueryRequest { get; set; }

    public OperationSymbol GetOperationSymbol(Type entityType) => ParseOperationAssert(this.ImportModel.OperationKey, entityType);

    public static OperationSymbol ParseOperationAssert(string operationKey, Type entityType)
    {
        var symbol = SymbolLogic<OperationSymbol>.ToSymbol(operationKey);

        OperationLogic.AssertOperationAllowed(symbol, entityType, inUserInterface: true, null);

        return symbol;
    }
}
