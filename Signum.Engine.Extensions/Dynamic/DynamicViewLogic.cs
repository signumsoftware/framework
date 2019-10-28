using Signum.Engine.Basics;
using Signum.Engine.DynamicQuery;
using Signum.Engine.Maps;
using Signum.Engine.Operations;
using Signum.Entities;
using Signum.Entities.Basics;
using Signum.Entities.Dynamic;
using Signum.Entities.Reflection;
using Signum.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Signum.Engine.Dynamic
{
    public static class DynamicViewLogic
    {
        public static ResetLazy<Dictionary<Type, Dictionary<string,  DynamicViewEntity>>> DynamicViews = null!;
        public static ResetLazy<Dictionary<Type, DynamicViewSelectorEntity>> DynamicViewSelectors = null!;
        public static ResetLazy<Dictionary<Type, List<DynamicViewOverrideEntity>>> DynamicViewOverrides = null!;

        public static void Start(SchemaBuilder sb)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
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
                    Construct = (_) => new DynamicViewEntity() {
                        Locals = "{\r\n" +
                        "  const forceUpdate = modules.Hooks.useForceUpdate(0);\r\n" +
                        "  return { forceUpdate };\r\n" +
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
                        Props = e.Props.Select(a => new DynamicViewPropEmbedded() {  Name = a.Name, Type = a.Type } ).ToMList(),
                        Locals = e.Locals,
                    },
                }.Register();

                DynamicViews = sb.GlobalLazy(() =>
                    Database.Query<DynamicViewEntity>().SelectCatch(dv => new { Type = dv.EntityType.ToType(), dv })
                    .AgGroupToDictionary(a => a.Type!, gr => gr.Select(a => a.dv!).ToDictionaryEx(a => a.ViewName)),
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
                    Database.Query<DynamicViewSelectorEntity>().SelectCatch(dvs => KeyValuePair.Create(dvs.EntityType.ToType(), dvs)).ToDictionaryEx(),
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
                 Database.Query<DynamicViewOverrideEntity>().SelectCatch(dvo => KeyValuePair.Create(dvo.EntityType.ToType(), dvo)).GroupToDictionary(kvp => kvp.Key, kvp => kvp.Value),
                 new InvalidateWith(typeof(DynamicViewOverrideEntity)));

                sb.Schema.Table<TypeEntity>().PreDeleteSqlSync += type => Administrator.UnsafeDeletePreCommand(Database.Query<DynamicViewEntity>().Where(dv => dv.EntityType == type));
                sb.Schema.Table<TypeEntity>().PreDeleteSqlSync += type => Administrator.UnsafeDeletePreCommand(Database.Query<DynamicViewOverrideEntity>().Where(dvo => dvo.EntityType == type));
                sb.Schema.Table<TypeEntity>().PreDeleteSqlSync += type => Administrator.UnsafeDeletePreCommand(Database.Query<DynamicViewSelectorEntity>().Where(dvs => dvs.EntityType == type));
            }
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

}
