using Signum.Engine.DiffLog;
using Signum.Entities.Basics;
using Signum.Entities.DiffLog;
using Microsoft.AspNetCore.Mvc;

namespace Signum.React.DiffLog;

public class DiffLogController : ControllerBase
{
    [HttpGet("api/diffLog/{id}")]
    public DiffLogResult GetOperationDiffLog(string id, bool simplify)
    {
        var operationLog = Database.Retrieve<OperationLogEntity>(PrimaryKey.Parse(id, typeof(OperationLogEntity)));

        var logs = DiffLogLogic.OperationLogNextPrev(operationLog);

        StringDistance sd = new StringDistance();

        var prevFinal = DiffLogLogic.SimplifyDump(logs.Min?.Mixin<DiffLogMixin>().FinalState.Text, simplify);

        string? nextInitial = logs.Max != null ? DiffLogLogic.SimplifyDump(logs.Max.Mixin<DiffLogMixin>().InitialState.Text, simplify) :
            operationLog.Target!.Exists() ? GetDump(operationLog.Target!) : null;

        string? initial = DiffLogLogic.SimplifyDump(operationLog.Mixin<DiffLogMixin>().InitialState.Text, simplify);
        string? final = DiffLogLogic.SimplifyDump(operationLog.Mixin<DiffLogMixin>().FinalState.Text, simplify);

        return new DiffLogResult
        {
            prev = logs.Min?.ToLite(),
            diffPrev = prevFinal == null || initial == null ? null : sd.DiffText(prevFinal, initial),
            initial = initial,
            diff = sd.DiffText(initial, final),
            final = final,
            diffNext = final == null || nextInitial == null ? null : sd.DiffText(final, nextInitial),
            next = logs.Max?.ToLite(),
        };
    }

    private string GetDump(Lite<IEntity> target)
    {
        var entity = target.Retrieve();

        using (CultureInfoUtils.ChangeBothCultures(Schema.Current.ForceCultureInfo))
            return entity.Dump();
    }
}

#pragma warning disable IDE1006 // Naming Styles
public class DiffLogResult
{
    public Lite<OperationLogEntity>? prev { get; internal set; }
    public List<StringDistance.DiffPair<List<StringDistance.DiffPair<string>>>>? diffPrev { get; internal set; }
    public string? initial { get; internal set; }
    public List<StringDistance.DiffPair<List<StringDistance.DiffPair<string>>>>? diff { get; internal set; }
    public string? final { get; internal set; }
    public List<StringDistance.DiffPair<List<StringDistance.DiffPair<string>>>>? diffNext { get; internal set; }
    public Lite<OperationLogEntity>? next { get; internal set; }
}
#pragma warning restore IDE1006 // Naming Styles
