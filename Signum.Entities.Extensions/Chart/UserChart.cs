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

namespace Signum.Entities.Chart
{
    public interface IHasEntitytype
    {
        Lite<TypeDN> EntityType { get; } 
    }

    [Serializable, EntityKind(EntityKind.Main, EntityData.Master)]
    public class UserChartDN : Entity, IChartBase, IHasEntitytype, IUserAssetEntity
    {
        public UserChartDN() { }
        public UserChartDN(object queryName)
        {
            this.queryName = queryName;
        }

        [HiddenProperty]
        public object QueryName
        {
            get { return ToQueryName(query); }
            set { Query = ToQueryDN(value); }
        }

        [Ignore]
        internal object queryName;

        [NotNullable]
        QueryDN query;
        [NotNullValidator]
        public QueryDN Query
        {
            get { return query; }
            set { Set(ref query, value); }
        }

        Lite<TypeDN> entityType;
        public Lite<TypeDN> EntityType
        {
            get { return entityType; }
            set { Set(ref entityType, value); }
        }

        Lite<IdentifiableEntity> owner;
        public Lite<IdentifiableEntity> Owner
        {
            get { return owner; }
            set { Set(ref owner, value); }
        }

        [NotNullable, SqlDbType(Size = 100)]
        string displayName;
        [StringLengthValidator(AllowNulls = false, Min = 3, Max = 100)]
        public string DisplayName
        {
            get { return displayName; }
            set { Set(ref displayName, value); }
        }

        [NotNullable]
        ChartScriptDN chartScript;
        [NotNullValidator]
        public ChartScriptDN ChartScript
        {
            get { return chartScript; }
            set
            {
                if (Set(ref chartScript, value))
                {
                    chartScript.SyncronizeColumns(this, changeParameters: true);
                    NotifyAllColumns();
                }
            }
        }

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
        MList<ChartColumnDN> columns = new MList<ChartColumnDN>();
        public MList<ChartColumnDN> Columns
        {
            get { return columns; }
            set { Set(ref columns, value); }
        }

        void NotifyAllColumns()
        {
            foreach (var item in Columns)
            {
                item.NotifyAll();
            }
        }

        [NotNullable, PreserveOrder]
        MList<QueryFilterDN> filters = new MList<QueryFilterDN>();
        public MList<QueryFilterDN> Filters
        {
            get { return filters; }
            set { Set(ref filters, value); }
        }

        [NotNullable, PreserveOrder]
        MList<QueryOrderDN> orders = new MList<QueryOrderDN>();
        public MList<QueryOrderDN> Orders
        {
            get { return orders; }
            set { Set(ref orders, value); }
        }

        [UniqueIndex]
        Guid guid = Guid.NewGuid();
        public Guid Guid
        {
            get { return guid; }
            set { Set(ref guid, value); }
        }

        static readonly Expression<Func<UserChartDN, string>> ToStringExpression = e => e.displayName;
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

        static Func<QueryDN, object> ToQueryName;
        static Func<object, QueryDN> ToQueryDN;

        public static void SetConverters(Func<QueryDN, object> toQueryName, Func<object, QueryDN> toQueryDN)
        {
            ToQueryName = toQueryName;
            ToQueryDN = toQueryDN;
        }

        protected override void PostRetrieving()
        {
            chartScript.SyncronizeColumns(this, changeParameters: false);
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
                Orders.IsNullOrEmpty() ? null : new XElement("Orders", Orders.Select(f => f.ToXml(ctx)).ToList()));
        }

        public void FromXml(XElement element, IFromXmlContext ctx)
        {
            DisplayName = element.Attribute("DisplayName").Value;
            Query = ctx.GetQuery(element.Attribute("Query").Value);
            EntityType = element.Attribute("EntityType").Try(a => Lite.Parse<TypeDN>(a.Value));
            Owner = element.Attribute("Owner").Try(a => Lite.Parse(a.Value));
            ChartScript = ctx.ChartScript(element.Attribute("ChartScript").Value);
            GroupResults = bool.Parse(element.Attribute("GroupResults").Value);
            Filters.Syncronize(element.Element("Filters").Try(fs => fs.Elements()).EmptyIfNull().ToList(), (f, x)=>f.FromXml(x, ctx));
            Columns.Syncronize(element.Element("Columns").Try(fs => fs.Elements()).EmptyIfNull().ToList(), (c, x) => c.FromXml(x, ctx));
            Orders.Syncronize(element.Element("Orders").Try(fs => fs.Elements()).EmptyIfNull().ToList(), (o, x)=>o.FromXml(x, ctx));
            ParseData(ctx.GetQueryDescription(Query));
        }
    }

    public static class UserChartOperation
    {
        public static readonly ExecuteSymbol<UserChartDN> Save = OperationSymbol.Execute<UserChartDN>();
        public static readonly DeleteSymbol<UserChartDN> Delete = OperationSymbol.Delete<UserChartDN>();
    }
}
