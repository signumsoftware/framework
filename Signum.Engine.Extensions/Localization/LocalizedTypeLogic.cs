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
    public static class LocalizedTypeLogic
    {
        public static void Start(SchemaBuilder sb, DynamicQueryManager dqm)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                sb.Include<LocalizedTypeDN>();

                dqm[typeof(LocalizedTypeDN)] = (from e in Database.Query<LocalizedTypeDN>()
                                                select new
                                                {
                                                    Entity = e,
                                                    e.Id,
                                                    e.TypeName,
                                                    e.SingularName,
                                                    e.PluralName,
                                                    e.Gender,
                                                }).ToDynamic();

                dqm[typeof(LocalizedPropertyDN)] = (from e in Database.Query<LocalizedTypeDN>()
                                                    from p in e.Properties
                                                    select new
                                                    {
                                                        Entity = e,
                                                        e.Id,
                                                        e.TypeName,
                                                        p.PropertyName,
                                                        p.LocalizedText,
                                                    }).ToDynamic();

                new BasicExecute<LocalizedTypeDN>(LocalizedTypeOperation.Save)
                {
                    AllowsNew = true,
                    Lite = false,
                    Execute = (e, _)=>{}
                }.Register();
            }
        }
    }
}
