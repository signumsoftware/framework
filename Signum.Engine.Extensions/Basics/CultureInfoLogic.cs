using System;
using System.Collections.Generic;
using System.Linq;
using Signum.Engine.Maps;
using Signum.Engine.DynamicQuery;
using Signum.Utilities.Reflection;
using Signum.Entities.Basics;
using System.Globalization;
using Signum.Utilities;
using System.Reflection;
using Signum.Engine.Operations;

namespace Signum.Engine.Basics
{
    public static class CultureInfoLogic
    {
        public static CultureInfo ToCultureInfo(this CultureInfoEntity ci)
        {
            return EntityToCultureInfo.Value.GetOrThrow(ci);
        }

        internal static void AssertStarted(SchemaBuilder sb)
        {
            sb.AssertDefined(ReflectionTools.GetMethodInfo(() => CultureInfoLogic.Start(null!)));
        }

        public static Func<CultureInfo, CultureInfo> CultureInfoModifier = ci => ci;

        public static ResetLazy<Dictionary<string, CultureInfoEntity>> CultureInfoToEntity = null!;
        public static ResetLazy<Dictionary<CultureInfoEntity, CultureInfo>> EntityToCultureInfo = null!;

        public static void Start(SchemaBuilder sb)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                sb.Include<CultureInfoEntity>()
                    .WithSave(CultureInfoOperation.Save)
                    .WithQuery(() => c => new
                    {
                        Entity = c,
                        c.Id,
                        c.Name,
                        c.EnglishName,
                        c.NativeName,
                    });

                CultureInfoToEntity = sb.GlobalLazy(() => Database.Query<CultureInfoEntity>().ToDictionary(ci => ci.Name,
                    ci => ci),
                    invalidateWith: new InvalidateWith(typeof(CultureInfoEntity)));

                EntityToCultureInfo = sb.GlobalLazy(() => Database.Query<CultureInfoEntity>().ToDictionary(ci => ci,
                    ci => CultureInfoModifier(CultureInfo.GetCultureInfo(ci.Name))),
                    invalidateWith: new InvalidateWith(typeof(CultureInfoEntity)));

                sb.Schema.Synchronizing += Schema_Synchronizing;
            }
        }

        static SqlPreCommand? Schema_Synchronizing(Replacements rep)
        {
            var cis = Database.Query<CultureInfoEntity>().ToList();

            var table = Schema.Current.Table(typeof(CultureInfoEntity));

            using (rep.WithReplacedDatabaseName())
                return cis.Select(c => table.UpdateSqlSync(c, ci => ci.Name == c.Name)).Combine(Spacing.Double);
        }

        public static CultureInfoEntity ToCultureInfoEntity(this CultureInfo ci)
        {
            return CultureInfoToEntity.Value.GetOrThrow(ci.Name);
        }

        public static CultureInfoEntity? TryGetCultureInfoEntity(this CultureInfo ci)
        {
            return CultureInfoToEntity.Value.TryGetC(ci.Name);
        }

        public static IEnumerable<CultureInfo> ApplicationCultures
        {
            get
            {
                return EntityToCultureInfo.Value.Values;
            }
        }

        public static IEnumerable<T> ForEachCulture<T>(Func<CultureInfoEntity, T> func)
        {
            if (EntityToCultureInfo.Value.Count == 0)
                throw new InvalidOperationException("No {0} found in the database".FormatWith(typeof(CultureInfoEntity).Name));


            foreach (var c in EntityToCultureInfo.Value)
            {
                using (CultureInfoUtils.ChangeBothCultures(c.Value))
                {
                    yield return func(c.Key);
                }
            }
        }

        public static CultureInfoEntity GetCultureInfoEntity(string cultureName)
        {
            return CultureInfoToEntity.Value.GetOrThrow(cultureName);
        }
    }
}
