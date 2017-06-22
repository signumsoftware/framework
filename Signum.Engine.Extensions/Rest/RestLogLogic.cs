using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Signum.Engine.Basics;
using Signum.Engine.DynamicQuery;
using Signum.Engine.Maps;
using Signum.Entities.Basics;
using Signum.Entities.Rest;
using System.Threading;

namespace Signum.Engine.Rest
{
    public class RestLogLogic
    {
        public static void Start(SchemaBuilder sb, DynamicQueryManager dqm)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                sb.Include<RestLogEntity>()
                    .WithIndex(a => a.StartDate)
                    .WithIndex(a => a.EndDate)
                    .WithIndex(a => a.Controller)
                    .WithIndex(a => a.Action)
                    .WithQuery(dqm, () => e => new
                    {
                        Entity = e,
                        e.Id,
                        e.StartDate,
                        e.Duration,
                        e.Url,
                        e.User,
                        e.Exception,
                    });

                ExceptionLogic.DeleteLogs += ExceptionLogic_DeleteRestLogs;
            }
        }

        private static void ExceptionLogic_DeleteRestLogs(DeleteLogParametersEmbedded parameters, StringBuilder sb, CancellationToken token)
        {
            Database.Query<RestLogEntity>().Where(a => a.StartDate < parameters.DateLimit).UnsafeDeleteChunksLog(parameters, sb, token);
        }
    }
}
