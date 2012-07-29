using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities.DynamicQuery;
using System.Reflection;
using Signum.Utilities;
using System.Linq.Expressions;
using Signum.Utilities.DataStructures;
using Signum.Entities.Extensions.Properties;
using System.ComponentModel;
using Signum.Utilities.Reflection;
using Signum.Entities.Basics;
using Signum.Entities.Reports;
using Signum.Utilities.ExpressionTrees;
using Signum.Entities.UserQueries;

namespace Signum.Entities.Chart
{
    [Serializable]
    public class ChartBase : EmbeddedEntity
    {
        public ChartBase()
        {
            SyncronizeColumns();
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
                    SyncronizeColumns();
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
                    NotifyChange(true);
                }
            }
        }

        [NotifyCollectionChanged, ValidateChildProperty]
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

        protected bool SyncronizeColumns()
        {
            bool result = false;

            if (chartScript == null)
            {
                result = true;
                columns.Clear();
            }

            for (int i = 0; i < chartScript.Columns.Count; i++)
            {
                if (columns.Count <= i)
                {
                    columns.Add(new ChartColumnDN());
                    result = true;
                }
               
                columns[i].ScriptColumn = chartScript.Columns[i];

            }

            if (columns.Count > chartScript.Columns.Count)
            {
                columns.RemoveRange(chartScript.Columns.Count, columns.Count - chartScript.Columns.Count);
                return true;
            }

                return result;
        }

        [field: NonSerialized, Ignore]
        public event Action ChartRequestChanged;

        internal void NotifyChange(bool needNewQuery)
        {
            if (needNewQuery)
                this.NeedNewQuery = true;

            if (ChartRequestChanged != null)
                ChartRequestChanged();
        }

        [NonSerialized]
        bool needNewQuery;
        [AvoidLocalization]
        public bool NeedNewQuery
        {
            get { return needNewQuery; }
            set { Set(ref needNewQuery, value, () => NeedNewQuery); }
        }

        public List<QueryToken> SubTokensFilters(QueryToken token, List<ColumnDescription> list)
        {
            return SubTokensChart(token, list, true); 
        }

        public List<QueryToken> SubTokensOrders(QueryToken token, List<ColumnDescription> list)
        {
            return SubTokensChart(token, list, true);
        }

        public List<QueryToken> SubTokensChart(QueryToken token, IEnumerable<ColumnDescription> columnDescriptions, bool canAggregate)
        {
            var result = QueryUtils.SubTokens(token, columnDescriptions);

            if (this.groupResults)
            {
                if (canAggregate)
                {
                    if (token == null)
                    {
                        result.Add(new AggregateToken(null, AggregateFunction.Count));
                    }
                    else if (!(token is AggregateToken))
                    {
                        FilterType? ft = QueryUtils.TryGetFilterType(token.Type);

                        if (ft == FilterType.Integer || ft == FilterType.Decimal || ft == FilterType.Boolean)
                        {
                            result.Add(new AggregateToken(token, AggregateFunction.Average));
                            result.Add(new AggregateToken(token, AggregateFunction.Sum));

                            result.Add(new AggregateToken(token, AggregateFunction.Min));
                            result.Add(new AggregateToken(token, AggregateFunction.Max));
                        }
                        else if (ft == FilterType.DateTime) /*ft == FilterType.String || */
                        {
                            result.Add(new AggregateToken(token, AggregateFunction.Min));
                            result.Add(new AggregateToken(token, AggregateFunction.Max));
                        }
                    }
                }
            }

            return result;
        }

      
    }

    [Serializable]
    public class ChartRequest : ModelEntity
    {
        public ChartRequest(object queryName)
        {
            this.queryName = queryName;
        }

        object queryName;
        [NotNullValidator]
        public object QueryName
        {
            get { return queryName; }
        }

        List<Filter> filters = new List<Filter>();
        public List<Filter> Filters
        {
            get { return filters; }
            set { Set(ref filters, value, () => Filters); }
        }

        [NotNullable]
        ChartBase chart = new ChartBase();
        [NotNullValidator]
        public ChartBase Chart
        {
            get { return chart; }
            set { Set(ref chart, value, () => Chart); }
        }

        List<Order> orders = new List<Order>();
        public List<Order> Orders
        {
            get { return orders; }
            set { Set(ref orders, value, () => Orders); }
        }

        public List<QueryToken> AllTokens()
        {
            var allTokens = Chart.Columns.Select(a => a.Token).ToList();

            if (Filters != null)
                allTokens.AddRange(Filters.Select(a => a.Token));

            if (Orders != null)
                allTokens.AddRange(Orders.Select(a => a.Token));

            return allTokens;
        }

        public List<CollectionElementToken> Multiplications
        {
            get { return CollectionElementToken.GetElements(AllTokens().ToHashSet()); }
        }
    }
    
    [Serializable]
    public class UserChartDN : Entity
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
            set { Query = ToQueryDN(value);}
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

        [NotNullable]
        MList<QueryFilterDN> filters = new MList<QueryFilterDN>();
        public MList<QueryFilterDN> Filters
        {
            get { return filters; }
            set { Set(ref filters, value, () => Filters); }
        }

        [NotNullable]
        ChartBase chart = new ChartBase();
        [NotNullValidator]
        public ChartBase Chart
        {
            get { return chart; }
            set { Set(ref chart, value, () => Chart); }
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
                    f.ParseData(t => this.Chart.SubTokensChart(t, description.Columns, true));

            if (Chart != null && Chart.Columns != null)
                foreach (var c in chart.Columns)
                    c.ParseData(description);

            if (Orders != null)
                foreach (var o in Orders)
                    o.ParseData(t => this.Chart.SubTokensChart(t, description.Columns, true));
        }

        static Func<QueryDN, object> ToQueryName;
        static Func<object, QueryDN> ToQueryDN;

        public static void SetConverters(Func<QueryDN, object> toQueryName, Func<object, QueryDN> toQueryDN)
        {
            ToQueryName = toQueryName;
            ToQueryDN = toQueryDN; 
        }

        public static UserChartDN FromRequest(ChartRequest request)
        {
            var result = new UserChartDN
            {
                QueryName = request.QueryName,

                Filters = request.Filters.Select(f => new QueryFilterDN
                {
                    Token = f.Token,
                    Operation = f.Operation,
                    ValueString = FilterValueConverter.ToString(f.Value, f.Token.Type),
                }).ToMList(),
                
                Chart =
                {
                    GroupResults = request.Chart.GroupResults,
                    ChartScript = request.Chart.ChartScript,
                    Columns = request.Chart.Columns.Select(c=>new ChartColumnDN
                    {
                         DisplayName = c.DisplayName,
                         Token = c.Token.Clone(),
                    }).ToMList()
                },

                Orders = request.Orders.Select(o=>new QueryOrderDN
                {
                     Token = o.Token,
                     OrderType = o.OrderType
                }).ToMList()
            };

            return result;
        }

        protected override void PreSaving(ref bool graphModified)
        {
            base.PreSaving(ref graphModified);

            if (Orders != null)
                for (int i = 0; i < Orders.Count; i++)
                    Orders[i].Index = i;
        }

        private static void Assign(ChartColumnDN result, ChartColumnDN request)
        {
            if (request == null || result == null)
                return;

            result.Token = request.Token.Clone();

            result.DisplayName = request.DisplayName;
        }

        public static ChartRequest ToRequest(UserChartDN uq)
        {
            var result = new ChartRequest(uq.QueryName)
            {
                Filters = uq.Filters.Select(qf => new Filter
                {
                    Token = qf.Token,
                    Operation = qf.Operation,
                    Value = qf.Value
                }).ToList(),
                
                Chart =
                {
                    GroupResults = uq.Chart.GroupResults,
                    ChartScript = uq.Chart.ChartScript,
                    Columns = uq.Chart.Columns.Select(c => new ChartColumnDN
                    {
                        DisplayName = c.DisplayName,
                        Token = c.Token.Clone(),
                    }).ToMList()
                },

                Orders = uq.Orders.Select(o => new Order(o.Token, o.OrderType)).ToList(),
            };

            return result;
        }
    }

    
}
