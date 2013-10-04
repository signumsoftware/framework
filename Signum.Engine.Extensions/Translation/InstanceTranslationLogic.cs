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
using Signum.Entities.Basics;
using System.Collections.Concurrent;
using Signum.Utilities.ExpressionTrees;

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
                sb.AddUniqueIndex<TranslatedInstanceDN>(ti => new { ti.Culture, ti.PropertyRoute, ti.Instance });

                DefaultCulture = defaultCulture;

                dqm.RegisterQuery(typeof(TranslatedInstanceDN), () =>
                    from e in Database.Query<TranslatedInstanceDN>()
                    select new
                    {
                        Entity = e,
                        e.Id,
                        e.Culture,
                        e.Instance,
                        e.PropertyRoute,
                        e.TranslatedText,
                        e.OriginalText,
                    });

                LocalizationCache = sb.GlobalLazy(() => Database.Query<TranslatedInstanceDN>()
                    .AgGroupToDictionary(a => a.Culture.ToCultureInfo(),
                    gr => gr.ToDictionary(a => new LocalizedInstanceKey(a.PropertyRoute.ToPropertyRoute(), a.Instance))),
                    new InvalidateWith(typeof(TranslatedInstanceDN)));
            }
        }

        public static void AddRoute<T, S>(Expression<Func<T, S>> propertyRoute) where T : IdentifiableEntity
        {
            AddRoute(PropertyRoute.Construct<T, S>(propertyRoute));
        }

        public static void AddRoute(PropertyRoute route)
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
            var cultures = TranslationLogic.CurrentCultureInfos(DefaultCulture); 

            return (from kvp in TraducibleRoutes
                    let original = FromEntities(kvp.Key)
                    from ci in cultures
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

        public static Dictionary<CultureInfo, Dictionary<LocalizedInstanceKey, TranslatedInstanceDN>> TranslationsForType(Type type, CultureInfo culture)
        {
            return LocalizationCache.Value.Where(c => culture == null || c.Key.Equals(culture)).ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value.Where(a => a.Key.Route.RootType == type).ToDictionary());
        }

        static TranslatedSummaryState? GetState(Dictionary<LocalizedInstanceKey, string> original, Dictionary<LocalizedInstanceKey, TranslatedInstanceDN> translations)
        {
            TranslatedSummaryState? result = null;

            foreach (var item in (from kvp in original
                                  where kvp.Value.HasText() 
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

        public static void CleanTranslations(Type t)
        {
            var routes = TraducibleRoutes.GetOrThrow(t).Select(pr => pr.ToPropertyRouteDN()).Where(a => !a.IsNew).ToList();

            Database.Query<TranslatedInstanceDN>().Where(a => a.PropertyRoute.Type == t.ToTypeDN() && !routes.Contains(a.PropertyRoute)).UnsafeDelete();

            giRemoveTranslationsForMissingEntities.GetInvoker(t).Invoke();
        }

        static GenericInvoker<Action> giRemoveTranslationsForMissingEntities = new GenericInvoker<Action>(() => RemoveTranslationsForMissingEntities<IdentifiableEntity>());
        static void RemoveTranslationsForMissingEntities<T>() where T : IdentifiableEntity
        {
            (from ti in Database.Query<TranslatedInstanceDN>()
             join e in Database.Query<T>().DefaultIfEmpty() on ti.Instance.Entity equals e
             where e == null
             select ti).UnsafeDelete();
        }

        public static string GetTranslation(Lite<IdentifiableEntity> lite, PropertyRoute route)
        {
            var key = new LocalizedInstanceKey(route, lite);

            var result = LocalizationCache.Value.TryGetC(CultureInfo.CurrentUICulture).TryGetC(key);

            if (result != null)
                return result.TranslatedText;

            if (CultureInfo.CurrentUICulture.IsNeutralCulture)
                return null;

            result = LocalizationCache.Value.TryGetC(CultureInfo.CurrentUICulture.Parent).TryGetC(key);

            if (result != null)
                return result.TranslatedText;

            return null;
        }

        static ConcurrentDictionary<LambdaExpression, Delegate> compiledExpressions = new ConcurrentDictionary<LambdaExpression, Delegate>(ExpressionComparer.GetComparer<LambdaExpression>());

        public T SaveTranslation<T>(this T entity, CultureInfo ci, Expression<Func<T, string>> propertyRoute, string translatedText)
            where T : IdentifiableEntity
        {
            Func<T, string> func = (Func<T, string>)compiledExpressions.GetOrAdd(propertyRoute, ld => ld.Compile());

            new TranslatedInstanceDN
            {
                PropertyRoute = PropertyRoute.Construct(propertyRoute).ToPropertyRouteDN(),
                Culture = ci.ToCultureInfoDN(),
                TranslatedText = translatedText,
                OriginalText = func(entity),
                Instance = entity.ToLite(),
            }.Save();

            return entity;
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

        public override string ToString()
        {
            return "{0} {1}".Formato(Route, Instance);
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
