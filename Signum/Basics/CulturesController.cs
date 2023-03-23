using Signum.Entities.Basics;
using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Mvc.Routing;
using Signum.API.Filters;
using Signum.Engine.Basics;

namespace Signum.React.Translation;

[ValidateModelFilter]
public class CultureController : ControllerBase
{
    IHostApplicationLifetime _env;
    public CultureController(IHostApplicationLifetime env)
    {
        _env = env;
    }

    [HttpGet("api/culture/cultures"), SignumAllowAnonymous]
    public List<CultureInfoEntity> GetCultures()
    {
        return CultureInfoLogic.CultureInfoToEntity.Value.Values.ToList();
    }

    [HttpGet("api/culture/currentCulture"), SignumAllowAnonymous]
    public CultureInfoEntity CurrentCulture()
    {
        return CultureInfo.CurrentCulture.TryGetCultureInfoEntity() ?? CultureInfoLogic.CultureInfoToEntity.Value.Values.FirstEx();
    }

    public static Action<Lite<CultureInfoEntity>>? OnChangeCulture;

    [HttpPost("api/culture/setCurrentCulture"), SignumAllowAnonymous]
    public string SetCurrentCulture([Required, FromBody]Lite<CultureInfoEntity> culture)
    {
        var ci = ExecutionMode.Global().Using(_ => culture.RetrieveAndRemember().ToCultureInfo());

     

        ControllerContext.HttpContext.Response.Cookies.Append("language", ci.Name, new CookieOptions
        {
            Expires = DateTimeOffset.Now.AddYears(10),
            Path = new UrlHelper(ControllerContext).Content("~/"),
            IsEssential = true,
            Domain = Request.Host.Host
        });
        return ci.Name;
    }
}
