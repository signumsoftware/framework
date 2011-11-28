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
            UpdateTokens();
        }

        ChartType chartType;
        public ChartType ChartType
        {
            get { return chartType; }
            set
            {
                if (Set(ref chartType, value, () => ChartType))
                {
                    ChartResultType = ChartUtils.GetChartResultType(value);
                    UpdateTokens();
                    NotifyChange(false);
                }
            }
        }

        ChartResultType chartResultType;
        public ChartResultType ChartResultType
        {
            get { return chartResultType; }
            private set
            {
                if (Set(ref chartResultType, value, () => ChartResultType))
                {
                    if (chartResultType == Chart.ChartResultType.TypeValue || chartResultType == Chart.ChartResultType.TypeTypeValue)
                        GroupResults = true;

                    NotifyChange(true);
                }
            }
        }

        bool groupResults = true;
        public bool GroupResults
        {
            get { return groupResults; }
            set
            {
                if (!value && (chartResultType == Chart.ChartResultType.TypeValue || chartResultType == Chart.ChartResultType.TypeTypeValue))
                    return;

                if (Set(ref groupResults, value, () => GroupResults))
                {
                    UpdateGroup();
                    NotifyChange(true);
                }
            }
        }

        ChartTokenDN dimension1;
        public ChartTokenDN Dimension1
        {
            get { return dimension1; }
        }

        ChartTokenDN dimension2;
        public ChartTokenDN Dimension2
        {
            get { return dimension2; }
        }

        ChartTokenDN value1;
        public ChartTokenDN Value1
        {
            get { return value1; }
        }

        ChartTokenDN value2;
        public ChartTokenDN Value2
        {
            get { return value2; }
        }


        void UpdateGroup()
        {
            UpdateTokenGroup(dimension1);
            UpdateTokenGroup(dimension2);
            UpdateTokenGroup(value1);
            UpdateTokenGroup(value2);
        }

        private void UpdateTokenGroup(ChartTokenDN chartToken)
        {
            if (chartToken != null)
            {
                chartToken.NotifyAll();
            }
        }

        protected void UpdateTokens()
        {
            SetToken(ref dimension1, ChartUtils.IsVisible(chartResultType, ChartTokenName.Dimension1), () => Dimension1);
            SetToken(ref dimension2, ChartUtils.IsVisible(chartResultType, ChartTokenName.Dimension2), () => Dimension2);
            SetToken(ref value1, ChartUtils.IsVisible(chartResultType, ChartTokenName.Value1), () => Value1);
            SetToken(ref value2, ChartUtils.IsVisible(chartResultType, ChartTokenName.Value2), () => Value2);
        }

        void SetToken(ref ChartTokenDN token, bool should, Expression<Func<ChartTokenDN>> property)
        {
            if (token == null)
            {
                if (should)
                {
                    token = new ChartTokenDN();
                    token.parentChart = this;
                    token.ExternalPropertyValidation += token_ExternalPropertyValidation;
                }
                else
                {
                    //nothing
                }
            }
            else
            {
                if (should)
                {
                    token.NotifyAll();
                }
                else
                {
                    token.ExternalPropertyValidation -= token_ExternalPropertyValidation;
                    token = null;
                }
            }

            Notify(property);
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

        internal bool token_ShouldAggregate(ChartTokenDN token)
        {
            return GroupResults && ChartUtils.ShouldAggregate(chartResultType, GetTokenName(token));
        }

        internal string token_PropertyLabel(ChartTokenDN token)
        {
            var chartLavel = ChartUtils.PropertyLabel(chartType, GetTokenName(token));
            if (chartLavel == null)
                return null;

            return chartLavel.NiceToString();
        }

        internal bool token_GroupByVisible(ChartTokenDN token)
        {
            return ChartUtils.CanGroupBy(chartResultType, GetTokenName(token));
        }

        string token_ExternalPropertyValidation(ModifiableEntity sender, PropertyInfo pi)
        {
            ChartTokenDN ct = sender as ChartTokenDN;

            if (ct != null)
            {
                if (pi.Is(() => ct.Token))
                {
                    if (groupResults)
                    {
                        if (ChartUtils.ShouldAggregate(chartResultType, GetTokenName((ChartTokenDN)sender)))
                        {
                            if (!(ct.Token is AggregateToken))
                                return Resources.ExpressionShouldBeSomeKindOfAggregate;
                        }
                        else
                        {
                            if (ct.Token is AggregateToken)
                                return Resources.ExpressionCanNotBeAnAggregate;
                        }
                    }

                    return ChartUtils.ValidateProperty(chartResultType, GetTokenName(ct), ct.Token);
                }
            }

            return null;
        }

        protected override void RebindEvents()
        {
            base.RebindEvents();

            RebindEvents(dimension1);
            RebindEvents(value1);

            RebindEvents(dimension2);
            RebindEvents(value2);             
        }

        private void RebindEvents(ChartTokenDN token)
        {
            if (token == null)
                return;

            token.ExternalPropertyValidation += token_ExternalPropertyValidation;
        }

        public ChartTokenName GetTokenName(ChartTokenDN token)
        {
            if (token == null)
                throw new ArgumentNullException("token");

            if (token == dimension1) return ChartTokenName.Dimension1;
            if (token == dimension2) return ChartTokenName.Dimension2;
            if (token == value1) return ChartTokenName.Value1;
            if (token == value2) return ChartTokenName.Value2;

            throw new InvalidOperationException("token not found");
        }

        public ChartTokenDN GetToken(ChartTokenName chartTokenName)
        {
            switch (chartTokenName)
            {
                case ChartTokenName.Dimension1: return dimension1;
                case ChartTokenName.Dimension2: return dimension2;
                case ChartTokenName.Value1: return value1;
                case ChartTokenName.Value2: return value2;
            }

            return null;
        }

        public List<QueryToken> SubTokensFilters(QueryToken token, List<ColumnDescription> list)
        {
            return SubTokensChart(token, list, true); 
        }

        public List<QueryToken> SubTokensOrders(QueryToken token, List<ColumnDescription> list)
        {
            return SubTokensChart(token, list, true);
        }

        internal List<QueryToken> SubTokensChart(QueryToken token, IEnumerable<ColumnDescription> columnDescriptions, bool canAggregate)
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

                        if (ft == FilterType.Number || ft == FilterType.DecimalNumber || ft == FilterType.Boolean)
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
                else if (token != null)
                {
                    FilterType? ft = QueryUtils.TryGetFilterType(token.Type);

                    if (ft == FilterType.Number || ft == FilterType.DecimalNumber)
                    {
                        result.Add(new IntervalQueryToken(token));
                    }
                }
            }

            return result;
        }

        public IEnumerable<ChartTokenDN> ChartTokens()
        {
            if (Dimension1 != null)
                yield return Dimension1;

            if (Dimension2 != null)
                yield return Dimension2;

            if (Value1 != null)
                yield return Value1;

            if (Value2 != null)
                yield return Value2;
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

        public IEnumerable<ChartTokenDN> ChartTokens()
        {
            return chart.ChartTokens();
        }

        public List<QueryToken> AllTokens()
        {
            var allTokens = ChartTokens().Select(a => a.Token).ToList();

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

        public override string ToString()
        {
            return displayName;
        }

        internal void ParseData(QueryDescription description)
        {
            if (Filters != null)
                foreach (var f in Filters)
                    f.ParseData(t => this.Chart.SubTokensChart(t, description.Columns, true));

            if (chart.Dimension1 != null)
                chart.Dimension1.ParseData(description);

            if (chart.Dimension2 != null)
                chart.Dimension2.ParseData(description);

            if (chart.Value1 != null)
                chart.Value1.ParseData(description);

            if (chart.Value2 != null)
                chart.Value2.ParseData(description);

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
                    ChartType = request.Chart.ChartType,
                },

                Orders = request.Orders.Select(o=>new QueryOrderDN
                {
                     Token = o.Token,
                     OrderType = o.OrderType
                }).ToMList()
            };

            Assign(result.Chart.Dimension1, request.Chart.Dimension1);
            Assign(result.Chart.Dimension2, request.Chart.Dimension2);
            Assign(result.Chart.Value1, request.Chart.Value1);
            Assign(result.Chart.Value2, request.Chart.Value2);

            return result;
        }

        protected override void PreSaving(ref bool graphModified)
        {
            base.PreSaving(ref graphModified);

            if (Orders != null)
                for (int i = 0; i < Orders.Count; i++)
                    Orders[i].Index = i;
        }

        private static void Assign(ChartTokenDN result, ChartTokenDN request)
        {
            if (request == null || result == null)
                return;

            if (request.Token != null)
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
                    ChartType = uq.Chart.ChartType,
                },

                Orders = uq.Orders.Select(o => new Order(o.Token, o.OrderType)).ToList(),
            };

            Assign(result.Chart.Dimension1, uq.Chart.Dimension1);
            Assign(result.Chart.Dimension2, uq.Chart.Dimension2);
            Assign(result.Chart.Value1, uq.Chart.Value1);
            Assign(result.Chart.Value2, uq.Chart.Value2);

            return result;

        }
    }
}
