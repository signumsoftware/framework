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
            
            var canExecutes = OperationLogic.ServiceCanExecute(entity);

            return new EntityPackTS
            {
                entity = entity,
                canExecute = canExecutes.ToDictionary(a => a.Key.Key, a => a.Value)
            };
        }

        [Route("api/operations/{type}")]
        public List<OperationInfoTS> GetOperationInfo(string type)
        {
            var entityType = TypeLogic.GetType(type);
            var operationInfos = OperationLogic.ServiceGetOperationInfos(entityType);

            return operationInfos.Select(o => new OperationInfoTS
            {
                key = o.OperationSymbol.Key,
                allowNews = o.AllowsNew,
                operationType = o.OperationType,
            }).ToList();
        }
    }

    public class EntityPackTS
    {
        public Entity entity;
        public Dictionary<string, string> canExecute; 
    }

    public class OperationInfoTS
    {
        public string key;
        public bool? allowNews;
        public OperationType operationType;
    }
}