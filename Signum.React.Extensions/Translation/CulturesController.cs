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
    public class CultureController : ApiController
    {
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

            var resp = Request.CreateResponse(ci.Name);
            TranslationServer.AddLanguageCookie(resp, Request, ci);
            return resp;
        }
    }
}
