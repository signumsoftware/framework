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

        public static bool ContainsRoute(PropertyRoute route)
        {
            var list = TraducibleRoutes.TryGetC(route.RootType);

            return list != null && list.Contains(route); 
        }

        public static List<TranslatedTypeSummary> TranslationInstancesStatus()
        {
            var cultures = TranslationLogic.CurrentCultureInfos(DefaultCulture);

            return (from type in TraducibleRoutes.Keys
                    from ci in cultures
                    select new TranslatedTypeSummary
                    {
                        Type = type,
                        CultureInfo = ci,
                        State = ci.IsNeutralCulture && ci.Name != DefaultCulture ? GetState(type, ci) : null,
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

            foreach (var pr in TraducibleRoutes.GetOrThrow(typeof(T)))
            {
                var selector = pr.GetLambdaExpression<T>();

                result.AddRange(
                    (from e in Database.Query<T>()
                     select KVP.Create(new LocalizedInstanceKey(pr, e.ToLite()), selector.Evaluate(e))).ToList());
            }

            return result;
        }

        public static Dictionary<CultureInfo, Dictionary<LocalizedInstanceKey, TranslatedInstanceDN>> TranslationsForType(Type type, CultureInfo culture)
        {
            return LocalizationCache.Value.Where(c => culture == null || c.Key.Equals(culture)).ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value.Where(a => a.Key.Route.RootType == type).ToDictionary());
        }

        static TranslatedSummaryState? GetState(Type type, CultureInfo ci)
        {
            using (HeavyProfiler.LogNoStackTrace("GetState", () => type.Name + " " + ci.Name))
            {
                if (!TraducibleRoutes.GetOrThrow(type).Any(pr => AnyNoTranslated(pr, ci)))
                    return TranslatedSummaryState.Completed;

                if (Database.Query<TranslatedInstanceDN>().Count(ti => ti.PropertyRoute.RootType == type.ToTypeDN() && ti.Culture == ci.ToCultureInfoDN()) == 0)
                    return TranslatedSummaryState.None;

                return TranslatedSummaryState.Pending;
            }
        }

        public static bool AnyNoTranslated(PropertyRoute pr, CultureInfo ci)
        {
            return giAnyNoTranslated.GetInvoker(pr.RootType)(pr, ci);
        }

        static GenericInvoker<Func<PropertyRoute, CultureInfo, bool>> giAnyNoTranslated =
            new GenericInvoker<Func<PropertyRoute, CultureInfo, bool>>((pr, ci) => AnyNoTranslated<IdentifiableEntity>(pr, ci));
        static bool AnyNoTranslated<T>(PropertyRoute pr, CultureInfo ci) where T : IdentifiableEntity
        {
            var exp = pr.GetLambdaExpression<T>();

            return (from e in Database.Query<T>()
                    let str = exp.Evaluate(e)
                    where str != null &&
                    !Database.Query<TranslatedInstanceDN>().Any(ti =>
                        ti.PropertyRoute.IsPropertyRoute(pr) &&
                        ti.Culture == ci.ToCultureInfoDN() &&
                        ti.OriginalText == str)
                    select e).Any();
        }

        public static void CleanTranslations(Type t)
        {
            var routes = TraducibleRoutes.GetOrThrow(t).Select(pr => pr.ToPropertyRouteDN()).Where(a => !a.IsNew).ToList();

            int deletedPr = Database.Query<TranslatedInstanceDN>().Where(a => a.PropertyRoute.RootType == t.ToTypeDN() && !routes.Contains(a.PropertyRoute)).UnsafeDelete();

            int deletedInstance = giRemoveTranslationsForMissingEntities.GetInvoker(t)();
        }

        static GenericInvoker<Func<int>> giRemoveTranslationsForMissingEntities = new GenericInvoker<Func<int>>(() => RemoveTranslationsForMissingEntities<IdentifiableEntity>());
        static int RemoveTranslationsForMissingEntities<T>() where T : IdentifiableEntity
        {
            return (from ti in Database.Query<TranslatedInstanceDN>()
                    where ti.PropertyRoute.RootType == typeof(T).ToTypeDN()
                    join e in Database.Query<T>().DefaultIfEmpty() on ti.Instance.Entity equals e
                    where e == null
                    select ti).UnsafeDelete();
        }


        public static string TranslatedField<T>(this T entity, Expression<Func<T, string>> property) where T : IdentifiableEntity
        {
            string fallbackString = TranslatedInstanceLogic.GetPropertyRouteAccesor(property)(entity);

            return entity.ToLite().TranslatedField(property, fallbackString);
        }

        public static string TranslatedField<T>(this Lite<T> lite, Expression<Func<T, string>> property, string fallbackString) where T : IdentifiableEntity
        {
            PropertyRoute route = PropertyRoute.Construct(Expression.Lambda<Func<T, object>>(property.Body, property.Parameters));

            return TranslatedField(lite, route, fallbackString);
        }

        public static string TranslatedField(Lite<IdentifiableEntity> lite, PropertyRoute route, string fallbackString)
        {
            var result = TranslatedInstanceLogic.GetTranslatedInstance(lite, route);

            if (result != null && result.OriginalText == fallbackString)
                return result.TranslatedText;

            return fallbackString;
        }

        public static TranslatedInstanceDN GetTranslatedInstance(Lite<IdentifiableEntity> lite, PropertyRoute route)
        {
            var key = new LocalizedInstanceKey(route, lite);

            var result = LocalizationCache.Value.TryGetC(CultureInfo.CurrentUICulture).TryGetC(key);

            if (result != null)
                return result;

            if (CultureInfo.CurrentUICulture.IsNeutralCulture)
                return null;

            result = LocalizationCache.Value.TryGetC(CultureInfo.CurrentUICulture.Parent).TryGetC(key);

            if (result != null)
                return result;

            return null;
        }


        public static T SaveTranslation<T>(this T entity, CultureInfoDN ci, Expression<Func<T, string>> propertyRoute, string translatedText)
            where T : IdentifiableEntity
        {
            entity.Save();

            if (translatedText.HasText())
                new TranslatedInstanceDN
                {
                    PropertyRoute = PropertyRoute.Construct(propertyRoute).ToPropertyRouteDN(),
                    Culture = ci,
                    TranslatedText = translatedText,
                    OriginalText = GetPropertyRouteAccesor(propertyRoute)(entity),
                    Instance = entity.ToLite(),
                }.Save();

            return entity;
        }


        static ConcurrentDictionary<LambdaExpression, Delegate> compiledExpressions = new ConcurrentDictionary<LambdaExpression, Delegate>(ExpressionComparer.GetComparer<LambdaExpression>());

        public static Func<T, string> GetPropertyRouteAccesor<T>(Expression<Func<T, string>> propertyRoute) where T:IdentifiableEntity
        {
            return (Func<T, string>)compiledExpressions.GetOrAdd(propertyRoute, ld => ld.Compile());
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
