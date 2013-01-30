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
    public static class DataLocalizationLogic
    {
        public static void Start(SchemaBuilder sb, DynamicQueryManager dqm)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                sb.Include<DataLocalizationDN>();

                dqm[typeof(DataLocalizationDN)] = (from e in Database.Query<DataLocalizationDN>()
                                                   select new
                                                   {
                                                       Entity = e,
                                                       e.Id,
                                                       e.Culture,
                                                       e.Instance,
                                                       e.PropertyRoute,
                                                       LocalizedText = e.LocalizedText.Etc(50),
                                                   }).ToDynamic();

                new BasicExecute<DataLocalizationDN>(CodeLocalizationOperation.Save)
                {
                    AllowsNew = true,
                    Lite = false,
                    Execute = (e, _)=>{}
                }.Register();
            }
        }
    }
}
