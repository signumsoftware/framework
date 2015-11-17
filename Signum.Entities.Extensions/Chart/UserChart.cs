using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities.Basics;
using Signum.Entities.DynamicQuery;
using System.Linq.Expressions;
using Signum.Entities.UserQueries;
using Signum.Utilities;
using System.Xml.Linq;
using Signum.Entities.UserAssets;
using System.Reflection;
using Signum.Utilities.ExpressionTrees;

namespace Signum.Entities.Chart
{
    public interface IHasEntitytype
    {
        Lite<TypeEntity> EntityType { get; }
    }

    [Serializable, EntityKind(EntityKind.Main, EntityData.Master)]
    public class UserChartEntity : Entity, IChartBase, IHasEntitytype, IUserAssetEntity
    {
        public UserChartEntity() { }
        public UserChartEntity(object queryName)
        {
            this.queryName = queryName;
        }

        [HiddenProperty]
        public object QueryName
        {
            get { return ToQueryName(Query); }
            set { Query = ToQueryEntity(value); }
        }

        [Ignore]
        internal object queryName;

        [NotNullable]
        [NotNullValidator]
        public QueryEntity Query { get; set; }

        public Lite<TypeEntity> EntityType { get; set; }

        public Lite<Entity> Owner { get; set; }

        [NotNullable, SqlDbType(Size = 100)]
        [StringLengthValidator(AllowNulls = false, Min = 3, Max = 100)]
        public string DisplayName { get; set; }

        [NotNullable]
        ChartScriptEntity chartScript;
        [NotNullValidator]
        public ChartScriptEntity ChartScript
        {
            get { return chartScript; }
            set
            {
                if (Set(ref chartScript, value))
                {
                    chartScript.SyncronizeColumns(this);
                    NotifyAllColumns();
                }
            }
        }

        [NotNullable]
        [NotNullValidator, NoRepeatValidator]
        public MList<ChartParameterEntity> Parameters { get; set; } = new MList<ChartParameterEntity>();

        bool groupResults = true;
        public bool GroupResults
        {
            get { return groupResults; }
            set
            {
                if (Set(ref groupResults, value))
                {
                    NotifyAllColumns();
                }
            }
        }

        [NotifyCollectionChanged, ValidateChildProperty, NotNullable, PreserveOrder]
        public MList<ChartColumnEntity> Columns { get; set; } = new MList<ChartColumnEntity>();

        void NotifyAllColumns()
        {
            foreach (var item in Columns)
            {
                item.NotifyAll();
            }
        }

        [NotNullable, PreserveOrder]
        public MList<QueryFilterEntity> Filters { get; set; } = new MList<QueryFilterEntity>();

        [NotNullable, PreserveOrder]
        public MList<QueryOrderEntity> Orders { get; set; } = new MList<QueryOrderEntity>();

        [UniqueIndex]
        public Guid Guid { get; set; } = Guid.NewGuid();

        static Expression<Func<UserChartEntity, string>> ToStringExpression = e => e.DisplayName;
        [ExpressionField]
        public override string ToString()
        {
            return ToStringExpression.Evaluate(this);
        }

        internal void ParseData(QueryDescription description)
        {
            if (Filters != null)
                foreach (var f in Filters)
                    f.ParseData(this, description, SubTokensOptions.CanElement | SubTokensOptions.CanAnyAll | (this.GroupResults ? SubTokensOptions.CanAggregate : 0));

            if (Columns != null)
                foreach (var c in Columns)
                    c.ParseData(this, description, SubTokensOptions.CanElement | (c.IsGroupKey == false ? SubTokensOptions.CanAggregate : 0));

            if (Orders != null)
                foreach (var o in Orders)
                    o.ParseData(this, description, SubTokensOptions.CanElement | (this.GroupResults ? SubTokensOptions.CanAggregate : 0));
        }

        static Func<QueryEntity, object> ToQueryName;
        static Func<object, QueryEntity> ToQueryEntity;

        public static void SetConverters(Func<QueryEntity, object> toQueryName, Func<object, QueryEntity> toQueryEntity)
        {
            ToQueryName = toQueryName;
            ToQueryEntity = toQueryEntity;
        }

        protected override void PostRetrieving()
        {
            chartScript.SyncronizeColumns(this);
        }

        public void InvalidateResults(bool needNewQuery)
        {
            Notify(() => Invalidator);
        }

        public bool Invalidator { get { return true; } }

        public XElement ToXml(IToXmlContext ctx)
        {
            return new XElement("UserChart",
                new XAttribute("Guid", Guid),
                new XAttribute("DisplayName", DisplayName),
                new XAttribute("Query", Query.Key),
                EntityType == null ? null : new XAttribute("EntityType", EntityType.Key()),
                Owner == null ? null : new XAttribute("Owner", Owner.Key()),
                new XAttribute("ChartScript", ChartScript.Name),
                new XAttribute("GroupResults", GroupResults),
                Filters.IsNullOrEmpty() ? null : new XElement("Filters", Filters.Select(f => f.ToXml(ctx)).ToList()),
                new XElement("Columns", Columns.Select(f => f.ToXml(ctx)).ToList()),
                Orders.IsNullOrEmpty() ? null : new XElement("Orders", Orders.Select(f => f.ToXml(ctx)).ToList()),
                Parameters.IsNullOrEmpty() ? null : new XElement("Parameters", Parameters.Select(f => f.ToXml(ctx)).ToList()));
        }

        public void FromXml(XElement element, IFromXmlContext ctx)
        {
            DisplayName = element.Attribute("DisplayName").Value;
            Query = ctx.GetQuery(element.Attribute("Query").Value);
            EntityType = element.Attribute("EntityType")?.Let(a => Lite.Parse<TypeEntity>(a.Value));
            Owner = element.Attribute("Owner")?.Let(a => Lite.Parse(a.Value));
            ChartScript = ctx.ChartScript(element.Attribute("ChartScript").Value);
            GroupResults = bool.Parse(element.Attribute("GroupResults").Value);
            Filters.Syncronize((element.Element("Filters")?.Elements()).EmptyIfNull().ToList(), (f, x) => f.FromXml(x, ctx));
            Columns.Syncronize((element.Element("Columns")?.Elements()).EmptyIfNull().ToList(), (c, x) => c.FromXml(x, ctx));
            Orders.Syncronize((element.Element("Orders")?.Elements()).EmptyIfNull().ToList(), (o, x) => o.FromXml(x, ctx));
            Parameters.Syncronize((element.Element("Parameters")?.Elements()).EmptyIfNull().ToList(), (p, x) => p.FromXml(x, ctx));
            ParseData(ctx.GetQueryDescription(Query));
        }

        public void FixParameters(ChartColumnEntity chartColumnEntity)
        {

        }

        protected override string PropertyValidation(PropertyInfo pi)
        {
            if (pi.Name == nameof(Parameters) && Parameters != null && ChartScript != null)
            {
                try
                {
                    EnumerableExtensions.JoinStrict(
                        Parameters,
                        ChartScript.Parameters,
                        p => p.Name,
                        ps => ps.Name, 
                        (p, ps) => new { p, ps }, pi.NiceName());
                }
                catch (Exception e)
                {
                    return e.Message;
                }
            }

            return base.PropertyValidation(pi);
        }
    }

    [AutoInit]
    public static class UserChartOperation
    {
        public static ExecuteSymbol<UserChartEntity> Save;
        public static DeleteSymbol<UserChartEntity> Delete;
    }
}
