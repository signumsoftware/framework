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
            get { return ToQueryName(query); }
            set { Query = ToQueryEntity(value); }
        }

        [Ignore]
        internal object queryName;

        [NotNullable]
        QueryEntity query;
        [NotNullValidator]
        public QueryEntity Query
        {
            get { return query; }
            set { Set(ref query, value); }
        }

        Lite<TypeEntity> entityType;
        public Lite<TypeEntity> EntityType
        {
            get { return entityType; }
            set { Set(ref entityType, value); }
        }

        Lite<Entity> owner;
        public Lite<Entity> Owner
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
        ChartScriptEntity chartScript;
        [NotNullValidator]
        public ChartScriptEntity ChartScript
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
        MList<ChartColumnEntity> columns = new MList<ChartColumnEntity>();
        public MList<ChartColumnEntity> Columns
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
        MList<QueryFilterEntity> filters = new MList<QueryFilterEntity>();
        public MList<QueryFilterEntity> Filters
        {
            get { return filters; }
            set { Set(ref filters, value); }
        }

        [NotNullable, PreserveOrder]
        MList<QueryOrderEntity> orders = new MList<QueryOrderEntity>();
        public MList<QueryOrderEntity> Orders
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

        static readonly Expression<Func<UserChartEntity, string>> ToStringExpression = e => e.displayName;
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
            EntityType = element.Attribute("EntityType").Try(a => Lite.Parse<TypeEntity>(a.Value));
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
        public static readonly ExecuteSymbol<UserChartEntity> Save = OperationSymbol.Execute<UserChartEntity>();
        public static readonly DeleteSymbol<UserChartEntity> Delete = OperationSymbol.Delete<UserChartEntity>();
    }
}
