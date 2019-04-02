using System.Collections.Generic;
using Signum.React.Facades;
using Signum.Entities.Basics;
using Signum.Engine.Basics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace Signum.React.ApiControllers
{
    public class ReflectionController : ControllerBase
    {
        [HttpGet("api/reflection/types"), AllowAnonymous]
        public Dictionary<string, TypeInfoTS> Types()
        {
            return ReflectionServer.GetTypeInfoTS();
        }

        [HttpGet("api/reflection/typeEntity/{typeName}")]
        public TypeEntity? GetTypeEntity(string typeName)
        {
            return TypeLogic.TryGetType(typeName)?.ToTypeEntity();
        }
    }
}
