using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities;
using Signum.Engine;
using Signum.Entities.Basics;
using Signum.Engine.Maps;
using Signum.Utilities;
using Signum.Engine.SchemaInfoTables;
using Signum.Engine.Basics;
using Signum.Entities.Map;
using Signum.Engine.Authorization;
using Signum.React.Maps;
using Microsoft.AspNetCore.Mvc;
using Signum.React.ApiControllers;

namespace Signum.React.Map
{
    public class MapController : ApiController
    {
        [Route("api/map/types"), HttpGet]
        public SchemaMapInfo Index()
        {
            MapPermission.ViewMap.AssertAuthorized();
            
            return SchemaMap.GetMapInfo();
            
        }

        [Route("api/map/operations/{typeName}"), HttpGet]
        public OperationMapInfo Operation(string typeName)
        {
            MapPermission.ViewMap.AssertAuthorized();

            return OperationMap.GetOperationMapInfo(TypeLogic.GetType(typeName));
        }
    }
}
