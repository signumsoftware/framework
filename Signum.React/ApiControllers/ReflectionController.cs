using Signum.React.Facades;
using Signum.Entities.Basics;
using Signum.Engine.Basics;
using Signum.React.Filters;
using Microsoft.AspNetCore.Http;
using Signum.Engine.Maps;
using Signum.Entities.Authorization;

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


    [HttpPost("api/registerClientError"), ValidateModelFilter, ProfilerActionSplitter]
    public void ClientError([Required, FromBody] ClientExceptionModel error)
    {
        var httpContext = this.HttpContext;
        var clientException =  new ExceptionEntity(error, httpContext);

        clientException.Version = Schema.Current.Version.ToString();
        clientException.ApplicationName = Schema.Current.ApplicationName;
        clientException.User = UserEntity.Current;

        if (Database.Query<ExceptionEntity>().Any(e => e.ExceptionMessageHash == clientException.ExceptionMessageHash && e.CreationDate.AddSeconds(60) > clientException.CreationDate))
            return;

        clientException.Save();
    }
}
