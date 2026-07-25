using System.Globalization;
using Signum.Utilities.Reflection;
using System.IO;
using Signum.Engine.Sync;
using Signum.Excel;
using System.Collections.Frozen;
using System.Runtime.CompilerServices;

namespace Signum.Translation.Instances;

public static class TranslatedInstanceLogic
{
    static Func<CultureInfo> getDefaultCulture = null!;
    public static CultureInfo DefaultCulture { get { return getDefaultCulture(); } }

    static ResetLazy<FrozenDictionary<CultureInfo, FrozenDictionary<LocalizedInstanceKey, TranslatedInstanceEntity>>> LocalizationCache = null!;

    /// <summary>
    /// Optional per-type filters restricting which instances are considered for translation (used by Status and Sync).
    /// Register in the application Starter, e.g:
    /// TranslatedInstanceLogic.RegisterFilter&lt;UserQueryEntity&gt;(uq => uq.Dashboards().Any() || uq.Toolbars().Any());
    /// </summary>
    public static Dictionary<Type, LambdaExpression> InstanceFilters = new();

    public static void RegisterFilter<T>(Expression<Func<T, bool>> filter) where T : Entity =>
        InstanceFilters[typeof(T)] = filter;

    static Expression<Func<T, bool>>? GetInstanceFilter<T>(bool applyFilter) where T : Entity =>
        applyFilter && InstanceFilters.TryGetValue(typeof(T), out var f) ? (Expression<Func<T, bool>>)f : null;

    static IQueryable<T> QueryFiltered<T>(bool applyFilter) where T : Entity
    {
        var query = Database.Query<T>();
        var filter = GetInstanceFilter<T>(applyFilter);
        return filter == null ? query : query.Where(filter);
    }

    public static void Start(SchemaBuilder sb, Func<CultureInfo> defaultCulture)
    {
        if (sb.AlreadyDefined(MethodInfo.GetCurrentMethod()))
            return;


        PropertyRouteTranslationLogic.Start(sb);
        sb.Include<TranslatedInstanceEntity>()
            //.WithDelete(TranslatedInstanceOperation.Delete)
            .WithUniqueIndex(ti => new { ti.Culture, ti.PropertyRoute, ti.Instance, ti.RowId })
            .WithQuery(() => e => new
            {
                Entity = e,
                e.Id,
                e.Culture,
                e.PropertyRoute,
                e.PropertyRoute.RootType,
                e.Instance,
                e.RowId,
                e.TranslatedText,
                e.OriginalText,
            });

        sb.Schema.EntityEvents<PropertyRouteEntity>().PreDeleteSqlSync += property =>
            Administrator.UnsafeDeletePreCommand(Database.Query<TranslatedInstanceEntity>().Where(ti => ti.PropertyRoute.Is(property)));

        getDefaultCulture = defaultCulture ?? throw new ArgumentNullException(nameof(defaultCulture));

        LocalizationCache = sb.GlobalLazy(() =>
            Database.Query<TranslatedInstanceEntity>()
            .ToList()
            .GroupAggregateToDictionary(a => a.Culture.ToCultureInfo(),
            gr2 => gr2.GroupBy(a => a.PropertyRoute)
                .SelectMany(gr =>
                {
                    PropertyRoute pr = gr.Key.ToPropertyRoute();

                    PropertyRoute? mListRoute = pr.GetMListItemsRoute();
                    if (mListRoute == null)
                        return gr.Select(ti => KeyValuePair.Create(new LocalizedInstanceKey(pr, ti.Instance, null), ti));

                    Type type = ((FieldMList)Schema.Current.Field(mListRoute.Parent!)).TableMList.PrimaryKey.Type;

                    return gr.Select(ti => KeyValuePair.Create(new LocalizedInstanceKey(pr, ti.Instance, new PrimaryKey((IComparable)ReflectionTools.Parse(ti.RowId!, type)!)), ti));

                }).ToFrozenDictionaryEx())
            .ToFrozenDictionaryEx()
                , new InvalidateWith(typeof(TranslatedInstanceEntity)));

        PropertyRouteTranslationLogic.TranslatedFieldFunc = (Lite<Entity> lite, PropertyRoute route, PrimaryKey? rowId, string? fallbackString) =>
        {
            var result = GetTranslatedInstance(lite, route, rowId);

            if (result != null && (fallbackString == null || result.OriginalText.Replace("\r", "").Replace("\n", "") == fallbackString.Replace("\r", "").Replace("\n", "")))
                return result.TranslatedText;

            return fallbackString;
        };

        PropertyRouteTranslationLogic.TranslatedFieldExpression = (Lite<Entity> lite, PropertyRoute route, PrimaryKey? rowId, string? fallbackString) =>
            
        Database.Query<TranslatedInstanceEntity>()
            .Where(a => a.Instance.Is(lite) && a.PropertyRoute.Is(route.ToPropertyRouteEntity()) &&
            (rowId == null ? a.RowId == null : a.RowId == rowId.ToString()) &&
            (a.Culture.Is(CultureInfo.CurrentUICulture.TryGetCultureInfoEntity()) || !CultureInfo.CurrentUICulture.IsNeutralCulture && a.Culture.Is(CultureInfo.CurrentUICulture.Parent.TryGetCultureInfoEntity()))
            ).OrderByDescending(a => a.Culture.Name.Length)
            .FirstOrDefault()!.TranslatedText ?? fallbackString;

        PropertyRouteTranslationLogic.IsActivated = true;
    }


