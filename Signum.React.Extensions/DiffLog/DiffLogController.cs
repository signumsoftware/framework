using Signum.Engine.DiffLog;
using Signum.Entities.Basics;
using Signum.Entities.DiffLog;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Amqp.Framing;
using Signum.Engine.Operations;

namespace Signum.React.DiffLog;

public class DiffLogController : ControllerBase
{

    [HttpGet("api/diffLog/previous/{id}")]
    public PreviousLog? GetPreviousOperationLog(string id)
    {
        var log = Lite.ParsePrimaryKey<OperationLogEntity>(id).InDB(a => new { a.Target, a.Start });
        var prev = Database.Query<OperationLogEntity>().Where(a => a.Exception == null && a.Target.Is(log.Target))
            .Where(a => a.End < log.Start).OrderByDescending(a => a.End).FirstOrDefault();

        if (prev == null)
            return null;
        
        string prevFinal = prev.Mixin<DiffLogMixin>().FinalState.Text!;

        return new PreviousLog
        {
            OperationLog = prev.ToLite(),
            Dump = prevFinal
        };
    }

    [HttpGet("api/diffLog/next/{id}")]
    public NextLog GetNextOperationLog(string id)
    {
        var log = Lite.ParsePrimaryKey<OperationLogEntity>(id).InDB(a => new { a.Target, a.End });
        var next = Database.Query<OperationLogEntity>().Where(a => a.Exception == null && a.Target.Is(log.Target))
          .Where(a => a.Start > log.End).OrderBy(a => a.Start).FirstOrDefault();

        if (next == null)
        {
            return new NextLog
            {
                Dump = !log.Target!.Exists() ? null : GetDump(log.Target!)
            };
        }
        
        return new NextLog
        {
            OperationLog = next.ToLite(),
            Dump = next.Mixin<DiffLogMixin>().InitialState.Text!
        };
    }

    private string GetDump(Lite<IEntity> target)
    {
        var entity = target.Retrieve();

        using (CultureInfoUtils.ChangeBothCultures(Schema.Current.ForceCultureInfo))
            return ObjectDumper.Dump(entity);
    }
}

public class PreviousLog
{
    public Lite<OperationLogEntity> OperationLog { get; internal set; }
    public string Dump { get; internal set; }
}

public class NextLog
{
    public Lite<OperationLogEntity>? OperationLog { get; internal set; }
    public string? Dump { get; internal set; }
}

