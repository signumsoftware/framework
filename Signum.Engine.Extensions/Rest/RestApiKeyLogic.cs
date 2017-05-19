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
using Signum.Entities.RestLog;
using IntTec.Entities;
using Signum.Entities.Authorization;
using Signum.Entities;
using Signum.Engine;
using Signum.Engine.Operations;
using Signum.Utilities;

namespace Signum.Engine.RestLog
{
    public class RestApiKeyLogic
    {
        public static void Start(SchemaBuilder sb, DynamicQueryManager dqm)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                sb.Include<RestApiKeyEntity>()
                    .WithQuery(dqm, e => new
                    {
                        Entity = e,
                        e.Id,
                        e.User,
                        e.ApiKey
                    });

                new Graph<RestApiKeyEntity>.Execute(RestApiKeyOperation.Save)
                {
                    AllowsNew = true,
                    Lite = false,
                    Execute = (e, _) => { },
                }.Register();
            }
        }
    }
}
