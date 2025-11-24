using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Net;
using System.Security.Authentication;
using System.Text.Json;

namespace Signum.API.Filters;

public class SignumExceptionFilterAttribute : IAsyncResourceFilter
{
    public static Func<Exception, bool> TranslateExceptionMessage = ex => ex is ApplicationException;

    public static Func<Exception, bool> ShouldLogException = e => e is not OperationCanceledException;

    public static Func<ResourceExecutedContext, HttpError> CustomHttpErrorFactory = rec => new HttpError(rec.Exception!);

    public static Action<ActionContext, ExceptionEntity>? ApplyMixins = null;

    public async Task OnResourceExecutionAsync(ResourceExecutingContext precontext, ResourceExecutionDelegate next)
    {
        //Eagerly reading the whole body just in case to avoid "Cannot access a disposed object" 
        //TODO: Make it more eficiently when https://github.com/aspnet/AspNetCore/issues/14396
        precontext.HttpContext.Request.EnableBuffering();

        var context = await next();

        if (context.Exception != null)
        {
            await LogException(context.Exception, context);

            if (ExpectsJsonResult(context))
            {
                var ci = TranslateExceptionMessage(context.Exception) ? SignumCultureSelectorFilter.GetCurrentCulture?.Invoke(precontext) : null;

                var response = context.HttpContext.Response;
                HttpStatusCode statusCode;
                HttpError error;
                using (ci == null ? null : CultureInfoUtils.ChangeBothCultures(ci))
                {
                    statusCode = GetStatus(context.Exception.GetType());
                    error = CustomHttpErrorFactory(context);
                } //No await inside

                response.StatusCode = (int)statusCode;
                response.ContentType = "application/json";

                var userWithClaims = (UserWithClaims?)context.HttpContext.Items[SignumAuthenticationFilter.Signum_User_Holder_Key];

                string errorJson;
                using (UserHolder.Current == null && userWithClaims != null ? UserHolder.UserSession(userWithClaims) : null)
                    errorJson = JsonSerializer.Serialize(error, SignumServer.JsonSerializerOptions);

                await response.WriteAsync(errorJson);
                context.ExceptionHandled = true;

            }
        }
    }

    internal static async Task LogException(Exception exception, ActionContext actionContext)
    {
        if (ShouldLogException(exception))
        {
            var req = actionContext.HttpContext.Request;

            var connFeature = actionContext.HttpContext.Features.Get<IHttpConnectionFeature>()!;

            byte[] body;
            try
            {
                body = await ReadAllBody(actionContext.HttpContext);
            }
            catch(Exception e)
            {
                body = Encoding.UTF8.GetBytes(e.GetType().Name + ":" + e.Message);
            }

            var exLog = exception.LogException(e =>
            {
                e.ActionName = Try(100, () => (actionContext.ActionDescriptor as ControllerActionDescriptor)?.ActionName);
                e.ControllerName = Try(100, () => (actionContext.ActionDescriptor as ControllerActionDescriptor)?.ControllerName);
                e.UserAgent = Try(300, () => req.Headers["User-Agent"].FirstOrDefault());
                e.RequestUrl = Try(int.MaxValue, () => req.GetDisplayUrl());
                e.UrlReferer = Try(int.MaxValue, () => req.Headers["Referer"].ToString());
                e.UserHostAddress = Try(100, () => connFeature.RemoteIpAddress?.ToString());
                e.UserHostName = Try(100, () => connFeature.RemoteIpAddress == null ? null : Dns.GetHostEntry(connFeature.RemoteIpAddress).HostName);
                e.User = UserHolder.Current?.User ?? ((UserWithClaims?)actionContext.HttpContext.Items[SignumAuthenticationFilter.Signum_User_Holder_Key])?.User ?? e.User;
                e.QueryString = new BigStringEmbedded(Try(int.MaxValue, () => req.QueryString.ToString()));
                e.Form = new BigStringEmbedded(Try(int.MaxValue, () => Encoding.UTF8.GetString(body)));
                e.Session = new BigStringEmbedded();
                ApplyMixins?.Invoke(actionContext, e);
            });
        }
    }

    private static string? Try(int size, Func<string?> getValue)
    {
        try
        {
            return getValue()?.TryStart(size);
        }
        catch (Exception e)
        {
            return (e.GetType().Name + ":" + e.Message).TryStart(size);
        }
    }

    public static Func<ResourceExecutedContext, bool> ExpectsJsonResult = (context) =>
    {
        if (context.ActionDescriptor is ControllerActionDescriptor cad)
        {
            return !typeof(IActionResult).IsAssignableFrom(cad.MethodInfo.ReturnType) ||
                typeof(FileResult).IsAssignableFrom(cad.MethodInfo.ReturnType) && context.HttpContext.Request.Method != "GET";

        }
        return false;
    };

    public static async Task<byte[]> ReadAllBody(HttpContext httpContext)
    {
        //httpContext.Request.EnableBuffering();
        httpContext.Request.Body.Seek(0, System.IO.SeekOrigin.Begin);
        var result = await httpContext.Request.Body.ReadAllBytesAsync();
        return result;
    }

    private static HttpStatusCode GetStatus(Type type)
    {
        if (type == typeof(UnauthorizedAccessException))
            return HttpStatusCode.Forbidden;

        if (type == typeof(AuthenticationException))
            return HttpStatusCode.Forbidden; // Unauthorized produces Login Password dialog in Mixed mode

        if (type == typeof(EntityNotFoundException))
            return HttpStatusCode.NotFound;

        if (type == typeof(IntegrityCheckException))
            return HttpStatusCode.BadRequest;

        return HttpStatusCode.InternalServerError;
    }


}

public class HttpError
{
    public HttpError(Exception e, bool includeErrorDetails = true, bool includeId = true)
    {
        this.ExceptionMessage = e.Message;
        this.ExceptionType = e.GetType().FullName!;
        this.Model = e is ModelRequestedException mre ? mre.Model : null;

        if (includeId)
            this.ExceptionId = e.GetExceptionEntity()?.Id.ToString();

        if (includeErrorDetails)
        {
            this.StackTrace = e.StackTrace;
            this.InnerException = e.InnerException == null ? null : new HttpError(e.InnerException);
        }
    }

    public string ExceptionType { get; set; }
    public string ExceptionMessage { get; set; }
    public string? ExceptionId { get; set; }
    public string? StackTrace { get; set; }
    public ModelEntity? Model; /*{ get; set; }*/
    public HttpError? InnerException; /*{ get; set; }*/
}

public class SignumInitializeFilterAttribute : IAsyncResourceFilter
{
    public static Action InitializeDatabase = () => throw new InvalidOperationException("SignumInitializeFilterAttribute.InitializeDatabase should be set in Startup");
    static Lock lockKey = new();
    public bool Initialized = false;

    public Task OnResourceExecutionAsync(ResourceExecutingContext context, ResourceExecutionDelegate next)
    {
        if (!Initialized)
        {
            lock (lockKey)
            {
                if (!Initialized)
                {
                    InitializeDatabase();
                    Initialized = true;
                }
            }
        }

        return next();
    }
}
