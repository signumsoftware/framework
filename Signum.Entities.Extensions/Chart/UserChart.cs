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
using Signum.Entities;

namespace Signum.Entities.Chart
{
    public interface IHasEntitytype
    {
        Lite<TypeEntity> EntityType { get; }
    }

    [Serializable, EntityKind(EntityKind.Main, EntityData.Master), InTypeScript(Undefined = false)]
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

        [NotNullValidator]
        public QueryEntity Query { get; set; }

        public Lite<TypeEntity> EntityType { get; set; }

        public bool HideQuickLink { get; set; }

        public Lite<Entity> Owner { get; set; }

        [StringLengthValidator(AllowNulls = false, Min = 3, Max = 200)]
        public string DisplayName { get; set; }

        ChartScriptEntity chartScript;
        [NotNullValidator]
        public ChartScriptEntity ChartScript
        {
            get { return chartScript; }
            set
            {
                if (Set(ref chartScript, value))
                {
                    chartScript.SynchronizeColumns(this);
                    NotifyAllColumns();
                }
            }
        }

        [NotNullValidator, NoRepeatValidator]
        public MList<ChartParameterEmbedded> Parameters { get; set; } = new MList<ChartParameterEmbedded>();

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

        [NotifyCollectionChanged, NotifyChildProperty, PreserveOrder]
        public MList<ChartColumnEmbedded> Columns { get; set; } = new MList<ChartColumnEmbedded>();

        void NotifyAllColumns()
        {
            foreach (var item in Columns)
            {
                item.NotifyAll();
            }
        }

        [NotNullValidator, PreserveOrder]
        public MList<QueryFilterEmbedded> Filters { get; set; } = new MList<QueryFilterEmbedded>();

        [NotNullValidator, PreserveOrder]
        public MList<QueryOrderEmbedded> Orders { get; set; } = new MList<QueryOrderEmbedded>();

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
            try
            {
                chartScript.SynchronizeColumns(this);
            }
            catch (InvalidOperationException e) when (e.Message.Contains("sealed"))
            {
                throw new InvalidOperationException($"Error Synchronizing columns for '{this}'. Maybe the ChartScript has changed. Consider opening UserChart and saving it again.");
            }
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
                EntityType == null ? null : new XAttribute("EntityType", ctx.TypeToName(EntityType)),
                new XAttribute("HideQuickLink", HideQuickLink),
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
            EntityType = element.Attribute("EntityType")?.Let(a => ctx.GetType(a.Value));
            HideQuickLink = element.Attribute("HideQuickLink")?.Let(a => bool.Parse(a.Value)) ?? false;
            Owner = element.Attribute("Owner")?.Let(a => Lite.Parse(a.Value));
            GroupResults = bool.Parse(element.Attribute("GroupResults").Value);
            Filters.Synchronize(element.Element("Filters")?.Elements().ToList(), (f, x) => f.FromXml(x, ctx));
            Columns.Synchronize(element.Element("Columns")?.Elements().ToList(), (c, x) => c.FromXml(x, ctx));
            Orders.Synchronize(element.Element("Orders")?.Elements().ToList(), (o, x) => o.FromXml(x, ctx));
            Parameters.Synchronize(element.Element("Parameters")?.Elements().ToList(), (p, x) => p.FromXml(x, ctx));
            ChartScript = ctx.ChartScript(element.Attribute("ChartScript").Value);
            ParseData(ctx.GetQueryDescription(Query));
        }

        public void FixParameters(ChartColumnEmbedded chartColumn)
        {
            ChartUtils.FixParameters(this, chartColumn);
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
