using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Signum.Engine.DynamicQuery;
using Signum.Engine.Maps;
using Signum.Entities.Basics;
using Signum.Entities.RestLogging;

namespace Signum.Engine.Basics
{
    public class RestLoggingLogic
    {
        public static void Start(SchemaBuilder sb, DynamicQueryManager dqm)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {

                sb.Include<RequestEntity>().WithQuery(dqm,
                    e => new
                    {
                        Entity = e,
                        e.CreationDate,
                        URI = e.URL,
                        e.Values.Count
                    });

            }
        }
    }
}
