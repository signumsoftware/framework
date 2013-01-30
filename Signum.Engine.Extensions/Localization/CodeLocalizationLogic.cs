using Signum.Engine.DynamicQuery;
using Signum.Engine.Maps;
using Signum.Engine.Operations;
using Signum.Entities.Localization;
using Signum.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Signum.Engine.Extensions.Localization
{
    public static class CodeLocalizationLogic
    {
        public static void Start(SchemaBuilder sb, DynamicQueryManager dqm)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                sb.Include<CodeLocalizationDN>();

                dqm[typeof(CodeLocalizationDN)] = (from e in Database.Query<CodeLocalizationDN>()
                                                   select new
                                                   {
                                                       Entity = e,
                                                       e.Id,
                                                       e.CodeType,
                                                       e.TypeName,
                                                       e.PropertyName,
                                                       LocalizedText = e.LocalizedText.Etc(50),
                                                   }).ToDynamic();

                new BasicExecute<CodeLocalizationDN>(CodeLocalizationOperation.Save)
                {
                    AllowsNew = true,
                    Lite = false,
                    Execute = (e, _)=>{}
                }.Register();
            }
        }
    }
}
