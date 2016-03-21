using Signum.Engine;
using Signum.Engine.Authorization;
using Signum.Engine.Basics;
using Signum.Engine.Operations;
using Signum.Entities;
using Signum.Entities.Authorization;
using Signum.Entities.Basics;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Web.Http;

namespace Signum.React.Translation
{
    public class TranslationController : ApiController
    {
        [Route("api/translation/cultures"), HttpGet]
        public Dictionary<string, Lite<CultureInfoEntity>> GetCultures()
        {
            return CultureInfoLogic.CultureInfoToEntity.Value.Values.ToDictionary(a => a.Name, a => a.ToLite());
        }

        [Route("api/translation/currentCulture"), HttpGet]
        public CultureInfoEntity CurrentCulture()
        {
            return CultureInfo.CurrentCulture.ToCultureInfoEntity();
        }

        [Route("api/translation/currentCulture"), HttpPost]
        public HttpResponseMessage SetCurrentCulture(Lite<CultureInfoEntity> culture)
        {
            var resp = new HttpResponseMessage();
            var ci = culture.Retrieve().ToCultureInfo();

            if (UserEntity.Current == null)
            {
                resp.Headers.AddCookies(new[] 
                {
                    new CookieHeaderValue("language", ci.Name)
                    {
                        Expires = DateTime.Now.AddMonths(6),
                        Domain = Request.RequestUri.Host,
                        Path = "/"
                    }
                });
            }
            else
            {
                UserEntity.Current.CultureInfo = culture.Retrieve();
                using (AuthLogic.Disable())
                using (OperationLogic.AllowSave<UserEntity>())
                    UserEntity.Current.Save();
            }

            resp.StatusCode = HttpStatusCode.NoContent;

            return resp;
        }
    }
}
