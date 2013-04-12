using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using Signum.Engine.DynamicQuery;
using Signum.Engine.Maps;
using Signum.Engine.Operations;
using Signum.Entities;
using Signum.Entities.Basics;
using Signum.Entities.Translation;
using Signum.Utilities;

namespace Signum.Engine.Translation
{
    public static class TranslationLogic
    {
        static Expression<Func<IUserDN, TranslatorDN>> TranslatorExpression =
             user => Database.Query<TranslatorDN>().SingleOrDefault(a => a.User.RefersTo(user));
        public static TranslatorDN Translator(this IUserDN entity)
        {
            return TranslatorExpression.Evaluate(entity);
        }

        public static void Start(SchemaBuilder sb, DynamicQueryManager dqm)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                sb.Include<TranslatorDN>();

                dqm.RegisterQuery(typeof(TranslatorDN), () =>
                    from e in Database.Query<TranslatorDN>()
                    select new
                    {
                        Entity = e,
                        e.Id,
                        e.User,
                        Cultures = e.Cultures.Count,
                    });

                dqm.RegisterExpression((IUserDN e) => e.Translator());

                new BasicExecute<TranslatorDN>(TranslatorOperation.Save)
                {
                    AllowsNew = true,
                    Lite = false,
                    Execute = (e, _) => { }
                }.Register();

                new BasicDelete<TranslatorDN>(TranslatorOperation.Delete)
                {
                    Delete = (e, _) => { e.Delete(); }
                }.Register();
            }
        }
    }
}
