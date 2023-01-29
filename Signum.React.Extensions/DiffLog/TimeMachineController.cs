using Microsoft.AspNetCore.Mvc;

namespace Signum.React.DiffLog;

public class TimeMachineController : ControllerBase
{
    [HttpGet("api/retrieveVersion/{typeName}/{id}")]
    public EntityDump RetrieveVersion(string typeName, string id, DateTimeOffset asOf)
    {
        var type = TypeLogic.GetType(typeName);
        var pk = PrimaryKey.Parse(id, type);

        using (SystemTime.Override(asOf))
        {
            var entity = Database.Retrieve(type, pk);
            return new EntityDump
            {
                Entity = entity,
                Dump = GetDump(entity)
            };
        }
    }

    private string GetDump(Entity entity)
    {
        using (CultureInfoUtils.ChangeBothCultures(Schema.Current.ForceCultureInfo))
            return ObjectDumper.Dump(entity);
    }

}

public class EntityDump
{
    public required Entity Entity;
    public required string Dump; 
}
