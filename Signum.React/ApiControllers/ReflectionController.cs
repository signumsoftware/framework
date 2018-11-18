using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using Signum.Entities;
using Signum.React.Facades;
using Signum.Utilities;
using Signum.Engine;
using Signum.React.Filters;
using Signum.Entities.Basics;
using Signum.Engine.Basics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace Signum.React.ApiControllers
{
    public class ReflectionController : ApiController
    {
        [HttpGet("api/reflection/types"), AllowAnonymous]
        public Dictionary<string, TypeInfoTS> Types()
        {
            return ReflectionServer.GetTypeInfoTS();
        }

        [HttpGet("api/reflection/typeEntity/{typeName}")]
        public TypeEntity GetTypeEntity(string typeName)
        {
            return TypeLogic.TryGetType(typeName)?.ToTypeEntity();
        }
    }
}