    public static List<TranslatedTypeSummary> TranslationInstancesStatus(bool applyFilter = true)
    {
        var cultures = TranslationLogic.CurrentCultureInfos(DefaultCulture);

        return (from type in PropertyRouteTranslationLogic.TranslateableRoutes.Keys
                from ci in cultures
                select new TranslatedTypeSummary
                {
                    Type = type,
                    CultureInfo = ci,
                    State = ci.IsNeutralCulture && ci.Name != DefaultCulture.Name ? GetState(type, ci, applyFilter) : null,
                }).ToList();

    }


    public static Dictionary<LocalizedInstanceKey, string?> FromEntities(Type type, bool applyFilter = false)
    {
        Dictionary<LocalizedInstanceKey, string?>? result = null;

        foreach (var pr in PropertyRouteTranslationLogic.TranslateableRoutes.GetOrThrow(type).Keys)
        {
            var mlist = pr.GetMListItemsRoute();

            var dic = mlist == null ?
                giFromRoute.GetInvoker(type)(pr, applyFilter) :
                giFromRouteMList.GetInvoker(type, mlist.Type)(pr, applyFilter);

            if (result == null)
                result = dic;
            else
                result.AddRange(dic);
        }

        return result!;
    }

    static GenericInvoker<Func<PropertyRoute, bool, Dictionary<LocalizedInstanceKey, string?>>> giFromRoute =
        new((pr, af) => FromRoute<Entity>(pr, af));
    static Dictionary<LocalizedInstanceKey, string?> FromRoute<T>(PropertyRoute pr, bool applyFilter) where T : Entity
    {
        var selector = pr.GetLambdaExpression<T, string?>(safeNullAccess: false);

        return (from e in QueryFiltered<T>(applyFilter)
                select KeyValuePair.Create(new LocalizedInstanceKey(pr, e.ToLite(), null), selector.Evaluate(e))).ToDictionary();
    }

    static GenericInvoker<Func<PropertyRoute, bool, Dictionary<LocalizedInstanceKey, string?>>> giFromRouteMList =
        new((pr, af) => FromRouteMList<Entity, EmbeddedEntity>(pr, af));
    static Dictionary<LocalizedInstanceKey, string?> FromRouteMList<T, M>(PropertyRoute pr, bool applyFilter) where T : Entity
    {
        var mlItemPr = pr.GetMListItemsRoute()!;
        var mListProperty = mlItemPr.Parent!.GetLambdaExpression<T, MList<M>>(safeNullAccess: false);
        var selector = pr.GetLambdaExpression<M, string>(safeNullAccess: false, skipBefore: mlItemPr);
        var filter = GetInstanceFilter<T>(applyFilter);

        var mleQuery = filter == null ? Database.MListQuery(mListProperty) :
            Database.MListQuery(mListProperty).Where(mle => filter.Evaluate(mle.Parent));

        return (from mle in mleQuery
                select KeyValuePair.Create(new LocalizedInstanceKey(pr, mle.Parent.ToLite(), mle.RowId), selector.Evaluate(mle.Element))).ToDictionary();
    }

    public static Dictionary<CultureInfo, Dictionary<LocalizedInstanceKey, TranslatedInstanceEntity>> TranslationsForType(Type type, CultureInfo? culture)
    {
        return LocalizationCache.Value.Where(c => culture == null || c.Key.Equals(culture)).ToDictionary(
            kvp => kvp.Key,
            kvp => kvp.Value.Where(a => a.Key.Route.RootType == type).ToDictionary());
    }

