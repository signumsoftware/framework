using Signum.React.Facades;
using Signum.Entities.Basics;
using Signum.Engine.Basics;
using Signum.React.Filters;
using Microsoft.AspNetCore.Http;
using Signum.Engine.Maps;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Http.Extensions;

namespace Signum.React.ApiControllers;

public class ReflectionController : ControllerBase
{
    [HttpGet("api/reflection/types"), SignumAllowAnonymous]
    public ActionResult<Dictionary<string, TypeInfoTS>> Types()
    {
        this.Response.GetTypedHeaders().LastModified = ReflectionServer.LastModified;

        var requestHeaders = this.Request.GetTypedHeaders();
        if (requestHeaders.IfModifiedSince.HasValue &&
            (ReflectionServer.LastModified - requestHeaders.IfModifiedSince.Value).TotalSeconds < 1)
        {
            return this.StatusCode(StatusCodes.Status304NotModified);
        }

        return ReflectionServer.GetTypeInfoTS();
    }

    [HttpGet("api/reflection/typeEntity/{typeName}")]
    public TypeEntity? GetTypeEntity(string typeName)
    {
        return TypeLogic.TryGetType(typeName)?.ToTypeEntity();
    }

    [HttpGet("api/reflection/enumEntities/{typeName}")]
    public Dictionary<string, Entity> GetEnumEntities(string typeName)
    {
        var type = EnumEntity.Extract(TypeLogic.GetType(typeName))!;

        return EnumEntity.GetValues(type).ToDictionary(a => a.ToString(), a => EnumEntity.FromEnumUntyped(a));
    }


    [HttpPost("api/registerClientError"), ValidateModelFilter, SignumAllowAnonymous]
    public void ClientError([Required, FromBody] ClientErrorModel error)
    {
        var httpContext = this.HttpContext;

        var req = httpContext.Request;
        var connFeature = httpContext.Features.Get<IHttpConnectionFeature>()!;

        var clientException = new ExceptionEntity(error)
        {
            UserAgent = Try(300, () => req.Headers["User-Agent"].FirstOrDefault()),
            RequestUrl = Try(int.MaxValue, () => req.GetDisplayUrl()),
            UrlReferer = Try(int.MaxValue, () => req.Headers["Referer"].ToString()),
            UserHostAddress = Try(100, () => connFeature.RemoteIpAddress?.ToString()),
            UserHostName = Try(100, () => connFeature.RemoteIpAddress == null ? null : Dns.GetHostEntry(connFeature.RemoteIpAddress).HostName),

            Version = Schema.Current.Version.ToString(),
            ApplicationName = Schema.Current.ApplicationName,
            User = UserHolder.Current?.User,
        };

        using (ExecutionMode.Global())
        {
            clientException.Save();
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
}
