﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities.Basics;
using Signum.Entities.DynamicQuery;
using System.Linq.Expressions;
using Signum.Entities.UserQueries;
using Signum.Utilities;
using System.Xml.Linq;

namespace Signum.Entities.Chart
{
    public interface IHasEntitytype
    {
        Lite<TypeDN> EntityType { get; } 
    }

    [Serializable, EntityKind(EntityKind.Main, EntityData.Master)]
    public class UserChartDN : IdentifiableEntity, IChartBase, IHasEntitytype, IUserAssetEntity
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
            set { Set(ref query, value, () => Query); }
        }

        Lite<TypeDN> entityType;
        public Lite<TypeDN> EntityType
        {
            get { return entityType; }
            set { Set(ref entityType, value, () => EntityType); }
        }

        Lite<IdentifiableEntity> related;
        public Lite<IdentifiableEntity> Related
        {
            get { return related; }
            set { Set(ref related, value, () => Related); }
        }

        [NotNullable, SqlDbType(Size = 100)]
        string displayName;
        [StringLengthValidator(AllowNulls = false, Min = 3, Max = 100)]
        public string DisplayName
        {
            get { return displayName; }
            set { Set(ref displayName, value, () => DisplayName); }
        }

        [NotNullable]
        ChartScriptDN chartScript;
        [NotNullValidator]
        public ChartScriptDN ChartScript
        {
            get { return chartScript; }
            set
            {
                if (Set(ref chartScript, value, () => ChartScript))
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
                if (Set(ref groupResults, value, () => GroupResults))
                {
                    NotifyAllColumns();
                }
            }
        }

        [NotifyCollectionChanged, ValidateChildProperty, NotNullable]
        MList<ChartColumnDN> columns = new MList<ChartColumnDN>();
        public MList<ChartColumnDN> Columns
        {
            get { return columns; }
            set { Set(ref columns, value, () => Columns); }
        }

        void NotifyAllColumns()
        {
            foreach (var item in Columns)
            {
                item.NotifyAll();
            }
        }

        [NotNullable]
        MList<QueryFilterDN> filters = new MList<QueryFilterDN>();
        public MList<QueryFilterDN> Filters
        {
            get { return filters; }
            set { Set(ref filters, value, () => Filters); }
        }

        [NotNullable]
        MList<QueryOrderDN> orders = new MList<QueryOrderDN>();
        public MList<QueryOrderDN> Orders
        {
            get { return orders; }
            set { Set(ref orders, value, () => Orders); }
        }

        [UniqueIndex]
        Guid guid = Guid.NewGuid();
        public Guid Guid
        {
            get { return guid; }
            set { Set(ref guid, value, () => Guid); }
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
                    f.ParseData(this, description, canAggregate: this.GroupResults);

            if (Columns != null)
                foreach (var c in Columns)
                    c.ParseData(this, description, canAggregate: c.IsGroupKey == false);

            if (Orders != null)
                foreach (var o in Orders)
                    o.ParseData(this, description, canAggregate: this.GroupResults);
        }

        static Func<QueryDN, object> ToQueryName;
        static Func<object, QueryDN> ToQueryDN;

        public static void SetConverters(Func<QueryDN, object> toQueryName, Func<object, QueryDN> toQueryDN)
        {
            ToQueryName = toQueryName;
            ToQueryDN = toQueryDN;
        }

        protected override void PreSaving(ref bool graphModified)
        {
            base.PreSaving(ref graphModified);

            if (Orders != null)
                Orders.ForEach((o, i) => o.Index = i);

            if (Columns != null)
                Columns.ForEach((c, i) => c.Index = i);

            if (Filters != null)
                Filters.ForEach((f, i) => f.Index = i);
        }

        protected override void PostRetrieving()
        {
            Orders.Sort(a => a.Index);
            Columns.Sort(a => a.Index);
            Filters.Sort(a => a.Index);

            chartScript.SyncronizeColumns(this, changeParameters: false);
        }

        public void InvalidateResults(bool needNewQuery)
        {

        }

        public void SetFilterValues()
        {
            if (Filters != null)
                foreach (var f in Filters)
                    f.SetValue();
        }

        public XElement ToXml(IToXmlContext ctx)
        {
            return new XElement("UserChart",
                new XAttribute("Guid", Guid),
                new XAttribute("DisplayName", DisplayName),
                new XAttribute("Query", Query.Key),
                EntityType == null ? null : new XAttribute("EntityType", EntityType.Key()),
                Related == null ? null : new XAttribute("Related", Related.Key()),
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
            EntityType = element.Attribute("EntityType").TryCC(a => Lite.Parse<TypeDN>(a.Value));
            Related = element.Attribute("Related").TryCC(a => Lite.Parse(a.Value));
            ChartScript = ctx.ChartScript(element.Attribute("ChartScript").Value);
            GroupResults = bool.Parse(element.Attribute("GroupResults").Value);
            Filters.Syncronize(element.Element("Filters").TryCC(fs => fs.Elements()).EmptyIfNull().ToList(), (f, x)=>f.FromXml(x, ctx));
            Columns.Syncronize(element.Element("Columns").TryCC(fs => fs.Elements()).EmptyIfNull().ToList(), (c, x) => c.FromXml(x, ctx));
            Orders.Syncronize(element.Element("Orders").TryCC(fs => fs.Elements()).EmptyIfNull().ToList(), (o, x)=>o.FromXml(x, ctx));
            ParseData(ctx.GetQueryDescription(Query));
        }
    }

    public enum UserChartOperation
    { 
        Save,
        Delete
    }
}