    static TranslatedSummaryState? GetState(Type type, CultureInfo ci, bool applyFilter)
    {
        using (HeavyProfiler.LogNoStackTrace("GetState", () => type.Name + " " + ci.Name))
        {
            if (!PropertyRouteTranslationLogic.TranslateableRoutes.GetOrThrow(type).Keys.Any(pr => AnyNoTranslated(pr, ci, applyFilter)))
                return TranslatedSummaryState.Completed;

            if (Database.Query<TranslatedInstanceEntity>().Count(ti => ti.PropertyRoute.RootType.Is(type.ToTypeEntity()) && ti.Culture.Is(ci.ToCultureInfoEntity())) == 0)
                return TranslatedSummaryState.None;

            return TranslatedSummaryState.Pending;
        }
    }

    public static bool AnyNoTranslated(PropertyRoute pr, CultureInfo ci, bool applyFilter = true)
    {
        var mlist = pr.GetMListItemsRoute();

        if (mlist == null)
            return giAnyNoTranslated.GetInvoker(pr.RootType)(pr, ci, applyFilter);

        return giAnyNoTranslatedMList.GetInvoker(pr.RootType, mlist.Type)(pr, ci, applyFilter);
    }

    static GenericInvoker<Func<PropertyRoute, CultureInfo, bool, bool>> giAnyNoTranslated =
        new((pr, ci, af) => AnyNoTranslated<Entity>(pr, ci, af));
    static bool AnyNoTranslated<T>(PropertyRoute pr, CultureInfo ci, bool applyFilter) where T : Entity
    {
        var exp = pr.GetLambdaExpression<T, string?>(safeNullAccess: false);

        return (from e in QueryFiltered<T>(applyFilter)
                let str = exp.Evaluate(e)
                let ti = Database.Query<TranslatedInstanceEntity>().SingleOrDefault(ti =>
                    ti.Instance.Is(e) &
                    ti.PropertyRoute.IsPropertyRoute(pr) &&
                    ti.Culture.Is(ci.ToCultureInfoEntity()))

                where (str ?? "") != (ti!.OriginalText ?? "")
                select e).Any();
    }

    static GenericInvoker<Func<PropertyRoute, CultureInfo, bool, bool>> giAnyNoTranslatedMList =
       new((pr, ci, af) => AnyNoTranslatedMList<Entity, string>(pr, ci, af));
    static bool AnyNoTranslatedMList<T, M>(PropertyRoute pr, CultureInfo ci, bool applyFilter) where T : Entity
    {
        var mlistItemPr = pr.GetMListItemsRoute()!;
        var mListProperty = mlistItemPr.Parent!.GetLambdaExpression<T, MList<M>>(safeNullAccess: false);

        var exp = pr.GetLambdaExpression<M, string?>(safeNullAccess: false, skipBefore: mlistItemPr);
        var filter = GetInstanceFilter<T>(applyFilter);

        var mleQuery = filter == null ? Database.MListQuery(mListProperty) :
            Database.MListQuery(mListProperty).Where(mle => filter.Evaluate(mle.Parent));

        return (from mle in mleQuery
                let str = exp.Evaluate(mle.Element)
                let ti = Database.Query<TranslatedInstanceEntity>().SingleOrDefaultEx(ti =>
                    ti.Instance.Is(mle.Parent) &&
                    ti.PropertyRoute.IsPropertyRoute(pr) &&
                    ti.RowId == mle.RowId.ToString() &&
                    ti.Culture.Is(ci.ToCultureInfoEntity()))
                where (str ?? "") != (ti!.OriginalText ?? "")
                select mle)
                .Any();
    }

    public static int CleanTranslations(Type t)
    {
        var routeDic = PropertyRouteTranslationLogic.TranslateableRoutes.GetOrThrow(t).Keys.ToDictionary(r => r, r => r.ToPropertyRouteEntity()).Where(kvp => !kvp.Value.IsNew).ToDictionary();

        int deletedPr = Database.Query<TranslatedInstanceEntity>().Where(a => a.PropertyRoute.RootType.Is(t.ToTypeEntity()) && !routeDic.Values.Contains(a.PropertyRoute)).UnsafeDelete();
        int deleteInconsistent = Database.Query<TranslatedInstanceEntity>().Where(a => a.PropertyRoute.RootType.Is(t.ToTypeEntity()) && a.RowId != null != a.PropertyRoute.Path.Contains("/")).UnsafeDelete();

        var routeGroups = routeDic.GroupBy(kvp => kvp.Key.GetMListItemsRoute());

        var mainGroup = routeGroups.SingleOrDefaultEx(a => a.Key == null);

        int deletedInstance = mainGroup == null ? 0 : giRemoveTranslationsForMissingEntities.GetInvoker(t)(mainGroup.ToDictionary());

        var deletedMList = 0;
        foreach (var gr in routeGroups.Where(a => a.Key != null))
        {
            deletedMList += giRemoveTranslationsForMissingRowIds.GetInvoker(t, gr.Key!.Parent!.Type.ElementType()!)(gr.Key, gr.ToDictionary());
        }

        return deletedPr + deleteInconsistent + deletedInstance + deletedMList;
    }

