using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Client;
using Signum.API.Filters;
using System.ComponentModel.DataAnnotations;

namespace Signum.API.ApiControllers;

public class EntitiesController : ControllerBase
{
    [HttpGet("api/entity/{type}/{id}"), ProfilerActionSplitter("type")]
    public Entity GetEntity(string type, string id, [FromQuery]int? partitionId)
    {
        var entityType = TypeLogic.GetType(type);

        var primaryKey = PrimaryKey.Parse(id, entityType);
        var lite = Lite.Create(entityType, primaryKey, partitionId: partitionId);

        using (ExecutionMode.ApiRetrievedScope(lite, "EntitiesController.GetEntity"))
        {
            var entity = Database.Retrieve(entityType, primaryKey, partitionId);
            return entity;
        }

    }

    [HttpGet("api/entityPack/{type}/{id}"), ProfilerActionSplitter("type")]
    public EntityPackTS GetEntityPack(string type, string id, [FromQuery] int? partitionId)
    {
        var entityType = TypeLogic.GetType(type);

        var primaryKey = PrimaryKey.Parse(id, entityType);
        var lite = Lite.Create(entityType, primaryKey, partitionId: partitionId);

        using (ExecutionMode.ApiRetrievedScope(lite, "EntitiesController.GetEntityPack"))
        {
            var entity = Database.Retrieve(lite);
            return SignumServer.GetEntityPack(entity);
        }
    }

    [HttpPost("api/entityPackEntity")/*, ValidateModelFilter*/]
    public EntityPackTS GetEntityPackEntity([Required, FromBody]Entity entity)
    {
        return SignumServer.GetEntityPack(entity);
    }

    [HttpPost("api/liteModels")]
    public object[] LiteModels([Required, FromBody]Lite<Entity>[] lites)
    {
        if (lites == null || lites.Length == 0)
            throw new ArgumentNullException(nameof(lites));

        return lites.Select(a => Database.GetLiteModel(a.EntityType, a.Id, a.ModelType)).ToArray();
    }

    [HttpGet("api/fetchAll/{typeName}"), ProfilerActionSplitter("typeName")]
    public List<Entity> FetchAll(string typeName)
    {
        if (typeName == null)
            throw new ArgumentNullException(typeName);

        var type = TypeLogic.GetType(typeName);

        if (EntityKindCache.GetEntityData(type) == EntityData.Transactional)
            throw new ArgumentNullException($"{typeName} is a Transactional entity");


        return Database.RetrieveAll(type);
    }

    [HttpPost("api/validateEntity"), ValidateModelFilter]
    public void ValidateEntity([Required, FromBody]ModifiableEntity entity)
    {
        return;
    }

    [HttpGet("api/exists/{type}/{id}")]
    public bool Exists(string type, string id)
    {
        var entityType = TypeLogic.GetType(type);

        var primaryKey = PrimaryKey.Parse(id, entityType);

        return Database.Exists(entityType, primaryKey);
    }
}
