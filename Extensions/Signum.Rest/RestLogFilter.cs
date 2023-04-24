using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using System.IO;

namespace Signum.Rest;

public class RestLogFilter : ActionFilterAttribute
{
    const string OriginalResponseStreamKey = "ORIGINAL_RESPONSE_STREAM";

    public RestLogFilter(bool allowReplay)
    {
        AllowReplay = allowReplay;
    }

    public bool AllowReplay { get; set; }

    public bool IgnoreRequestBody { get; set; }
    public bool IgnoreResponseBody{ get; set; }

    public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        try
        {
            var request = context.HttpContext.Request;
            if (!IgnoreResponseBody)
            {
                context.HttpContext.Items[OriginalResponseStreamKey] = context.HttpContext.Response.Body;
                context.HttpContext.Response.Body = new MemoryStream();
            }

            var connection = context.HttpContext.Features.Get<IHttpConnectionFeature>()!;

            var queryParams = context.HttpContext.Request.Query
                 .Select(a => new QueryStringValueEmbedded { Key = a.Key, Value = a.Value.ToString() })
                 .ToMList();

            var restLog = new RestLogEntity
            {
                AllowReplay = this.AllowReplay,
                HttpMethod = request.Method.ToString(),
                Url = request.Path.ToString(),
                QueryString = queryParams,
                User = UserHolder.Current?.User,
                Controller = context.Controller.GetType().FullName!,
                ControllerName = context.Controller.GetType().Name,
                Action = ((ControllerActionDescriptor)context.ActionDescriptor).ActionName,
                MachineName = System.Environment.MachineName,
                ApplicationName = AppDomain.CurrentDomain.FriendlyName,
                StartDate = Clock.Now,
                UserHostAddress = connection.RemoteIpAddress!.ToString(),
                UserHostName = request.Host.Value,
                Referrer = request.Headers["Referrer"].ToString(),
                RequestBody = IgnoreRequestBody ? null : await GetRequestBody(context.HttpContext.Request)
            };

            context.HttpContext.Items.Add(typeof(RestLogEntity).FullName!, restLog);

        }
        catch (Exception e)
        {
            e.LogException();
        }

        var executedContext = await next();

        if (executedContext.Exception != null)
        {
            var restLog = (RestLogEntity)executedContext.HttpContext.Items.GetOrThrow(typeof(RestLogEntity).FullName!)!;
            restLog.EndDate = Clock.Now;
            restLog.Exception = executedContext.Exception.LogException()?.ToLite();

            if (!IgnoreResponseBody)
            {
                RestoreOriginalStream(executedContext);
            }

            using (ExecutionMode.Global())
                restLog.Save();
        }
    }


    private async Task<string> GetRequestBody(HttpRequest request)
    {
        // Allows using several time the stream in ASP.Net Core
        request.EnableBuffering();

        string result;
        // Arguments: Stream, Encoding, detect encoding, buffer size
        // AND, the most important: keep stream opened
        request.Body.Position = 0;
        using (StreamReader reader = new StreamReader(request.Body, Encoding.UTF8, true, 1024, true))
        {
            result = await reader.ReadToEndAsync();
        }

        // Rewind, so the core is not lost when it looks the body for the request
        request.Body.Position = 0;

        return result;
    }

    public override async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
    {
        var resultContext = await next();

        try
        {
            var restLog = (RestLogEntity)resultContext.HttpContext.Items.GetOrThrow(typeof(RestLogEntity).FullName!)!;
            restLog.EndDate = Clock.Now;

            if (!IgnoreResponseBody)
            {
                Stream memoryStream = await RestoreOriginalStream(resultContext);

                if (resultContext.Exception == null)
                {
                    memoryStream.Seek(0, System.IO.SeekOrigin.Begin);
                    restLog.ResponseBody = Encoding.UTF8.GetString(memoryStream.ReadAllBytes());
                }
            }

            if (resultContext.Exception != null)
            {
                restLog.Exception = resultContext.Exception.LogException()?.ToLite();
            }

            using (ExecutionMode.Global())
                restLog.Save();
        }
        catch (Exception e)
        {
            e.LogException();
        }

    }


 

    private static async Task<Stream> RestoreOriginalStream(FilterContext context)
    {
        var originalStream = (Stream)context.HttpContext.Items.GetOrThrow(OriginalResponseStreamKey)!;
        var memoryStream = context.HttpContext.Response.Body;
        memoryStream.Seek(0, System.IO.SeekOrigin.Begin);
        await memoryStream.CopyToAsync(originalStream);

        context.HttpContext.Response.Body = originalStream;
        return memoryStream;
    }
}