    static GenericInvoker<Func<Dictionary<PropertyRoute, PropertyRouteEntity>, int>> giRemoveTranslationsForMissingEntities = new(dic => RemoveTranslationsForMissingEntities<Entity>(dic));
    static int RemoveTranslationsForMissingEntities<T>(Dictionary<PropertyRoute, PropertyRouteEntity> routes) where T : Entity
    {
        var result = (from ti in Database.Query<TranslatedInstanceEntity>()
                      where ti.PropertyRoute.RootType.Is(typeof(T).ToTypeEntity())
                      join e in Database.Query<T>().DefaultIfEmpty() on ti.Instance.Entity equals e
                      where e == null
                      select ti).UnsafeDelete();

        foreach (var item in routes)
        {
            if (((IColumn)Schema.Current.Field(item.Key)).Nullable != IsNullable.No)
            {
                var exp = item.Key.GetLambdaExpression<T, string?>(false, null);

                result += (from ti in Database.Query<TranslatedInstanceEntity>()
                           where ti.PropertyRoute.Is(item.Value)
                           join e in Database.Query<T>().DefaultIfEmpty() on ti.Instance.Entity equals e
                           where exp.Evaluate(e) == null || exp.Evaluate(e) == ""
                           select ti).UnsafeDelete();
            }
        }

        return result;

    }

    static GenericInvoker<Func<PropertyRoute, Dictionary<PropertyRoute, PropertyRouteEntity>, int>> giRemoveTranslationsForMissingRowIds = new((pr, dic) => RemoveTranslationsForMissingRowIds<Entity, EmbeddedEntity>(pr, dic));
    static int RemoveTranslationsForMissingRowIds<T, E>(PropertyRoute mlistRoute, Dictionary<PropertyRoute, PropertyRouteEntity> routes) where T : Entity
    {
        Expression<Func<T, MList<E>>> expression = mlistRoute.Parent!.GetLambdaExpression<T, MList<E>>(false);
        var prefix = mlistRoute.PropertyString();
        var result = (from ti in Database.Query<TranslatedInstanceEntity>()
                      where ti.PropertyRoute.RootType.Is(typeof(T).ToTypeEntity()) && ti.PropertyRoute.Path.StartsWith(prefix)
                      join mle in Database.MListQuery(expression).DefaultIfEmpty() on new { ti.Instance.Entity, ti.RowId } equals new { Entity = (Entity)mle.Parent, RowId = mle.RowId.ToString() }
                      where mle == null
                      select ti).UnsafeDelete();

        foreach (var item in routes)
        {
            if (((IColumn)Schema.Current.Field(item.Key)).Nullable != IsNullable.No)
            {
                var exp = item.Key.GetLambdaExpression<E, string?>(false, mlistRoute);

                result += (from ti in Database.Query<TranslatedInstanceEntity>()
                           where ti.PropertyRoute.Is(item.Value)
                           join mle in Database.MListQuery(expression).DefaultIfEmpty() on new { ti.Instance.Entity, ti.RowId } equals new { Entity = (Entity)mle.Parent, RowId = mle.RowId.ToString() }
                           where exp.Evaluate(mle.Element) == null || exp.Evaluate(mle.Element) == ""
                           select ti).UnsafeDelete();
            }
        }

        return result;
    }

    public static TranslatedInstanceEntity? GetTranslatedInstance(Lite<Entity> lite, PropertyRoute route, PrimaryKey? rowId)
    {
        return GetTranslatedInstance(lite, route, rowId, CultureInfo.CurrentUICulture);

    }

    public static TranslatedInstanceEntity? GetTranslatedInstance(Lite<Entity> lite, PropertyRoute route, PrimaryKey? rowId, CultureInfo cultureInfo)
    {
        var hastMList = route.GetMListItemsRoute() != null;

        if (hastMList && !rowId.HasValue)
            throw new InvalidOperationException("Route {0} has MList so rowId should have a value".FormatWith(route));

        if (!hastMList && rowId.HasValue)
            throw new InvalidOperationException("Route {0} has not MList so rowId should be null".FormatWith(route));

        if (route.RootType != lite.EntityType)
            throw new InvalidOperationException("Route {0} belongs to type {1}, not {2}".FormatWith(route, route.RootType.TypeName(), lite.EntityType.TypeName()));

        var key = new LocalizedInstanceKey(route, lite, rowId);

        var result = LocalizationCache.Value.TryGetC(cultureInfo)?.TryGetC(key);

        if (result != null)
            return result;

        if (cultureInfo.IsNeutralCulture)
            return null;

        result = LocalizationCache.Value.TryGetC(cultureInfo.Parent)?.TryGetC(key);

        if (result != null)
            return result;

        return null;
    }


