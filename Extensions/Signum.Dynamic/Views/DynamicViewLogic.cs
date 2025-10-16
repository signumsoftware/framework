using System.Collections.Frozen;

namespace Signum.Dynamic.Views;

public static class DynamicViewLogic
{
    public static ResetLazy<FrozenDictionary<Type, FrozenDictionary<string, DynamicViewEntity>>> DynamicViews = null!;
    public static ResetLazy<FrozenDictionary<Type, DynamicViewSelectorEntity>> DynamicViewSelectors = null!;
    public static ResetLazy<FrozenDictionary<Type, List<DynamicViewOverrideEntity>>> DynamicViewOverrides = null!;

    public static void Start(SchemaBuilder sb)
    {
        if (sb.AlreadyDefined(MethodInfo.GetCurrentMethod()))
            return;

        sb.Include<DynamicViewEntity>()
            .WithUniqueIndex(a => new { a.ViewName, a.EntityType })
            .WithSave(DynamicViewOperation.Save)
            .WithDelete(DynamicViewOperation.Delete)
            .WithQuery(() => e => new
            {
                Entity = e,
                e.Id,
                e.ViewName,
                e.EntityType,
            });

        DynamicViewEntity.TryGetDynamicView = (type, name) => DynamicViews.Value.TryGetC(type)?.TryGetC(name);

        new Graph<DynamicViewEntity>.Construct(DynamicViewOperation.Create)
        {
            Construct = (_) => new DynamicViewEntity()
            {
                Locals = "{\n" +
                "  const forceUpdate = modules.Hooks.useForceUpdate();\n" +
                "  return { forceUpdate };\n" +
                "}",
            },
        }.Register();

        new Graph<DynamicViewEntity>.ConstructFrom<DynamicViewEntity>(DynamicViewOperation.Clone)
        {
            Construct = (e, _) => new DynamicViewEntity()
            {
                ViewName = "",
                EntityType = e.EntityType,
                ViewContent = e.ViewContent,
                Props = e.Props.Select(a => new DynamicViewPropEmbedded() { Name = a.Name, Type = a.Type }).ToMList(),
                Locals = e.Locals,
            },
        }.Register();

        DynamicViews = sb.GlobalLazy(() =>
            Database.Query<DynamicViewEntity>().SelectCatch(dv => new { Type = dv.EntityType.ToType(), dv })
            .AgGroupToDictionary(a => a.Type!, gr => gr.Select(a => a.dv!).ToFrozenDictionaryEx(a => a.ViewName)).ToFrozenDictionary(),
            new InvalidateWith(typeof(DynamicViewEntity)));

        sb.Include<DynamicViewSelectorEntity>()
            .WithSave(DynamicViewSelectorOperation.Save)
            .WithDelete(DynamicViewSelectorOperation.Delete)
            .WithQuery(() => e => new
            {
                Entity = e,
                e.Id,
                e.EntityType,
            });

        DynamicViewSelectors = sb.GlobalLazy(() =>
            Database.Query<DynamicViewSelectorEntity>().SelectCatch(dvs => KeyValuePair.Create(dvs.EntityType.ToType(), dvs)).ToFrozenDictionaryEx(),
            new InvalidateWith(typeof(DynamicViewSelectorEntity)));

        sb.Include<DynamicViewOverrideEntity>()
           .WithSave(DynamicViewOverrideOperation.Save)
           .WithDelete(DynamicViewOverrideOperation.Delete)
           .WithQuery(() => e => new
           {
               Entity = e,
               e.Id,
               e.EntityType,
               e.ViewName,
           });

        DynamicViewOverrides = sb.GlobalLazy(() =>
         Database.Query<DynamicViewOverrideEntity>().SelectCatch(dvo => KeyValuePair.Create(dvo.EntityType.ToType(), dvo)).GroupToFrozenDictionary(kvp => kvp.Key, kvp => kvp.Value),
         new InvalidateWith(typeof(DynamicViewOverrideEntity)));

        sb.Schema.EntityEvents<TypeEntity>().PreDeleteSqlSync += type => Administrator.UnsafeDeletePreCommand(Database.Query<DynamicViewEntity>().Where(dv => dv.EntityType.Is(type)));
        sb.Schema.EntityEvents<TypeEntity>().PreDeleteSqlSync += type => Administrator.UnsafeDeletePreCommand(Database.Query<DynamicViewOverrideEntity>().Where(dvo => dvo.EntityType.Is(type)));
        sb.Schema.EntityEvents<TypeEntity>().PreDeleteSqlSync += type => Administrator.UnsafeDeletePreCommand(Database.Query<DynamicViewSelectorEntity>().Where(dvs => dvs.EntityType.Is(type)));
    }

    public static List<SuggestedFindOptions> GetSuggestedFindOptions(Type type)
    {
        var schema = Schema.Current;
        var queries = QueryLogic.Queries;

        var table = schema.Tables.TryGetC(type);

        if (table == null)
            return new List<SuggestedFindOptions>();

        return (from t in Schema.Current.Tables.Values
                from c in t.Columns.Values
                where c.ReferenceTable == table
                where queries.TryGetQuery(t.Type) != null
                let parentColumn = GetParentColumnExpression(t.Fields, c)?.Let(s => "Entity." + s)
                where parentColumn != null
                select new SuggestedFindOptions(
                    queryKey: QueryLogic.GetQueryEntity(t.Type).Key,
                    parentToken: parentColumn
                )).ToList();

    }

    static string? GetParentColumnExpression(Dictionary<string, EntityField> fields, IColumn c)
    {
        var simple = fields.Values.SingleOrDefault(f => f.Field == c);
        if (simple != null)
            return Reflector.TryFindPropertyInfo(simple.FieldInfo)?.Name;

        var ib = fields.Values.SingleEx(a => a.Field is FieldImplementedBy && ((FieldImplementedBy)a.Field).ImplementationColumns.Values.Contains(c));
        if (ib != null)
            return Reflector.TryFindPropertyInfo(ib.FieldInfo)?.Name;

        foreach (var embedded in fields.Values.Where(f => f.Field is FieldEmbedded))
        {
            var part = GetParentColumnExpression(((FieldEmbedded)embedded.Field).EmbeddedFields, c);
            if (part != null)
                return Reflector.TryFindPropertyInfo(embedded.FieldInfo)?.Let(pi => pi.Name + "." + part);
        }

        return null;
    }
}

public class SuggestedFindOptions
{
    public string queryKey;
    public string parentToken;

    public SuggestedFindOptions(string queryKey, string parentToken)
    {
        this.queryKey = queryKey;
        this.parentToken = parentToken;
    }
}

