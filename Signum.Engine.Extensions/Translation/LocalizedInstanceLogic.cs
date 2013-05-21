using Signum.Engine.DynamicQuery;
using Signum.Engine.Maps;
using Signum.Engine.Operations;
using Signum.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Signum.Entities.Translation;

namespace Signum.Engine.Translation
{
    public static class LocalizedInstanceLogic
    {
        public static void Start(SchemaBuilder sb, DynamicQueryManager dqm)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                sb.Include<LocalizedInstanceDN>();

                dqm.RegisterQuery(typeof(LocalizedInstanceDN), () =>
                    from e in Database.Query<LocalizedInstanceDN>()
                    select new
                    {
                        Entity = e,
                        e.Id,
                        e.Culture,
                        e.Instance,
                    });

                new Graph<LocalizedInstanceDN>.Execute(LocalizedInstanceOperation.Save)
                {
                    AllowsNew = true,
                    Lite = false,
                    Execute = (e, _)=>{}
                }.Register();
            }
        }
    }
}
