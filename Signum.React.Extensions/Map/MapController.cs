using Signum.Engine.Basics;
using Signum.Entities.Map;
using Signum.Engine.Authorization;
using Signum.React.Maps;
using Microsoft.AspNetCore.Mvc;

namespace Signum.React.Map
{
    public class MapController : ControllerBase
    {
        [HttpGet("api/map/types")]
        public SchemaMapInfo Index()
        {
            MapPermission.ViewMap.AssertAuthorized();

            return SchemaMap.GetMapInfo();

        }

        [HttpGet("api/map/operations/{typeName}")]
        public OperationMapInfo Operation(string typeName)
        {
            MapPermission.ViewMap.AssertAuthorized();

            return OperationMap.GetOperationMapInfo(TypeLogic.GetType(typeName));
        }
    }
}
