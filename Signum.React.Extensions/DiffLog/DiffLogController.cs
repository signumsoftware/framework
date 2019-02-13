using Signum.Engine;
using Signum.Engine.DiffLog;
using Signum.Engine.Maps;
using Signum.Entities;
using Signum.Entities.Basics;
using Signum.Entities.DiffLog;
using Signum.Utilities;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;

namespace Signum.React.DiffLog
{
    public class DiffLogController : ControllerBase
    {
        [HttpGet("api/diffLog/{id}")]
        public DiffLogResult GetOperationDiffLog(string id)
        {
            var operationLog = Database.Retrieve<OperationLogEntity>(PrimaryKey.Parse(id, typeof(OperationLogEntity)));

            var logs = DiffLogLogic.OperationLogNextPrev(operationLog);

            StringDistance sd = new StringDistance();

            var prevFinal = logs.Min?.Mixin<DiffLogMixin>().FinalState;

            string? nextInitial = logs.Max != null ? logs.Max.Mixin<DiffLogMixin>().InitialState :
                operationLog.Target!.Exists() ? GetDump(operationLog.Target!) : null;

            return new DiffLogResult
            {
                prev = logs.Min?.ToLite(),
                diffPrev = prevFinal == null || operationLog.Mixin<DiffLogMixin>().InitialState == null ? null : sd.DiffText(prevFinal, operationLog.Mixin<DiffLogMixin>().InitialState),
                diff = sd.DiffText(operationLog.Mixin<DiffLogMixin>().InitialState, operationLog.Mixin<DiffLogMixin>().FinalState),
                diffNext = operationLog.Mixin<DiffLogMixin>().FinalState == null || nextInitial == null ? null : sd.DiffText(operationLog.Mixin<DiffLogMixin>().FinalState, nextInitial),
                next = logs.Max?.ToLite(),
            };
        }

        private string GetDump(Lite<IEntity> target)
        {
            var entity = target.RetrieveAndForget();

            using (CultureInfoUtils.ChangeBothCultures(Schema.Current.ForceCultureInfo))
                return entity.Dump();
        }
    }

#pragma warning disable IDE1006 // Naming Styles
    public class DiffLogResult
    {
        public Lite<OperationLogEntity>? prev { get; internal set; }
        public List<StringDistance.DiffPair<List<StringDistance.DiffPair<string>>>>? diffPrev { get; internal set; }

        public List<StringDistance.DiffPair<List<StringDistance.DiffPair<string>>>>? diff { get; internal set; }

        public List<StringDistance.DiffPair<List<StringDistance.DiffPair<string>>>>? diffNext { get; internal set; }
        public Lite<OperationLogEntity>? next { get; internal set; }
    }
#pragma warning restore IDE1006 // Naming Styles
}
