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
using System.Reflection;
using Signum.Engine.Operations;
using Signum.Engine.Authorization;
using Signum.Entities.Authorization;

namespace Signum.Engine.Basics
{
    public static class CultureInfoLogic
    {
        public static CultureInfo ToCultureInfo(this CultureInfoDN ci)
        {
            if (ci == null)
                return null;
           
            return CultureInfoDNToCultureInfo.Value.TryGetC(ci);
        }

        internal static void AssertStarted(SchemaBuilder sb)
        {
            sb.AssertDefined(ReflectionTools.GetMethodInfo(() => CultureInfoLogic.Start(null, null)));
        }

        public static Func<CultureInfo, CultureInfo> CultureInfoModifier = ci => ci;

        public static ResetLazy<Dictionary<string, CultureInfoDN>> CultureInfoToEntity;
        public static ResetLazy<Dictionary<CultureInfoDN, CultureInfo>> CultureInfoDNToCultureInfo;

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

                CultureInfoToEntity = sb.GlobalLazy(() => Database.Query<CultureInfoDN>().ToDictionary(ci => ci.Name,
                    ci => ci),
                    invalidateWith: new InvalidateWith(typeof(CultureInfoDN)));

                CultureInfoDNToCultureInfo = sb.GlobalLazy(() => Database.Query<CultureInfoDN>().ToDictionary(ci => ci, 
                    ci => CultureInfoModifier(CultureInfo.GetCultureInfo(ci.Name))),
                    invalidateWith: new InvalidateWith(typeof(CultureInfoDN)));

                new Graph<CultureInfoDN>.Execute(CultureInfoOperation.Save)
                {
                    AllowsNew = true,
                    Lite = false,
                    Execute = (ci, _) => { },
                }.Register();

                sb.Schema.Synchronizing += Schema_Synchronizing;
            }
        }

        static SqlPreCommand Schema_Synchronizing(Replacements arg)
        {
            var cis = Database.Query<CultureInfoDN>().ToList();

            var table = Schema.Current.Table(typeof(CultureInfoDN));

            return cis.Select(c => table.UpdateSqlSync(c)).Combine(Spacing.Double);
        }

        public static CultureInfoDN ToCultureInfoDN(this CultureInfo ci)
        {
            return CultureInfoToEntity.Value.GetOrThrow(ci.Name);
        }

        public static IEnumerable<CultureInfo> ApplicationCultures
        {
            get
            {
                return CultureInfoDNToCultureInfo.Value.Values;
            }
        }

        public static IEnumerable<T> ForEachCulture<T>(Func<CultureInfoDN, T> func)
        {
            if (CultureInfoDNToCultureInfo.Value.Count == 0)
                throw new InvalidOperationException("No {0} found in the database".Formato(typeof(CultureInfoDN).Name));


            foreach (var c in CultureInfoDNToCultureInfo.Value)
            {
                using (CultureInfoUtils.ChangeBothCultures(c.Value))
                {
                    yield return func(c.Key);
                }
            }
        }

        public static CultureInfoDN GetCultureInfoDN(string cultureName)
        {
            return CultureInfoToEntity.Value.GetOrThrow(cultureName);
        }
    }
}
