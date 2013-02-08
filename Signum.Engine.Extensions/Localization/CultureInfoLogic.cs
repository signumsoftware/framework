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
using Signum.Entities.Localization;

namespace Signum.Engine.Localization
{
    public static class CultureInfoLogic
    {
        internal static void AssertStarted(SchemaBuilder sb)
        {
            sb.AssertDefined(ReflectionTools.GetMethodInfo(() => CultureInfoLogic.Start(null, null, null)));
        }

        public static void Start(SchemaBuilder sb, DynamicQueryManager dqm, List<CultureInfo> cultures)
        {
            sb.Include<CultureInfoDN>();

            dqm.RegisterQuery(typeof(CultureInfoDN), () =>
                from c in Database.Query<CultureInfoDN>()
                select new
                {
                    Entity = c,
                    c.Id,
                    CI = c.Name,
                    Description = c.DisplayName
                });

            cultures.AddRange(cultures);

            sb.Schema.Initializing[InitLevel.Level2NormalEntities] += Schema_Initializing;
            sb.Schema.Generating += new Func<SqlPreCommand>(Schema_Generating);
            sb.Schema.Synchronizing += new Func<Replacements, SqlPreCommand>(Schema_Synchronizing);
        }

        static Dictionary<CultureInfo, CultureInfoDN> cultureInfoToDN;

        public static CultureInfoDN ToDN(this CultureInfo ci)
        {
            return cultureInfoToDN.GetOrThrow(ci, "The culture info {0} is not loaded in the application");
        }

        static void Schema_Initializing()
        {
            var cultures = Database.RetrieveAll<CultureInfoDN>();

            cultureInfoToDN = EnumerableExtensions.JoinStrict(
                cultures,
                applicationCultures,
                ciDN => ciDN.Name,
                ci => ci.Name,
                (ciDN, ci) => KVP.Create(ci, ciDN),
                "caching CultureInfos").ToDictionary();
        }

        static readonly string cultureInfoReplacmentKey = "";

        static SqlPreCommand Schema_Synchronizing(Replacements replacements)
        {
            Table table = Schema.Current.Table<CultureInfoDN>();

            var should = GetEntities().ToDictionary(ci => ci.Name);
            var old = Administrator.TryRetrieveAll<CultureInfoDN>(replacements).ToDictionary(ci => ci.Name);

            replacements.AskForReplacements(old.Keys.ToHashSet(), should.Keys.ToHashSet(), cultureInfoReplacmentKey);
            var current = replacements.ApplyReplacementsToOld(old, cultureInfoReplacmentKey);

            return Synchronizer.SynchronizeScript(should, current,
                (tn, s) => table.InsertSqlSync(s),
                (tn, c) => table.DeleteSqlSync(c),
                (tn, s, c) =>
                {
                    c.Name = s.Name;
                    c.DisplayName = s.DisplayName;
                    return table.UpdateSqlSync(c);
                },
                Spacing.Double);
        }

        static SqlPreCommand Schema_Generating()
        {
            Table table = Schema.Current.Table<CultureInfoDN>();

            return (from c in GetEntities()
                    select table.InsertSqlSync(c)).Combine(Spacing.Simple);
        }

        public static List<CultureInfoDN> GetEntities()
        {
            return applicationCultures.Select(c => new CultureInfoDN(c)).ToList();
        }

        static List<CultureInfo> applicationCultures;
        public static List<CultureInfo> ApplicationCultures
        {
            get
            {
                return applicationCultures;
            }
        }

        public static IEnumerable<T> ForEachCulture<T>(Func<T> func)
        {
            foreach (var c in applicationCultures)
            {
                using (Sync.ChangeBothCultures(c))
                {
                    yield return func();
                }
            }
        }
    }
}