    public static T SaveTranslation<T>(this T entity, CultureInfoEntity ci, Expression<Func<T, string?>> propertyRoute, string? translatedText)
        where T : Entity
    {
        if (translatedText.HasText())
            new TranslatedInstanceEntity
            {
                PropertyRoute = PropertyRoute.Construct(propertyRoute).ToPropertyRouteEntity(),
                Culture = ci,
                TranslatedText = translatedText,
                OriginalText = PropertyRouteTranslationLogic.GetPropertyRouteAccesor(propertyRoute)(entity)!,
                Instance = entity.ToLite(),
            }.Save();

        return entity;
    }

    public static T SyncTranslation<T>(this T entity, Dictionary<PropertyRouteEntity, TranslatedInstanceEntity>? translation, CultureInfoEntity ci, Expression<Func<T, string?>> propertyRoute, string? translatedText)
        where T : Entity
    {
        var pr = PropertyRoute.Construct(propertyRoute).ToPropertyRouteEntity();

        if (translatedText.HasText())
        {
            var originalText = PropertyRouteTranslationLogic.GetPropertyRouteAccesor(propertyRoute)(entity)!;
            var current = translation?.TryGetC(pr);
            if (current != null)
            {
                if (current.OriginalText != originalText ||
                    current.TranslatedText != translatedText)
                {
                    current.OriginalText = originalText;
                    current.TranslatedText = translatedText;
                    current.Save();
                }
            }
            else
            {
                new TranslatedInstanceEntity
                {
                    PropertyRoute = pr,
                    Culture = ci,
                    TranslatedText = translatedText,
                    OriginalText = originalText,
                    Instance = entity.ToLite(),
                }.Save();
            }
        }
        else
        {
            translation?.TryGetC(pr)?.Delete();
        } 

        return entity;
    }



    public static string ExportExcelFile(Type type, CultureInfo culture, string folder, bool applyFilter = true)
    {
        var fc = ExportExcelFile(type, culture, applyFilter);
        var fileName = Path.Combine(folder, fc.FileName);
        File.WriteAllBytes(fileName, fc.Bytes);
        return fileName;
    }

    public static FileContent ExportExcelFile(Type type, CultureInfo culture, bool applyFilter = true)
    {
        var isAllowed = Schema.Current.GetInMemoryFilter<TranslatedInstanceEntity>(userInterface: true);
        var master = FromEntities(type, applyFilter);
        var result = TranslationsForType(type, culture).Single().Value
            .Where(a => isAllowed(a.Value) && master.ContainsKey(a.Key))
            .ToDictionary();

        var list = result
            .OrderBy(a => a.Key.Instance.Id)
            .ThenBy(a => a.Key.Route.PropertyInfo!.MetadataToken).Select(r => new ExcelRow
            {
                Instance = r.Key.Instance.KeyLong(),
                Path = r.Key.Route.PropertyString(),
                RowId = r.Key.RowId?.ToString(),
                Original = r.Value.OriginalText,
                Translated = r.Value.TranslatedText
            }).ToList();

        return new FileContent(
            fileName: "{0}.{1}.View.xlsx".FormatWith(TypeLogic.GetCleanName(type), culture.Name),
            bytes: PlainExcelGenerator.WritePlainExcel<ExcelRow>(list)
        );
    }

    public static FileContent ExportExcelFileSync(Type type, CultureInfo culture, bool applyFilter = true)
    {
        var changes = GetInstanceChanges(type, culture, new List<CultureInfo> { DefaultCulture }, applyFilter);

        var list = (from ic in changes
                    from pr in ic.RouteConflicts
                    orderby ic.Instance, pr.Key.ToString()
                    select new ExcelRow
                    {
                        Instance = ic.Instance.KeyLong(),
                        Path = pr.Key.Route.PropertyString(),
                        RowId = pr.Key.RowId?.ToString(),
                        Original = pr.Value.GetOrThrow(DefaultCulture).Original,
                        Translated = null
                    }).ToList();

        return new FileContent(
            fileName: "{0}.{1}.Sync.xlsx".FormatWith(TypeLogic.GetCleanName(type), culture.Name),
            bytes: PlainExcelGenerator.WritePlainExcel(list)
        );
    }



