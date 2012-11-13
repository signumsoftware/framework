using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities.Basics;
using Signum.Entities.DynamicQuery;
using System.Linq.Expressions;
using Signum.Entities.UserQueries;
using Signum.Utilities;

namespace Signum.Entities.Chart
{
    [Serializable]
    public class UserChartDN : IdentifiableEntity, IChartBase
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

        QueryDN query;
        [NotNullValidator]
        public QueryDN Query
        {
            get { return query; }
            set { Set(ref query, value, () => Query); }
        }

        [ImplementedBy()]
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


        static readonly Expression<Func<UserChartDN, string>> ToStringExpression = e => e.displayName;
        public override string ToString()
        {
            return ToStringExpression.Evaluate(this);
        }

        internal void ParseData(QueryDescription description)
        {
            if (Filters != null)
                foreach (var f in Filters)
                    f.ParseData(t => t.SubTokensChart(description.Columns, this.GroupResults));

            if (Columns != null)
                foreach (var c in Columns)
                    c.ParseData(description);

            if (Orders != null)
                foreach (var o in Orders)
                    o.ParseData(t => t.SubTokensChart(description.Columns, this.GroupResults));
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
                for (int i = 0; i < Orders.Count; i++)
                    Orders[i].Index = i;

            if (Columns != null)
                for (int i = 0; i < Columns.Count; i++)
                    Columns[i].Index = i;
        }

        protected override void PostRetrieving()
        {
            chartScript.SyncronizeColumns(this, changeParameters: false);
        }

        public void InvalidateResults(bool needNewQuery)
        {

        }
    }

    public enum UserChartOperation
    { 
        Save,
        Delete
    }
}
