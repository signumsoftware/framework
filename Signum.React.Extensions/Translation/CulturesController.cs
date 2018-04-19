using Signum.Engine;
using Signum.Engine.Authorization;
using Signum.Engine.Basics;
using Signum.Engine.Operations;
using Signum.Engine.Translation;
using Signum.Entities;
using Signum.Entities.Authorization;
using Signum.Entities.Basics;
using Signum.React.Filters;
using Signum.Utilities;
using Signum.Utilities.DataStructures;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Signum.React.ApiControllers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

namespace Signum.React.Translation
{
    public class CultureController : ApiController
    {
        IHostingEnvironment _env;
        public CultureController(IHostingEnvironment env)
        {
            _env = env;
        }

        [Route("api/culture/cultures"), HttpGet, AllowAnonymous]
        public List<CultureInfoEntity> GetCultures()
        {
            return CultureInfoLogic.CultureInfoToEntity.Value.Values.ToList();
        }

        [Route("api/culture/currentCulture"), HttpGet, AllowAnonymous]
        public CultureInfoEntity CurrentCulture()
        {
            return CultureInfo.CurrentCulture.TryGetCultureInfoEntity() ?? CultureInfoLogic.CultureInfoToEntity.Value.Values.FirstEx();
        }

        [Route("api/culture/currentCulture"), HttpPost, AllowAnonymous]
        public void SetCurrentCulture(Lite<CultureInfoEntity> culture)
        {
            var ci = ExecutionMode.Global().Using(_ => culture.Retrieve().ToCultureInfo());

            if (UserEntity.Current != null) //Won't be used till next refresh
            {
                var user = UserEntity.Current.ToLite().Retrieve();

                user.CultureInfo = culture.Retrieve();
                using (AuthLogic.Disable())
                using (OperationLogic.AllowSave<UserEntity>())
                    user.Save();
            }

            this.ActionContext.HttpContext.Response.Cookies.Append("language", ci.Name, new CookieOptions
            {
                Expires = DateTime.Now.AddMonths(6),
                Domain = ActionContext.HttpContext.Request.Host.ToString(),
                Path = _env.WebRootPath
            });
        }
    }
}
