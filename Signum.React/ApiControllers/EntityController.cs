using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using Signum.Engine.Basics;
using Signum.Engine.DynamicQuery;
using Signum.Entities.DynamicQuery;
using Signum.React.Facades;
using Signum.Utilities;
using Signum.Entities;
using Signum.Engine;
using Signum.Engine.Operations;
using Signum.React.Filters;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace Signum.React.ApiControllers
{
    [ValidateModelFilter]
    public class EntitiesController : ApiController
    {
        [HttpGet("api/entity/{type}/{id}"), ProfilerActionSplitter("type")]
        public Entity GetEntity(string type, string id)
        {
            var entityType = TypeLogic.GetType(type);

            var primaryKey = PrimaryKey.Parse(id, entityType);

            return Database.Retrieve(entityType, primaryKey);
        }

        [HttpGet("api/entityPack/{type}/{id}"), ProfilerActionSplitter("type")]
        public EntityPackTS GetEntityPack(string type, string id)
        {
            var entityType = TypeLogic.GetType(type);

            var primaryKey = PrimaryKey.Parse(id, entityType);

            var entity = Database.Retrieve(entityType, primaryKey);

            return SignumServer.GetEntityPack(entity);
        }

        [HttpPost("api/entityPackEntity")]
        public EntityPackTS GetEntityPackEntity([Required, FromBody]Entity entity)
        { 
            return SignumServer.GetEntityPack(entity);
        }

        [HttpPost("api/entityToStrings")]
        public string[] EntityToStrings([Required, FromBody]Lite<Entity>[] lites)
        {
            if (lites == null || lites.Length == 0)
                throw new ArgumentNullException(nameof(lites));

            return lites.Select(a => Database.GetToStr(a.EntityType, a.Id)).ToArray();
        }

        [HttpGet("api/fetchAll/{typeName}"), ProfilerActionSplitter("typeName")]
        public List<Entity> FetchAll(string typeName)
        {
            if (typeName == null)
            {
                throw new ArgumentNullException(typeName);
            }

            var type = TypeLogic.GetType(typeName);

            return Database.RetrieveAll(type);
        }

        [HttpPost("api/validateEntity"), ValidateModelFilter]
        public void ValidateEntity([Required, FromBody]ModifiableEntity entity)
        {
            return;
        }
    }
}