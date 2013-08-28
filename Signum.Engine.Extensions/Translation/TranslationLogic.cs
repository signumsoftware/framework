using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using Signum.Engine.DynamicQuery;
using Signum.Engine.Maps;
using Signum.Engine.Operations;
using Signum.Entities;
using Signum.Entities.Authorization;
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
                CultureInfoLogic.AssertStarted(sb); 

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

                new Graph<TranslatorDN>.Execute(TranslatorOperation.Save)
                {
                    AllowsNew = true,
                    Lite = false,
                    Execute = (e, _) => { }
                }.Register();

                new Graph<TranslatorDN>.Delete(TranslatorOperation.Delete)
                {
                    Delete = (e, _) => { e.Delete(); }
                }.Register();
            }
        }

        public static List<CultureInfo> CurrentCultureInfos(string defaultCulture)
        {
            var cultures = CultureInfoLogic.ApplicationCultures;

            TranslatorDN tr = UserDN.Current.Translator();

            if (tr != null)
                cultures = cultures.Where(ci => ci.Name == defaultCulture || tr.Cultures.Any(tc => tc.Culture.CultureInfo == ci));

            return cultures.OrderByDescending(a => a.Name == defaultCulture).ThenBy(a => a.Name).ToList();
        }
    }
}
