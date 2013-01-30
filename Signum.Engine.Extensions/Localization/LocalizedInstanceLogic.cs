using Signum.Engine.DynamicQuery;
using Signum.Engine.Maps;
using Signum.Engine.Operations;
using Signum.Entities.Extensions.Localization;
using Signum.Entities.Localization;
using Signum.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Signum.Engine.Extensions.Localization
{
    public static class LocalizedInstanceLogic
    {
        public static void Start(SchemaBuilder sb, DynamicQueryManager dqm)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                sb.Include<LocalizedInstanceDN>();

                dqm[typeof(LocalizedInstanceDN)] = (from e in Database.Query<LocalizedInstanceDN>()
                                                   select new
                                                   {
                                                       Entity = e,
                                                       e.Id,
                                                       e.Culture,
                                                       e.Instance,
                                                   }).ToDynamic();

                new BasicExecute<LocalizedInstanceDN>(LocalizedInstanceOperation.Save)
                {
                    AllowsNew = true,
                    Lite = false,
                    Execute = (e, _)=>{}
                }.Register();
            }
        }
    }
}
