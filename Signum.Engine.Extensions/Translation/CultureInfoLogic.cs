using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Engine.Maps;
using Signum.Engine.DynamicQuery;
using Signum.Utilities.Reflection;
using Signum.Entities.Basics;
using System.Linq.Expressions;
using System.Globalization;
using Signum.Utilities;
using Signum.Entities.Translation;
using System.Reflection;
using Signum.Engine.Operations;
using Signum.Engine.Authorization;

namespace Signum.Engine.Translation
{
    public static class CultureInfoLogic
    {
        internal static void AssertStarted(SchemaBuilder sb)
        {
            sb.AssertDefined(ReflectionTools.GetMethodInfo(() => CultureInfoLogic.Start(null, null)));
        }

        public static ResetLazy<Dictionary<CultureInfoDN, CultureInfo>> EntityToCultureInfo;

        public static void Start(SchemaBuilder sb, DynamicQueryManager dqm)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                sb.Include<CultureInfoDN>();

                dqm.RegisterQuery(typeof(CultureInfoDN), () =>
                    from c in Database.Query<CultureInfoDN>()
                    select new
                    {
                        Entity = c,
                        c.Id,
                        c.Name,
                        c.EnglishName,
                        c.NativeName,
                    });

                EntityToCultureInfo = sb.GlobalLazy(() => Database.Query<CultureInfoDN>().ToDictionary(ci=>ci, ci => CultureInfo.GetCultureInfo(ci.Name)),
                    invalidateWith: new InvalidateWith(typeof(CultureInfoDN)));

                new Graph<CultureInfoDN>.Execute(CultureInfoOperation.Save)
                {
                    AllowsNew = true,
                    Lite = false,
                    Execute = (ci, _) => { },
                }.Register();

                sb.Schema.Synchronizing += Schema_Synchronizing;

                PermissionAuthLogic.RegisterTypes(typeof(TranslationPermission));
            }
        }

        static SqlPreCommand Schema_Synchronizing(Replacements arg)
        {
            var cis = Database.Query<CultureInfoDN>().ToList();

            var table = Schema.Current.Table(typeof(CultureInfoDN));

            return cis.Select(c => table.UpdateSqlSync(c)).Combine(Spacing.Double);
        }

        public static CultureInfo ToCultureInfo(this CultureInfoDN culture)
        {
            return EntityToCultureInfo.Value.GetOrThrow(culture);
        }

        public static IEnumerable<CultureInfo> ApplicationCultures
        {
            get
            {
                return EntityToCultureInfo.Value.Values;
            }
        }

        public static IEnumerable<T> ForEachCulture<T>(Func<CultureInfoDN, T> func)
        {
            foreach (var c in EntityToCultureInfo.Value)
            {
                using (Sync.ChangeBothCultures(c.Value))
                {
                    yield return func(c.Key);
                }
            }
        }
    }
}