    public static TypeCulturePair ImportExcelFile(string filePath, MatchTranslatedInstances mode = MatchTranslatedInstances.ByInstanceID)
    {
        using (var stream = File.OpenRead(filePath))
            return ImportExcelFile(stream, Path.GetFileName(filePath), mode);
    }

    public static TypeCulturePair ImportExcelFile(Stream stream, string fileName, MatchTranslatedInstances mode)
    {
        Type type = TypeLogic.GetType(fileName.Before('.'));
        CultureInfo culture = CultureInfo.GetCultureInfo(fileName.After('.').Before('.'));

        var records = PlainExcelGenerator.ReadPlainExcel(stream, cellValues => new TranslationRecord
        {
            Culture = culture,
            Key = new LocalizedInstanceKey(
                route: PropertyRoute.Parse(type, cellValues[1]!),
                instance: Lite.Parse<Entity>(cellValues[0]!)!,
                rowId: cellValues[2].DefaultToNull()?.Let(s => PrimaryKey.Parse(s, type))),
            OriginalText = cellValues[3]!,
            TranslatedText = cellValues[4]!
        });

        if (mode == MatchTranslatedInstances.ByInstanceID)
            SaveRecordsByInstance(records, type, isSync: false, culture);
        else
            SaveRecordsByOriginalText(records, type, isSync: false, culture);

        return new TypeCulturePair(type, culture);
    }

    public static List<InstanceChanges> GetInstanceChanges(Type type, CultureInfo targetCulture, List<CultureInfo> cultures, bool applyFilter = true)
    {
        Dictionary<CultureInfo, Dictionary<LocalizedInstanceKey, TranslatedInstanceEntity>> support = TranslationsForType(type, culture: null);

        Dictionary<LocalizedInstanceKey, TranslatedInstanceEntity>? target = support.TryGetC(targetCulture);

        var instances = FromEntities(type, applyFilter).GroupBy(a => a.Key.Instance).Select(gr =>
        {
            Dictionary<LocalizedInstanceKey, string> routeConflicts =
                (from kvp in gr
                 let t = target?.TryGetC(kvp.Key)
                 where kvp.Value.HasText() && (t == null || t.OriginalText.Replace("\r", "").Replace("\n", "") != kvp.Value.Replace("\r", "").Replace("\n", ""))
                 select KeyValuePair.Create(kvp.Key, kvp.Value)).ToDictionary();

            if (routeConflicts.IsEmpty())
                return null;

            var result = (from rc in routeConflicts
                          from c in cultures
                          let str = c.Equals(DefaultCulture) ? rc.Value : support.TryGetC(c)?.TryGetC(rc.Key)?.Let(a => a.OriginalText == rc.Value ? a.TranslatedText : null)
                          where str.HasText()
                          let old = c.Equals(DefaultCulture) ? target?.TryGetC(rc.Key) : null
                          select new
                          {
                              rc.Key.Route,
                              rc.Key.RowId,
                              Culture = c,
                              Conflict = new PropertyRouteConflict
                              {
                                  OldOriginal = old?.OriginalText,
                                  OldTranslation = old?.TranslatedText,

                                  Original = str,
                              }
                          }).GroupAggregateToDictionary(a => new IndexedPropertyRoute(a.Route!, a.RowId), g => g.ToDictionary(a => a.Culture!, a => a.Conflict!));

            return new InstanceChanges
            {
                Instance = gr.Key,
                RouteConflicts = result
            };

        }).NotNull().OrderByDescending(ic => ic.RouteConflicts.Values.Any(dic => dic.Values.Any(rc => rc.OldOriginal != null))).ToList();
        return instances;
    }



    public static void SaveRecordsByInstance(List<TranslationRecord> records, Type type, bool isSync, CultureInfo? c)
    {
        Dictionary<(CultureInfo culture, LocalizedInstanceKey instanceKey), TranslationRecord> should = records
            .Where(a => !isSync || a.TranslatedText.HasText())
            .ToDictionary(a => (a.Culture, a.Key));

        Dictionary<(CultureInfo culture, LocalizedInstanceKey instanceKey), TranslatedInstanceEntity> current =
            (from ci in TranslationsForType(type, c)
             from key in ci.Value
             select KeyValuePair.Create((culture: ci.Key, instanceKey: key.Key), key.Value)).ToDictionary();

        using (var tr = new Transaction())
        {
            Dictionary<PropertyRoute, PropertyRouteEntity> routes = should.Keys.Select(a => a.instanceKey.Route).Distinct().ToDictionary(a => a, a => a.ToPropertyRouteEntity());

            routes.Values.Where(a => a.IsNew).SaveList();

            List<TranslatedInstanceEntity> toInsert = new List<TranslatedInstanceEntity>();

            Synchronizer.Synchronize(
                should,
                current,
                (k, n) => toInsert.Add(new TranslatedInstanceEntity
                {
                    Culture = n.Culture.ToCultureInfoEntity(),
                    PropertyRoute = routes.GetOrThrow(n.Key.Route),
                    Instance = n.Key.Instance,
                    RowId = n.Key.RowId?.ToString(),
                    OriginalText = n.OriginalText,
                    TranslatedText = n.TranslatedText,
                }),
                (k, o) => { },
                (k, n, o) =>
                {
                    if (!n.TranslatedText.HasText())
                    {
                        o.Delete();
                    }
                    else if (o.TranslatedText != n.TranslatedText || o.OriginalText != n.OriginalText)
                    {
                        var r = o.ToLite().RetrieveAndRemember();
                        r.OriginalText = n.OriginalText;
                        r.TranslatedText = n.TranslatedText;
                        r.Save();
                    }
                });

            toInsert.BulkInsert(message: "auto");

            tr.Commit();
        }
    }

