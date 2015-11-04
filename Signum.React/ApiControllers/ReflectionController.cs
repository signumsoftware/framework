using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Web.Http;
using Signum.Entities;
using Signum.React.Facades;
using Signum.Utilities;

namespace Signum.React.ApiControllers
{
    public class ReflectionController : ApiController
    {
        [Route("api/reflection/types"), HttpGet]
        public Dictionary<string, TypeInfoTS> Types()
        {
            return ReflectionClient.GetTypeInfoTS(CultureInfo.CurrentUICulture);
        }
    }
}
