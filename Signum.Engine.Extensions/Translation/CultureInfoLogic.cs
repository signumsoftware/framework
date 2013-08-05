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

        public static ResetLazy<Dictionary<CultureInfo, CultureInfoDN>> CultureInfoToEntity;
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
                        CI = c.Name,
                        Description = c.DisplayName
                    });

                CultureInfoToEntity = sb.GlobalLazy(() => Database.Query<CultureInfoDN>().ToDictionary(ci => CultureInfo.GetCultureInfo(ci.Name)),
                    invalidateWith: new InvalidateWith(typeof(CultureInfoDN)));

                EntityToCultureInfo = sb.GlobalLazy(() => CultureInfoToEntity.Value.ToDictionary(a=>a.Value, a=>a.Key),
                    invalidateWith: new InvalidateWith(typeof(CultureInfoDN)));

                new Graph<CultureInfoDN>.Execute(CultureInfoOperation.Save)
                {
                    AllowsNew = true,
                    Lite = false,
                    Execute = (ci, _) => { },
                }.Register();

                PermissionAuthLogic.RegisterTypes(typeof(TranslationPermission));
            }
        }

        public static CultureInfo ToCultureInfo(this CultureInfoDN culture)
        {
            return EntityToCultureInfo.Value.GetOrThrow(culture);
        }

        public static CultureInfoDN ToCultureInfoDN(this CultureInfo culture)
        {
            return CultureInfoToEntity.Value.GetOrThrow(culture);
        }
        
        public static IEnumerable<CultureInfo> ApplicationCultures
        {
            get
            {
                return CultureInfoToEntity.Value.Keys;
            }
        }

        public static IEnumerable<T> ForEachCulture<T>(Func<CultureInfoDN, T> func)
        {
            foreach (var c in CultureInfoToEntity.Value)
            {
                using (Sync.ChangeBothCultures(c.Key))
                {
                    yield return func(c.Value);
                }
            }
        }
    }
}