    public static void SaveRecordsByOriginalText(List<TranslationRecord> records, Type type, bool isSync, CultureInfo? c)
    {
        Dictionary<string, List<string>> errors = new Dictionary<string, List<string>>();

        Dictionary<(PropertyRoute route, string originalText), Dictionary<CultureInfo, string>> excelTranslations = records
            .Where(a => !isSync || a.TranslatedText.HasText())
            .GroupBy(a => (a.Key.Route, a.OriginalText), a => (a.Culture, a.TranslatedText))
            .Select(gr => KeyValuePair.Create((gr.Key.Route, gr.Key.OriginalText), 
                gr.GroupAggregateToDictionary(a => a.Culture, a =>
                {
                    var only = a.Select(a => a.TranslatedText).Distinct().Only();
                    if (only != null)
                        return only;
                    errors.Add(gr.Key.OriginalText, a.Select(a => a.TranslatedText).Distinct().ToList());
                    return null!;
                }))
            ).ToDictionary();

        if (errors.Any())
            throw new InvalidOperationException($"There are more than one translations for:\n {errors.ToString(a => "* " + a.Key + ":" + a.Value.ToString(", "), "\n")}");

        Dictionary<LocalizedInstanceKey, List<TranslatedInstanceEntity>> databaseTranslations =
            Database.Query<TranslatedInstanceEntity>()
            .Where(a => a.PropertyRoute.RootType.Is(type.ToTypeEntity()) && (c == null || a.Culture.Is(c.ToCultureInfoEntity())))
            .ToList()
            .GroupToDictionary(a =>
            {
                var pr = a.PropertyRoute.ToPropertyRoute();
                return new LocalizedInstanceKey(pr, a.Instance, a.RowId == null ? null : PrimaryKey.Parse(a.RowId.ToString(), pr.GetMListItemsRoute()!.Parent!));
            });

        Dictionary<(PropertyRoute route, string originalText), List<(Lite<Entity> instance, PrimaryKey? rowId)>> currentInstances =
            FromEntities(type)
            .Where(a=>a.Value != null)
            .GroupToDictionary(a => (a.Key.Route, a.Value!), a => (a.Key.Instance, a.Key.RowId));

        using (var tr = new Transaction())
        {
            Dictionary<PropertyRoute, PropertyRouteEntity> routes = excelTranslations.Keys.Select(a => a.route).Distinct().ToDictionary(a => a, a => a.ToPropertyRouteEntity());

            routes.Values.Where(a => a.IsNew).SaveList();

            List<TranslatedInstanceEntity> toInsert = new List<TranslatedInstanceEntity>();

            List<TranslatedInstanceEntity> toDelete = new List<TranslatedInstanceEntity>();
            Synchronizer.Synchronize(
                excelTranslations,
                currentInstances,
                createNew: (k, excelTrans) =>
                {
                    //translations in excel are unnecessary (entity removed or old originalText
                },
                (k, entities) =>
                {
                    foreach (var e in entities)
                    {
                        toDelete.AddRange(databaseTranslations.TryGetC(new LocalizedInstanceKey(k.route, e.instance, e.rowId)).EmptyIfNull());
                    }
                    //Consoider deleting old translations in the database
                },
                (k, excelTrans, entities) =>
                {
                    foreach (var e in entities)
                    {
                        var dbTrans = databaseTranslations.TryGetC(new LocalizedInstanceKey(k.route, e.instance, e.rowId)).EmptyIfNull().ToDictionary(a => a.Culture.ToCultureInfo());

                        Synchronizer.Synchronize(
                            excelTrans,
                            dbTrans,
                            createNew: (ci, translatedText) => toInsert.Add(new TranslatedInstanceEntity
                            {
                                Culture = ci.ToCultureInfoEntity(),
                                PropertyRoute = routes.GetOrThrow(k.route),
                                Instance = e.instance,
                                RowId = e.rowId?.ToString(),
                                OriginalText = k.originalText,
                                TranslatedText = translatedText,
                            }),
                            removeOld: (ci, dbTr) =>
                            {
                                toDelete.Add(dbTr);
                            },
                            merge: (ci, translatedText, dbTr) =>
                            {
                                dbTr.TranslatedText = translatedText; // Only translation changed
                                dbTr.OriginalText = k.originalText;
                                dbTr.Save();
                            });
                    }
                });

            if (toDelete.Any())
            {
                Database.DeleteList(toDelete);
            }

            toInsert.BulkInsert(message: "auto");

            tr.Commit();
        }
    }
}

