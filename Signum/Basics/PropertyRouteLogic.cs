using Signum.API;
using Signum.Engine.Maps;
using Signum.Engine.Sync;
using System.Collections.Frozen;

namespace Signum.Basics;

public static class PropertyRouteLogic
{
    [AutoExpressionField]
    public static bool IsPropertyRoute(this PropertyRouteEntity prdn, PropertyRoute pr) =>
        As.Expression(() => prdn.RootType.Is(pr.RootType.ToTypeEntity()) && prdn.Path == pr.PropertyString());

    public static ResetLazy<FrozenDictionary<TypeEntity, FrozenDictionary<string, PropertyRouteEntity>>> Properties = null!;

    public static void Start(SchemaBuilder sb)
    {
        if (sb.AlreadyDefined(MethodInfo.GetCurrentMethod()))
            return;

        sb.Include<PropertyRouteEntity>()
            .WithUniqueIndex(p => new { p.Path, p.RootType })
            .WithQuery(() => p => new
            {
                Entity = p,
                p.Id,
                p.Path,
                p.RootType
            });

        sb.Schema.Synchronizing += SynchronizeProperties;

        Properties = sb.GlobalLazy(() => Database.Query<PropertyRouteEntity>().AgGroupToDictionary(a => a.RootType, gr => gr.ToFrozenDictionaryEx(a => a.Path)).ToFrozenDictionaryEx(),
            new InvalidateWith(typeof(PropertyRouteEntity)), Schema.Current.InvalidateMetadata);

        PropertyRouteEntity.ToPropertyRouteFunc = ToPropertyRouteImplementation;

        sb.Schema.EntityEvents<TypeEntity>().PreDeleteSqlSync += PropertyRouteLogic_PreDeleteSqlSync;

        if (sb.WebServerBuilder != null)
        {
            SignumServer.WebEntityJsonConverterFactory.AfterDeserilization.Register((PropertyRouteEntity wc) =>
            {
                var route = PropertyRouteLogic.TryGetPropertyRouteEntity(wc.RootType, wc.Path);
                if (route != null)
                {
                    wc.SetId(route.Id);
                    wc.SetIsNew(false);
                    wc.SetCleanModified(false);
                }
            });
        }
    }

    private static SqlPreCommand? PropertyRouteLogic_PreDeleteSqlSync(TypeEntity type)
    {
        Table table = Schema.Current.Table<PropertyRouteEntity>();

        var prs = Database.Query<PropertyRouteEntity>().Where(a => a.RootType.Is(type)).ToList();

        return prs.Select(pr => table.DeleteSqlSync(pr, p => p.RootType.CleanName == pr.RootType.CleanName && p.Path == pr.Path)).Combine(Spacing.Simple);
    }

    public static PropertyRouteEntity? TryGetPropertyRouteEntity(TypeEntity entity, string path)
    {
        return Properties.Value.TryGetC(entity)?.TryGetC(path);
    }

    public const string PropertiesFor = "Properties For:{0}";
    static SqlPreCommand? SynchronizeProperties(Replacements rep)
    {
        var current = Administrator.TryRetrieveAll<PropertyRouteEntity>(rep).AgGroupToDictionary(a => a.RootType.CleanName, g => g.ToDictionaryEx(f => f.Path, "PropertyEntity in the database with path"));

        var should = TypeLogic.TryEntityToType(rep).SelectDictionary(dn => dn.CleanName, (dn, t) => GenerateProperties(t, dn, forSync: true).ToDictionaryEx(f => f.Path, "PropertyEntity in the database with path"));

        Table table = Schema.Current.Table<PropertyRouteEntity>();

        using (rep.WithReplacedDatabaseName())
            return Synchronizer.SynchronizeScript(Spacing.Double, should, current,
                createNew: null,
                removeOld: null,
                mergeBoth: (cleanName, dicShould, dicCurr) =>
                    Synchronizer.SynchronizeScriptReplacing(rep, PropertiesFor.FormatWith(cleanName), Spacing.Simple,
                    dicShould,
                    dicCurr,
                    createNew: null,
                    removeOld: (path, c) => table.DeleteSqlSync(c, p => p.RootType.CleanName == cleanName && p.Path == c.Path)?.TransactionBlock($"PropertyRoute Type = {c.RootType.CleanName} Path = {c.Path}"),
                    mergeBoth: (path, s, c) =>
                    {
                        var originalPathName = c.Path;
                        c.Path = s.Path;
                        return table.UpdateSqlSync(c, p => p.RootType.CleanName == cleanName && p.Path == originalPathName)?.TransactionBlock($"PropertyRoute Type = {c.RootType.CleanName} Path = {c.Path}");
                    })
                );
    }

    public static List<PropertyRouteEntity> RetrieveOrGenerateProperties(TypeEntity typeEntity)
    {
        var retrieve = Database.Query<PropertyRouteEntity>().Where(f => f.RootType.Is(typeEntity)).ToDictionary(a => a.Path);
        var generate = GenerateProperties(TypeLogic.EntityToType.GetOrThrow(typeEntity), typeEntity, forSync: false).ToDictionary(a => a.Path);

        return generate.Select(kvp => retrieve.TryGetC(kvp.Key) ?? kvp.Value).ToList();
    }

    public static List<PropertyRouteEntity> GenerateProperties(Type type, TypeEntity typeEntity, bool forSync)
    {
        return PropertyRoute.GenerateRoutes(type, includeMListElements: forSync).Select(pr =>
            new PropertyRouteEntity
            {
                RootType = typeEntity,
                Path = pr.PropertyString()
            }).ToList();
    }

    static PropertyRoute ToPropertyRouteImplementation(PropertyRouteEntity route)
    {
        return PropertyRoute.Parse(TypeLogic.EntityToType.GetOrThrow(route.RootType), route.Path);
    }

    public static PropertyRouteEntity ToPropertyRouteEntity(this PropertyRoute route)
    {
        TypeEntity type = TypeLogic.TypeToEntity.GetOrThrow(route.RootType);
        string path = route.PropertyString();
        return Database.Query<PropertyRouteEntity>().SingleOrDefaultEx(f => f.RootType.Is(type) && f.Path == path) ??
             new PropertyRouteEntity
             {
                 RootType = type,
                 Path = path
             };
    }



}
