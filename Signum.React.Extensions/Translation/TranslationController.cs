using Signum.Engine;
using Signum.Entities.Basics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace Signum.React.Translation
{
    public class TranslationController : ApiController
    {
        [Route("api/translation/cultures"), HttpGet]
        public Dictionary<string, string> GetCultures()
        {
            using (ExecutionMode.Global())
                return Database.Query<CultureInfoEntity>().ToDictionary(a => a.Name, a => a.NativeName);
        }
    }
}
