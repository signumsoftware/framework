using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Signum.API.Filters;
using Signum.Engine.Maps;
using System.ComponentModel.DataAnnotations;
using System.Net;


namespace Signum.API.Controllers;

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


    [HttpGet("api/reflection/typeInDomains")]
    public Dictionary<string/*entityType*/, Dictionary<string /*domain type*/, ReadWriteIds>> GetTypeInDomain()
    {
        Schema s = Schema.Current;
        var context = (from entityType in TypeLogic.TypeToEntity.Keys
                       where s.IsAllowed(entityType, true) == null
                       let dic = ReflectionServer.GetAllowedDomains(entityType)
                       where dic != null
                       select KeyValuePair.Create(TypeLogic.GetCleanName(entityType),
                            dic.ToDictionaryEx(kvp => TypeLogic.GetCleanName(kvp.Key), kvp => new ReadWriteIds
                            {
                                Read = kvp.Value.Where(a=>a.Value == DomainAccess.Read).Select(a => (object)a.Key.Id.Object).ToList(),
                                Write = kvp.Value.Where(a=>a.Value == DomainAccess.Write).Select(a => (object)a.Key.Id.Object).ToList(),
                            })))
                       .ToDictionaryEx();

        return context;
    }

    public class ReadWriteIds
    {
        public List<object> Read { get; set; } = new List<object>();
        public List<object> Write { get; set; } = new List<object>();
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
            RequestUrl = Try(int.MaxValue, () => error.Url),
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
