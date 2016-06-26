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
using System.Web.Http;

namespace Signum.React.Translation
{
    public class CulturesController : ApiController
    {
        [Route("api/cultures/cultures"), HttpGet, AllowAnonymous]
        public Dictionary<string, Lite<CultureInfoEntity>> GetCultures()
        {
            return CultureInfoLogic.CultureInfoToEntity.Value.Values.ToDictionary(a => a.Name, a => a.ToLite());
        }

        [Route("api/cultures/currentCulture"), HttpGet, AllowAnonymous]
        public CultureInfoEntity CurrentCulture()
        {
            return CultureInfo.CurrentCulture.ToCultureInfoEntity();
        }

        [Route("api/cultures/currentCulture"), HttpPost, AllowAnonymous]
        public HttpResponseMessage SetCurrentCulture(Lite<CultureInfoEntity> culture)
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

            var resp = new HttpResponseMessage();
            resp.Headers.AddCookies(new[]
            {
                new CookieHeaderValue("language", ci.Name)
                {
                    Expires = DateTime.Now.AddMonths(6),
                    Domain = Request.RequestUri.Host,
                    Path = "/"
                }
            });
            resp.StatusCode = HttpStatusCode.NoContent;
            return resp;
        }
    }
}
