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

namespace Signum.React.Translation
{
    [ValidateModelFilter]
    public class CultureController : ControllerBase
    {
        IHostingEnvironment _env;
        public CultureController(IHostingEnvironment env)
        {
            _env = env;
        }

        [HttpGet("api/culture/cultures"), AllowAnonymous]
        public List<CultureInfoEntity> GetCultures()
        {
            return CultureInfoLogic.CultureInfoToEntity.Value.Values.ToList();
        }

        [HttpGet("api/culture/currentCulture"), AllowAnonymous]
        public CultureInfoEntity CurrentCulture()
        {
            return CultureInfo.CurrentCulture.TryGetCultureInfoEntity() ?? CultureInfoLogic.CultureInfoToEntity.Value.Values.FirstEx();
        }

        [HttpPost("api/culture/setCurrentCulture"), AllowAnonymous]
        public string SetCurrentCulture([Required, FromBody]Lite<CultureInfoEntity> culture)
        {
            var ci = ExecutionMode.Global().Using(_ => culture.Retrieve().ToCultureInfo());

            if (UserEntity.Current != null && !UserEntity.Current.Is(AuthLogic.AnonymousUser)) //Won't be used till next refresh
            {
                var user = UserEntity.Current.ToLite().Retrieve();
                user.CultureInfo = culture.Retrieve();

                using (AuthLogic.Disable())
                using (OperationLogic.AllowSave<UserEntity>())
                {
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
