using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using Signum.Engine.Basics;
using Signum.Engine.DynamicQuery;
using Signum.Entities.DynamicQuery;
using Signum.React.Facades;
using Signum.Utilities;
using Signum.Entities;
using Signum.Engine;
using Signum.Engine.Operations;

namespace Signum.React.ApiControllers
{
    public class EntitiesController : ApiController
    {
        [Route("api/entity/{type}/{id}")]
        public Entity GetEntity(string type, string id)
        {
            var entityType = TypeLogic.GetType(type);

            var primaryKey = PrimaryKey.Parse(id, entityType);

            return Database.Retrieve(entityType, primaryKey);
        }

        [Route("api/entityPack/{type}/{id}")]
        public EntityPackTS GetEntityPack(string type, string id)
        {
            var entityType = TypeLogic.GetType(type);

            var primaryKey = PrimaryKey.Parse(id, entityType);

            var entity = Database.Retrieve(entityType, primaryKey);

            return SignumServer.GetEntityPack(entity);
        }

        [Route("api/entityPackEntity"), HttpPost]
        public EntityPackTS GetEntityPackEntity(Entity entity)
        { 
            return SignumServer.GetEntityPack(entity);
        }

        [Route("api/entityToStrings"), HttpPost]
        public string[] EntityToStrings(Lite<Entity>[] lites)
        {
            if (lites == null || lites.Length == 0)
                throw new ArgumentNullException(nameof(lites));

            return lites.Select(a => Database.GetToStr(a.EntityType, a.Id)).ToArray();
        }
    }
}