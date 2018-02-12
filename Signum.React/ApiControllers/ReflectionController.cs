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
using Signum.Engine;
using Signum.React.Filters;
using Signum.Entities.Basics;
using Signum.Engine.Basics;

namespace Signum.React.ApiControllers
{
    public class ReflectionController : ApiController
    {
        [Route("api/reflection/types"), HttpGet, AllowAnonymous]
        public Dictionary<string, TypeInfoTS> Types()
        {
            return ReflectionServer.GetTypeInfoTS();
        }

        [Route("api/reflection/typeEntity/{typeName}"), HttpGet]
        public TypeEntity GetTypeEntity(string typeName)
        {
            return TypeLogic.TryGetType(typeName)?.ToTypeEntity();
        }
    }
}
