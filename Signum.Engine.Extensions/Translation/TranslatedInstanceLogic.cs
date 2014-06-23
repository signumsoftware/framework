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
using System.IO;
using Signum.Engine.Excel;

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
                    gr => gr.ToDictionary(a => new LocalizedInstanceKey(a.PropertyRoute.ToPropertyRoute(), a.Instance, a.RowId))),
                    new InvalidateWith(typeof(TranslatedInstanceDN)));

                sb.Schema.Initializing[InitLevel.Level0SyncEntities] += () =>
                {
                    var s = Schema.Current;

                    var prs = (from t in s.Tables.Keys
                             from pr in PropertyRoute.GenerateRoutes(t)
                             where pr.PropertyRouteType == PropertyRouteType.FieldOrProperty && pr.FieldInfo != null && pr.FieldInfo.FieldType == typeof(string)
                             && s.Settings.FieldAttributes(pr).OfType<TranslateFieldAttribute>().Any() && 
                             !s.Settings.FieldAttributes(pr).OfType<IgnoreAttribute>().Any()
                             select pr).ToList();

                    foreach (var pr in prs)
                    {
                        AddRoute(pr);
                    }

                };
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
            Dictionary<LocalizedInstanceKey, string> result = null;

            foreach (var pr in TraducibleRoutes.GetOrThrow(type))
            {
                var mlist = pr.GetMListItemsRoute();

                var dic = mlist == null ?
                    giFromRoute.GetInvoker(type)(pr) :
                    giFromRouteMList.GetInvoker(type, mlist.Type)(pr);

                if (result == null)
                    result = dic;
                else
                    result.AddRange(dic);
            }

            return result;
        }

        static GenericInvoker<Func<PropertyRoute, Dictionary<LocalizedInstanceKey, string>>> giFromRoute =
            new GenericInvoker<Func<PropertyRoute, Dictionary<LocalizedInstanceKey, string>>>(pr => FromRoute<IdentifiableEntity>(pr));
        static Dictionary<LocalizedInstanceKey, string> FromRoute<T>(PropertyRoute pr) where T : IdentifiableEntity
        {
            var selector = pr.GetLambdaExpression<T, string>();

            return (from e in Database.Query<T>()
                    select KVP.Create(new LocalizedInstanceKey(pr, e.ToLite(), null), selector.Evaluate(e))).ToDictionary();
        }

        static GenericInvoker<Func<PropertyRoute, Dictionary<LocalizedInstanceKey, string>>> giFromRouteMList =
            new GenericInvoker<Func<PropertyRoute, Dictionary<LocalizedInstanceKey, string>>>(pr => FromRouteMList<IdentifiableEntity, string>(pr));
        static Dictionary<LocalizedInstanceKey, string> FromRouteMList<T, M>(PropertyRoute pr) where T : IdentifiableEntity
        {
            var mListProperty = pr.GetMListItemsRoute().Parent.GetLambdaExpression<T, MList<M>>();
            var selector = pr.GetLambdaExpression<M, string>();

            return (from mle in Database.MListQuery(mListProperty)
                    select KVP.Create(new LocalizedInstanceKey(pr, mle.Parent.ToLite(), mle.RowId), selector.Evaluate(mle.Element))).ToDictionary();
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
            var mlist = pr.GetMListItemsRoute();

            if (mlist == null)
                return giAnyNoTranslated.GetInvoker(pr.RootType)(pr, ci);

            return giAnyNoTranslatedMList.GetInvoker(pr.RootType, mlist.Type)(pr, ci); 
        }

        static GenericInvoker<Func<PropertyRoute, CultureInfo, bool>> giAnyNoTranslated =
            new GenericInvoker<Func<PropertyRoute, CultureInfo, bool>>((pr, ci) => AnyNoTranslated<IdentifiableEntity>(pr, ci));
        static bool AnyNoTranslated<T>(PropertyRoute pr, CultureInfo ci) where T : IdentifiableEntity
        {
            var exp = pr.GetLambdaExpression<T, string>();

            return (from e in Database.Query<T>()
                    let str = exp.Evaluate(e)
                    where str != null &&
                    !Database.Query<TranslatedInstanceDN>().Any(ti =>
                        ti.Instance.RefersTo(e) &
                        ti.PropertyRoute.IsPropertyRoute(pr) &&
                        ti.Culture == ci.ToCultureInfoDN() &&
                        ti.OriginalText == str)
                    select e).Any();
        }

        static GenericInvoker<Func<PropertyRoute, CultureInfo, bool>> giAnyNoTranslatedMList =
           new GenericInvoker<Func<PropertyRoute, CultureInfo, bool>>((pr, ci) => AnyNoTranslatedMList<IdentifiableEntity, string>(pr, ci));
        static bool AnyNoTranslatedMList<T, M>(PropertyRoute pr, CultureInfo ci) where T : IdentifiableEntity
        {
            var mListProperty = pr.GetMListItemsRoute().Parent.GetLambdaExpression<T, MList<M>>();

            var exp = pr.GetLambdaExpression<M, string>();

            return (from mle in Database.MListQuery(mListProperty)
                    let str = exp.Evaluate(mle.Element)
                    where str != null &&
                    !Database.Query<TranslatedInstanceDN>().Any(ti =>
                        ti.Instance.RefersTo(mle.Parent) &&
                        ti.PropertyRoute.IsPropertyRoute(pr) &&
                        ti.RowId == mle.RowId &&
                        ti.Culture == ci.ToCultureInfoDN() &&
                        ti.OriginalText == str)
                    select mle).Any();
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

        public static IEnumerable<TranslatableElement<T>> TranslatedMList<E, T>(this E entity, Expression<Func<E, MList<T>>> mlistProperty) where E : IdentifiableEntity
        {
            var mlist = GetPropertyRouteAccesor(mlistProperty);

            PropertyRoute route = PropertyRoute.Construct(mlistProperty).Add("Item");

            var lite = entity.ToLite();

            foreach (var item in ((IMListPrivate<T>)mlist(entity)).InnerList)
            {
                yield return new TranslatableElement<T>(lite, route, item);
            }
        }

        public static string TranslatedElement<T>(this TranslatableElement<T> element, Expression<Func<T, string>> property)
        {
            string fallback = GetPropertyRouteAccesor(property)(element.Value);
            
            PropertyRoute route = element.ElementRoute.Continue(property); 

            return TranslatedField(element.Lite, route, element.RowId, fallback);
        }

        public static string TranslatedField<T>(this Lite<T> lite, Expression<Func<T, string>> property, string fallbackString) where T : IdentifiableEntity
        {
            PropertyRoute route = PropertyRoute.Construct(property);

            return TranslatedField(lite, route, fallbackString);
        }

        public static string TranslatedField(Lite<IdentifiableEntity> lite, PropertyRoute route, string fallbackString)
        {
            return TranslatedField(lite, route, null, fallbackString);
        }

        public static string TranslatedField(Lite<IdentifiableEntity> lite, PropertyRoute route, int? rowId, string fallbackString)
        {
            var result = TranslatedInstanceLogic.GetTranslatedInstance(lite, route, rowId);

            if (result != null && result.OriginalText == fallbackString)
                return result.TranslatedText;

            return fallbackString;
        }


        public static TranslatedInstanceDN GetTranslatedInstance(Lite<IdentifiableEntity> lite, PropertyRoute route, int? rowId)
        {
            var hastMList = route.GetMListItemsRoute() != null;

            if (hastMList != rowId.HasValue)
            {
                if (rowId.HasValue)
                    throw new InvalidOperationException("Route {0} has not MList so rowId should be null".Formato(route));
                else
                    throw new InvalidOperationException("Route {0} has MList so rowId should have a value".Formato(route));

                if (route.Type != lite.EntityType)
                    throw new InvalidOperationException("Route {0} belongs to type {1}, not {2}".Formato(route, route.RootType.TypeName(), lite.EntityType.TypeName()));
            }

            var key = new LocalizedInstanceKey(route, lite, rowId);

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

        public static Func<T, R> GetPropertyRouteAccesor<T, R>(Expression<Func<T, R>> propertyRoute)
        {
            return (Func<T, R>)compiledExpressions.GetOrAdd(propertyRoute, ld => ld.Compile());
        }

        public static byte[] GetExcelFile(Type t, CultureInfo c)
        {
            var result = TranslatedInstanceLogic.TranslationsForType(t, c).Single().Value;

            var list = result
                .OrderBy(a=>a.Key.Instance.Id)
                .ThenBy(a=>a.Key.Route.PropertyInfo.MetadataToken).Select(r => new ExcelRow
            {
                Instance = r.Key.Instance.Key(),
                Path = r.Key.Route.PropertyString(),
                RowId = r.Key.RowId,
                Original = r.Value.OriginalText,
                Translated = r.Value.TranslatedText
            }).ToList();

            return PlainExcelGenerator.WritePlainExcel<ExcelRow>(list);
        }

        public static byte[] GetSyncExcelFile(List<InstanceChanges> changes, CultureInfo master, CultureInfo target)
        {
            var list = (from ic in changes
                        from pr in ic.RouteConflicts
                        orderby ic.Instance, pr.Key.Item1.PropertyString(), pr.Key.Item2
                        select new ExcelRow
                        {
                            Instance = ic.Instance.Key(),
                            Path = pr.Key.Item1.PropertyString(),
                            RowId = pr.Key.Item2,
                            Original = pr.Value.GetOrThrow(master).Original,
                            Translated = null
                        }).ToList();

            return PlainExcelGenerator.WritePlainExcel<ExcelRow>(list);
        }

        public static void SaveExcelFile(Stream stream, Type type, CultureInfo culture)
        {
            var records = PlainExcelGenerator.ReadPlainExcel(stream, cellValues => new TranslationRecord
            {
                 Culture = culture,
                 Key = new LocalizedInstanceKey(PropertyRoute.Parse(type, cellValues[1]), Lite.Parse<IdentifiableEntity>(cellValues[0]), cellValues[2].DefaultText(null).Try(int.Parse)),
                 OriginalText = cellValues[3],
                 TranslatedText = cellValues[4]
            });

            SaveRecords(records, type, culture);
        }


        public static List<InstanceChanges> GetInstanceChanges(Type type, CultureInfo targetCulture, List<CultureInfo> cultures)
        {
            CultureInfo masterCulture = CultureInfo.GetCultureInfo(TranslatedInstanceLogic.DefaultCulture);

            Dictionary<CultureInfo, Dictionary<LocalizedInstanceKey, TranslatedInstanceDN>> support = TranslatedInstanceLogic.TranslationsForType(type, culture: null);

            Dictionary<LocalizedInstanceKey, TranslatedInstanceDN> target = support.TryGetC(targetCulture);

            var instances = TranslatedInstanceLogic.FromEntities(type).GroupBy(a => a.Key.Instance).Select(gr =>
            {
                var routeConflicts = (from kvp in gr
                                      let t = target.TryGetC(kvp.Key)
                                      where kvp.Value.HasText() && (t == null || t.OriginalText != kvp.Value)
                                      select KVP.Create(kvp.Key, kvp.Value)).ToDictionary();

                if (routeConflicts.IsEmpty())
                    return null;

                var result = (from rc in routeConflicts
                              from c in cultures
                              let str = c.Equals(masterCulture) ? rc.Value : support.TryGetC(c).TryGetC(rc.Key).Try(a => a.TranslatedText)
                              where str.HasItems()
                              select new
                              {
                                  rc.Key.Route,
                                  rc.Key.RowId,
                                  Culture = c,
                                  Conflict = new PropertyRouteConflict { Original = str, AutomaticTranslation = null }
                              }).AgGroupToDictionary(a =>Tuple.Create(a.Route, a.RowId), g => g.ToDictionary(a => a.Culture, a => a.Conflict));

                return new InstanceChanges
                {
                    Instance = gr.Key,
                    RouteConflicts = result
                };

            }).NotNull().ToList();
            return instances;
        }



        public static void SaveRecords(List<TranslationRecord> records, Type t, CultureInfo c)
        {
            Dictionary<Tuple<CultureInfo, LocalizedInstanceKey>, TranslationRecord> should = records.Where(a => a.TranslatedText.HasText())
                .ToDictionary(a => Tuple.Create(a.Culture, a.Key));

            Dictionary<Tuple<CultureInfo, LocalizedInstanceKey>, TranslatedInstanceDN> current =
                (from ci in TranslatedInstanceLogic.TranslationsForType(t, c)
                 from key in ci.Value
                 select KVP.Create(Tuple.Create(ci.Key, key.Key), key.Value)).ToDictionary();

            using (Transaction tr = new Transaction())
            {
                Dictionary<PropertyRoute, PropertyRouteDN> routes = should.Keys.Select(a => a.Item2.Route).Distinct().ToDictionary(a => a, a => a.ToPropertyRouteDN());

                Synchronizer.Synchronize(
                    should,
                    current,
                    (k, n) => new TranslatedInstanceDN
                    {
                        Culture = n.Culture.ToCultureInfoDN(),
                        PropertyRoute = routes.GetOrThrow(n.Key.Route),
                        Instance = n.Key.Instance,
                        OriginalText = n.OriginalText,
                        TranslatedText = n.TranslatedText,
                    }.Save(),
                    (k, o) => { },
                    (k, n, o) =>
                    {
                        if (!n.TranslatedText.HasText())
                        {
                            o.Delete();
                        }
                        else if (o.TranslatedText != n.TranslatedText || o.OriginalText != n.OriginalText)
                        {
                            var r = o.ToLite().Retrieve();
                            r.OriginalText = n.OriginalText;
                            r.TranslatedText = n.TranslatedText;
                            r.Save();
                        }
                    });

                tr.Commit();
            }
        }

      
    }

    public struct TranslatableElement<T>
    {
        public readonly Lite<IdentifiableEntity> Lite;
        public readonly PropertyRoute ElementRoute;
        public readonly T Value;
        public readonly int RowId;

        internal TranslatableElement(Lite<IdentifiableEntity> entity, PropertyRoute route, MList<T>.RowIdValue item)
        {
            this.Lite = entity;
            this.ElementRoute = route;
            this.Value = item.Value;
            this.RowId = item.RowId.Value;
        }
    }

    public class TranslationRecord
    {
        public CultureInfo Culture;
        public LocalizedInstanceKey Key;
        public string TranslatedText;
        public string OriginalText;

        public override string ToString()
        {
            return "{0} {1} {2} -> {3}".Formato(Culture, Key.Instance, Key.Route, TranslatedText);
        }
    }

    public class InstanceChanges
    {
        public Lite<IdentifiableEntity> Instance { get; set; }

        public Dictionary<Tuple<PropertyRoute, int?>, Dictionary<CultureInfo, PropertyRouteConflict>> RouteConflicts { get; set; }

        public override string ToString()
        {
            return "Changes for {0}".Formato(Instance);
        }
    }

    public class PropertyRouteConflict
    {
        public string Original;
        public string AutomaticTranslation;

        public override string ToString()
        {
            return "Conflict {0} -> {1}".Formato(Original, AutomaticTranslation);
        }
    }

    class ExcelRow
    {
        public string Instance; 
        public string Path;
        public int? RowId; 
        public string Original; 
        public string Translated; 
    }

    public struct LocalizedInstanceKey : IEquatable<LocalizedInstanceKey>
    {
        public readonly PropertyRoute Route;
        public readonly Lite<IdentifiableEntity> Instance;
        public readonly int? RowId; 

        public LocalizedInstanceKey(PropertyRoute route, Lite<IdentifiableEntity> instance, int? rowId)
        {
            if (route == null) throw new ArgumentNullException("route");
            if (instance == null) throw new ArgumentNullException("entity");

            this.Route = route;
            this.Instance = instance;
            this.RowId = rowId;
        }

        public bool Equals(LocalizedInstanceKey other)
        {
            return
                Route.Equals(other.Route) &&
                Instance.Equals(other.Instance) &&
                RowId.Equals(other.RowId);
        }

        public override int GetHashCode()
        {
            return Route.GetHashCode() ^ Instance.GetHashCode() ^ RowId.GetHashCode();
        }

        public override string ToString()
        {
            var result = "{0} {1}".Formato(Route, Instance);

            if (RowId.HasValue)
                result += " " + RowId;

            return result;
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
