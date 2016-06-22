using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using Signum.Engine.Authorization;
using Signum.Entities;
using Signum.Entities.Authorization;
using Signum.Services;
using Signum.Utilities;
using Signum.React.Facades;
using Signum.React.Authorization;
using Signum.Engine.Cache;
using Signum.Engine;
using Signum.Entities.Cache;
using Signum.Utilities.ExpressionTrees;
using Signum.Entities.Processes;
using Signum.Engine.Processes;
using System.Threading;
using Signum.React.ApiControllers;
using Signum.Engine.Basics;
using System.Web;
using Signum.React.Files;
using System.IO;
using Signum.Entities.Basics;
using Signum.Engine.DiffLog;
using Signum.Entities.DiffLog;
using Signum.Engine.Maps;

namespace Signum.React.DiffLog
{
    public class DiffLogController : ApiController
    {
        [Route("api/diffLog/{id}"), HttpGet]
        public DiffLogResult GetOperationDiffLog(string id)
        {
            var operationLog = Database.Retrieve<OperationLogEntity>(PrimaryKey.Parse(id, typeof(OperationLogEntity)));

            var logs = DiffLogLogic.OperationLogNextPrev(operationLog);
            
            StringDistance sd = new StringDistance();

            var prevFinal = logs.Min?.Mixin<DiffLogMixin>().FinalState;

            string nextInitial = logs.Max != null ? logs.Max.Mixin<DiffLogMixin>().InitialState :
                operationLog.Target.Exists() ? GetDump(operationLog.Target) : null;
            
            return new DiffLogResult
            {
                prev = logs.Min?.ToLite(),
                diffPrev = prevFinal == null ? null : sd.DiffText(prevFinal, operationLog.Mixin<DiffLogMixin>().InitialState),
                diff = sd.DiffText(operationLog.Mixin<DiffLogMixin>().InitialState, operationLog.Mixin<DiffLogMixin>().FinalState),
                diffNext = nextInitial == null ? null : sd.DiffText(operationLog.Mixin<DiffLogMixin>().FinalState, nextInitial),
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

    public class DiffLogResult
    {
        public Lite<OperationLogEntity> prev { get; internal set; }
        public List<StringDistance.DiffPair<List<StringDistance.DiffPair<string>>>> diffPrev { get; internal set; }

        public List<StringDistance.DiffPair<List<StringDistance.DiffPair<string>>>> diff { get; internal set; }

        public List<StringDistance.DiffPair<List<StringDistance.DiffPair<string>>>> diffNext { get; internal set; }
        public Lite<OperationLogEntity> next { get; internal set; }
    }
}