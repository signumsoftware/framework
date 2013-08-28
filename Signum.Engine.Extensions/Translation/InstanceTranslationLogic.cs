using Signum.Engine.DynamicQuery;
using Signum.Engine.Maps;
using Signum.Engine.Operations;
using Signum.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Signum.Entities.Translation;
using Signum.Entities;
using Signum.Entities.Reflection;
using System.Globalization;
using Signum.Engine.Basics;
using System.Linq.Expressions;
using Signum.Utilities.Reflection;

namespace Signum.Engine.Translation
{
    public static class TranslatedInstanceLogic
    {
        public static string DefaultCulture { get; private set; }

        public static Dictionary<Type, List<PropertyRoute>> TraducibleRoutes = new Dictionary<Type, List<PropertyRoute>>();
        static ResetLazy<Dictionary<CultureInfo, Dictionary<LocalizedInstanceKey, TranslatedInstanceDN>>> LocalizationCache;

        public static void Start(SchemaBuilder sb, DynamicQueryManager dqm, string defaultCulture)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                sb.Include<TranslatedInstanceDN>();

                DefaultCulture = defaultCulture;

                dqm.RegisterQuery(typeof(TranslatedInstanceDN), () =>
                    from e in Database.Query<TranslatedInstanceDN>()
                    select new
                    {
                        Entity = e,
                        e.Id,
                        e.Culture,
                        Instance = e.Instance,
                    });

                new Graph<TranslatedInstanceDN>.Execute(TranslatedInstanceOperation.Save)
                {
                    AllowsNew = true,
                    Lite = false,
                    Execute = (e, _) => { }
                }.Register();

                new Graph<TranslatedInstanceDN>.Delete(TranslatedInstanceOperation.Delete)
                {
                    Lite = false,
                    Delete = (e, _) => e.Delete()
                }.Register();

                LocalizationCache = sb.GlobalLazy(() => Database.Query<TranslatedInstanceDN>()
                    .Select(li => new
                    {
                        CultureInfo = li.Culture.CultureInfo,
                        Key = new LocalizedInstanceKey(li.PropertyRoute.ToPropertyRoute(), li.Instance),
                        li,
                    })
                    .AgGroupToDictionary(a => a.CultureInfo,
                    gr => gr.ToDictionary(a => a.Key, a => a.li)),
                    new InvalidateWith(typeof(TranslatedInstanceDN)));
            }
        }

        
        public static void AddTraducibleRoute(PropertyRoute route)
        {
            if (route.PropertyRouteType != PropertyRouteType.FieldOrProperty)
                throw new InvalidOperationException("Routes of type {0} can not be traducibles".Formato(route.PropertyRouteType));

            if (route.Type != typeof(string))
                throw new InvalidOperationException("Only string routes can be traducibles");

            if (route.FollowC(a => a.Parent).Any(a => a.PropertyRouteType == PropertyRouteType.MListItems))
                throw new NotImplementedException("MList elements are not traducible yet");

            TraducibleRoutes.GetOrCreate(route.RootType).Add(route); 
        }

      

        public static List<TranslatedTypeSummary> TranslationInstancesStatus()
        {
            return (from kvp in TraducibleRoutes
                    let original = FromEntities(kvp.Key)
                    from ci in CultureInfoLogic.CultureInfos(DefaultCulture)
                    select new TranslatedTypeSummary
                    {
                        Type = kvp.Key,
                        CultureInfo = ci,
                        State = GetState(original, LocalizationCache.Value.TryGetC(ci)),
                    }).ToList();

        }

        public static Dictionary<LocalizedInstanceKey, string> FromEntities(Type type)
        {
            return giFromEntities.GetInvoker(type)(); 
        }

        static GenericInvoker<Func<Dictionary<LocalizedInstanceKey, string>>> giFromEntities = 
            new GenericInvoker<Func<Dictionary<LocalizedInstanceKey, string>>>(() => FromEntities<IdentifiableEntity>());
        static Dictionary<LocalizedInstanceKey, string> FromEntities<T>() where T : IdentifiableEntity
        {
            Dictionary<LocalizedInstanceKey, string> result = new Dictionary<LocalizedInstanceKey, string>();

            ParameterExpression pe = Expression.Parameter(typeof(T));
            foreach (var pr in TraducibleRoutes.GetOrThrow(typeof(T)))
            {
                Expression exp = pe;

                foreach (var p in pr.FollowC(a => a.Parent).Reverse().Skip(1))
                    exp = Expression.Property(exp, p.PropertyInfo);

                var selector = Expression.Lambda<Func<T, string>>(exp, pe);

                result.AddRange(Database.Query<T>().Select(e => KVP.Create(new LocalizedInstanceKey(pr, e.ToLite()), selector.Evaluate(e))).ToList());
            }

            return result;
        }

        public static Dictionary<CultureInfo, Dictionary<LocalizedInstanceKey, TranslatedInstanceDN>> TranslationsForType(Type type)
        {
            return LocalizationCache.Value.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value.Where(a => a.Key.Route.RootType == type).ToDictionary());
        }

        static TranslatedSummaryState? GetState(Dictionary<LocalizedInstanceKey, string> original, Dictionary<LocalizedInstanceKey, TranslatedInstanceDN> translations)
        {
            TranslatedSummaryState? result = null;

            foreach (var item in (from kvp in original
                                  let t = translations.TryGetC(kvp.Key)
                                  select t != null && t.OriginalText == kvp.Value))
            {
                if (result == null)
                {
                    result = item ? TranslatedSummaryState.Completed : TranslatedSummaryState.None;
                }
                else
                {
                    if (result.Value == TranslatedSummaryState.Completed)
                    {
                        if (!item)
                            return TranslatedSummaryState.Pending;
                    }
                    else
                    {
                        if (item)
                            return TranslatedSummaryState.Pending;
                    }
                }
            }

            return result;
        }
    }

    public struct LocalizedInstanceKey : IEquatable<LocalizedInstanceKey>
    {
        public readonly PropertyRoute Route;
        public readonly Lite<IdentifiableEntity> Instance;

        public LocalizedInstanceKey(PropertyRoute route, Lite<IdentifiableEntity> instance)
        {
            if (route == null) throw new ArgumentNullException("route");
            if (instance == null) throw new ArgumentNullException("entity");

            this.Route = route;
            this.Instance = instance;
        }

        public bool Equals(LocalizedInstanceKey other)
        {
            return
                Route.Equals(other.Route) &&
                Instance.Equals(other.Instance);
        }

        public override int GetHashCode()
        {
            return Route.GetHashCode() ^ Instance.GetHashCode();
        }
    }

    public class TranslatedTypeSummary
    {
        public Type Type;
        public CultureInfo CultureInfo;
        public TranslatedSummaryState? State;
    }

    public enum TranslatedSummaryState
    {
        Completed,
        Pending,
        None,
    }
}
