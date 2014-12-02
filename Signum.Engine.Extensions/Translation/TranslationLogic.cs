using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Signum.Engine.Authorization;
using Signum.Engine.Basics;
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
        static Expression<Func<IUserEntity, TranslatorUserEntity>> TranslatorUserExpression =
             user => Database.Query<TranslatorUserEntity>().SingleOrDefault(a => a.User.RefersTo(user));
        public static TranslatorUserEntity TranslatorUser(this IUserEntity entity)
        {
            return TranslatorUserExpression.Evaluate(entity);
        }

  
        public static void Start(SchemaBuilder sb, DynamicQueryManager dqm)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                CultureInfoLogic.AssertStarted(sb);

                sb.Include<TranslatorUserEntity>();

                dqm.RegisterQuery(typeof(TranslatorUserEntity), () =>
                    from e in Database.Query<TranslatorUserEntity>()
                    select new
                    {
                        Entity = e,
                        e.Id,
                        e.User,
                        Cultures = e.Cultures.Count,
                    });


                PermissionAuthLogic.RegisterTypes(typeof(TranslationPermission));

                dqm.RegisterExpression((IUserEntity e) => e.TranslatorUser(), () => typeof(TranslatorUserEntity).NiceName());

                new Graph<TranslatorUserEntity>.Execute(TranslatorUserOperation.Save)
                {
                    AllowsNew = true,
                    Lite = false,
                    Execute = (e, _) => { }
                }.Register();

                new Graph<TranslatorUserEntity>.Delete(TranslatorUserOperation.Delete)
                {
                    Delete = (e, _) => { e.Delete(); }
                }.Register();
            }
        }

        public static List<CultureInfo> CurrentCultureInfos(CultureInfo defaultCulture)
        {
            var cultures = CultureInfoLogic.ApplicationCultures;

            if (Schema.Current.Tables.ContainsKey(typeof(TranslatorUserEntity)))
            {
                TranslatorUserEntity tr = UserEntity.Current.TranslatorUser();

                if (tr != null)
                    cultures = cultures.Where(ci => ci.Name == defaultCulture.Name || tr.Cultures.Any(tc => tc.Culture.ToCultureInfo() == ci));
            }

            return cultures.OrderByDescending(a => a.Name == defaultCulture.Name).ThenBy(a => a.Name).ToList();
        }


    }
}
