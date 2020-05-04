using System;
using System.Collections.Generic;
using System.Linq;
using Signum.Engine.Basics;
using Signum.React.Facades;
using Signum.Entities;
using Signum.Engine;
using Signum.React.Filters;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace Signum.React.ApiControllers
{
    public class EntitiesController : ControllerBase
    {
        [HttpGet("api/entity/{type}/{id}"), ProfilerActionSplitter("type")]
        public Entity GetEntity(string type, string id)
        {
            var entityType = TypeLogic.GetType(type);

            var primaryKey = PrimaryKey.Parse(id, entityType);

            var entity = Database.Retrieve(entityType, primaryKey);
            using (ExecutionMode.ApiRetrievedScope(entity, "EntitiesController.GetEntity"))
            {
                return entity;
            }

        }

        [HttpGet("api/entityPack/{type}/{id}"), ProfilerActionSplitter("type")]
        public EntityPackTS GetEntityPack(string type, string id)
        {
            var entityType = TypeLogic.GetType(type);

            var primaryKey = PrimaryKey.Parse(id, entityType);

            var entity = Database.Retrieve(entityType, primaryKey);
            using (ExecutionMode.ApiRetrievedScope(entity, "EntitiesController.GetEntityPack"))
            {
                return SignumServer.GetEntityPack(entity);
            }
        }

        [HttpPost("api/entityPackEntity")/*, ValidateModelFilter*/]
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
