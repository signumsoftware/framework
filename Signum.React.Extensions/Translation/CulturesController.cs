using System;
using Signum.Engine;
using Signum.Engine.Authorization;
using Signum.Engine.Basics;
using Signum.Engine.Operations;
using Signum.Entities;
using Signum.Entities.Authorization;
using Signum.Entities.Basics;
using Signum.React.Filters;
using Signum.Utilities;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using System.ComponentModel.DataAnnotations;
using System.Net;
using Microsoft.AspNetCore.Http;
using Signum.React.Authorization;
using Microsoft.Extensions.Hosting;

namespace Signum.React.Translation
{
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

        [HttpPost("api/culture/setCurrentCulture"), SignumAllowAnonymous]
        public string SetCurrentCulture([Required, FromBody]Lite<CultureInfoEntity> culture)
        {
            var ci = ExecutionMode.Global().Using(_ => culture.RetrieveAndRemember().ToCultureInfo());

            if (UserEntity.Current != null && !UserEntity.Current.Is(AuthLogic.AnonymousUser)) //Won't be used till next refresh
            {
                using (AuthLogic.Disable())
                using (OperationLogic.AllowSave<UserEntity>())
                {
                    var user = UserEntity.Current.ToLite().RetrieveAndRemember();
                    user.CultureInfo = culture.RetrieveAndRemember();
                    UserEntity.Current = user;
                    user.Save();
                }
            }

            ControllerContext.HttpContext.Response.Cookies.Append("language", ci.Name, new CookieOptions
            {
                Expires = DateTimeOffset.Now.AddYears(10),
                Path = "/",
                IsEssential = true,
                Domain = Request.Host.Host
            });
            return ci.Name;
        }
    }
}
