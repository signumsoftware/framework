using Signum.Engine;
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
using System.Text;
using System.Threading.Tasks;

namespace Signum.Engine.Dynamic
{
    public static class DynamicViewLogic
    {
        public static ResetLazy<Dictionary<Type, List<DynamicViewEntity>>> DynamicViews;

        public static void Start(SchemaBuilder sb, DynamicQueryManager dqm)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                sb.Include<DynamicViewEntity>()
                    .WithUniqueIndex(a => new { a.ViewName, a.EntityType })
                    .WithSave(DynamicViewOperation.Save)
                    .WithDelete(DynamicViewOperation.Delete)
                    .WithQuery(dqm, e => new
                    {
                        Entity = e,
                        e.Id,
                        e.ViewName,
                        e.EntityType,
                    });


                new Graph<DynamicViewEntity>.Construct(DynamicViewOperation.Create)
                {
                    Construct = (_) => new DynamicViewEntity(),
                }.Register();

                new Graph<DynamicViewEntity>.ConstructFrom<DynamicViewEntity>(DynamicViewOperation.Clone)
                {
                    Construct = (e, _) => new DynamicViewEntity()
                    {
                        ViewName = "",
                        EntityType = e.EntityType,
                        ViewContent = e.ViewContent,
                    },
                }.Register();

                DynamicViews = sb.GlobalLazy(() =>
                    Database.Query<DynamicViewEntity>().GroupToDictionary(a => a.EntityType.ToType()),
                    new InvalidateWith(typeof(DynamicViewEntity)));

                sb.Include<DynamicViewSelectorEntity>()
                    .WithSave(DynamicViewSelectorOperation.Save)
                    .WithDelete(DynamicViewSelectorOperation.Delete)
                    .WithQuery(dqm, e => new
                    {
                        Entity = e,
                        e.Id,
                        e.EntityType,
                    });
            }
        }

        public static List<SuggestedFindOptions> GetSuggestedFindOptions(Type type)
        {
            var schema = Schema.Current;
            var dqm = DynamicQueryManager.Current;

            var table = schema.Tables.TryGetC(type);

            if (table == null)
                return new List<SuggestedFindOptions>();

            return (from t in Schema.Current.Tables.Values
                    from c in t.Columns.Values
                    where c.ReferenceTable == table
                    where dqm.TryGetQuery(t.Type) != null
                    let parentColumn = GetParentColumnExpression(t.Fields, c)?.Let(s => "Entity." + s)
                    where parentColumn != null
                    select new SuggestedFindOptions
                    {
                        queryKey = QueryLogic.GetQueryEntity(t.Type).Key,
                        parentColumn = parentColumn,
                    }).ToList();

        }

        static string GetParentColumnExpression(Table t, IColumn c)
        {
            var res = GetParentColumnExpression(t.Fields, c);
            if (res != null)
                return "Entity." + res;

            if (t.Mixins != null)
                foreach (var m in t.Mixins)
                {
                    res = GetParentColumnExpression(m.Value.Fields, c);
                    if (res != null)
                        return "Entity." + res;
                }

            return null;
        }

        static string GetParentColumnExpression(Dictionary<string, EntityField> fields, IColumn c)
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
        public string parentColumn;
    }

}
