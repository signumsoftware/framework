using Microsoft.AspNetCore.Mvc;

namespace Signum.React.DiffLog;

public class TimeMachineController : ControllerBase
{
    [HttpGet("api/retrieveVersion/{typeName}/{id}")]
    public Entity RetrieveVersion(string typeName, string id, DateTimeOffset asOf)
    {
        var type = TypeLogic.GetType(typeName);
        var pk = PrimaryKey.Parse(id, type);


        using (SystemTime.Override(asOf))
            return Database.Retrieve(type, pk);
    }

    [HttpGet("api/diffVersions/{typeName}/{id}")]
    public List<StringDistance.DiffPair<List<StringDistance.DiffPair<string>>>> DiffVersiones(string typeName, string id, DateTimeOffset from, DateTimeOffset to)
    {
        var type = TypeLogic.GetType(typeName);
        var pk = PrimaryKey.Parse(id, type);


        var f = SystemTime.Override(from).Using(_ => Database.Retrieve(type, pk));
        var t = SystemTime.Override(to).Using(_ => Database.Retrieve(type, pk));

        var fDump = GetDump(f);
        var tDump = GetDump(t);
        StringDistance sd = new StringDistance();

        return sd.DiffText(fDump, tDump);
    }

    private string GetDump(Entity entity)
    {
        using (CultureInfoUtils.ChangeBothCultures(Schema.Current.ForceCultureInfo))
            return ObjectDumper.Dump(entity);
    }

}