public class TypeCulturePair
{
    public Type Type;
    public CultureInfo Culture;

    public TypeCulturePair(Type type, CultureInfo culture)
    {
        Type = type;
        Culture = culture;
    }
}



public class TranslationRecord
{
    public required CultureInfo Culture;
    public required LocalizedInstanceKey Key;
    public required string TranslatedText;
    public required string OriginalText;

    public override string ToString()
    {
        return "{0} {1} {2} -> {3}".FormatWith(Culture, Key.Instance, Key.Route, TranslatedText);
    }
}

public class InstanceChanges
{
    public required Lite<Entity> Instance { get; set; }

    public required Dictionary<IndexedPropertyRoute, Dictionary<CultureInfo, PropertyRouteConflict>> RouteConflicts { get; set; }

    public override string ToString()
    {
        return "Changes for {0}".FormatWith(Instance);
    }

    public int TotalOriginalLength()
    {
        return RouteConflicts.Values.Sum(dic => dic[TranslatedInstanceLogic.DefaultCulture].Original.Length);
    }
}

public struct IndexedPropertyRoute : IEquatable<IndexedPropertyRoute>
{
    public readonly PropertyRoute Route;
    public readonly PrimaryKey? RowId;

    public IndexedPropertyRoute(PropertyRoute route, PrimaryKey? rowId)
    {
        Route = route;
        RowId = rowId;
    }

    public override string ToString()
    {
        return Route.PropertyString().Replace("/", "[" + RowId + "].");
    }

    public string RouteRowId()
    {
        return Route.PropertyString() + (RowId == null ? null : ";" + RowId);
    }

    public override bool Equals(object? obj) => obj is IndexedPropertyRoute ipr && base.Equals(ipr);

    public bool Equals(IndexedPropertyRoute other)
    {
        return Route.Equals(other.Route) && RowId.Equals(other.RowId);
    }

    public override int GetHashCode()
    {
        return Route.GetHashCode() ^ RowId.GetHashCode();
    }
}

public class PropertyRouteConflict
{
    public string? OldOriginal;
    public string? OldTranslation;

    public required string Original;
    public List<AutomaticTranslation> AutomaticTranslations = new List<AutomaticTranslation>();

    public override string ToString()
    {
        return "Conflict {0} -> {1}".FormatWith(Original, AutomaticTranslations.Count);
    }
}

class ExcelRow
{
    public required string Instance;
    public required string Path;
    public required string? RowId;
    public required string Original;
    public required string? Translated;
}

public struct LocalizedInstanceKey : IEquatable<LocalizedInstanceKey>
{
    public readonly PropertyRoute Route;
    public readonly Lite<Entity> Instance;
    public readonly PrimaryKey? RowId;

    public LocalizedInstanceKey(PropertyRoute route, Lite<Entity> instance, PrimaryKey? rowId)
    {
        Route = route ?? throw new ArgumentNullException(nameof(route));
        Instance = instance ?? throw new ArgumentNullException("entity");
        RowId = rowId;
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
        var result = "{0} {1}".FormatWith(Route, Instance);

        if (RowId.HasValue)
            result += " " + RowId;

        return result;
    }

    public string RouteAndRowId()
    {
        if (RowId.HasValue)
            return Route.PropertyString() + ";" + RowId;

        return Route.PropertyString();
    }
}

public class TranslatedTypeSummary
{
    public required Type Type;
    public required CultureInfo CultureInfo;
    public required TranslatedSummaryState? State;
}

[InTypeScript(true), DescriptionOptions(DescriptionOptions.Members | DescriptionOptions.Description)]
public enum MatchTranslatedInstances
{
    //Export and import happend in the same DB (stable Ids)
    ByInstanceID,

    //Export and import happend in the a different DB (unstable Ids), like Generate Environment
    ByOriginalText,
}
