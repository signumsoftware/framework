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
                    NotifyChange(true);
            }
        }

        bool groupResults;
        public bool GroupResults
        {
            get { return groupResults; }
            set
            {
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

        void UpdateTokenGroup(ChartTokenDN token)
        {
            if (token == null)
                return;

            if (token.Aggregate != null && !GroupResults)
                token.Aggregate = null;

            token.NotifyGroup();
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
                    token.ChartRequestChanged += NotifyChange;
                    token.ExternalPropertyValidation += token_ExternalPropertyValidation;
                    token.ShouldAggregateEvent += token_ShouldAggregateEvent;
                    token.GroupByVisibleEvent += token_GroupByVisibleEvent;
                    token.PropertyLabeleEvent += token_PropertyLabeleEvent;
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
                    token.ChartRequestChanged -= NotifyChange;
                    token.ExternalPropertyValidation -= token_ExternalPropertyValidation;
                    token.ShouldAggregateEvent -= token_ShouldAggregateEvent;
                    token.GroupByVisibleEvent -= token_GroupByVisibleEvent;
                    token.PropertyLabeleEvent -= token_PropertyLabeleEvent;
                    token = null;
                }
            }

            Notify(property);
        }

        [field: NonSerialized, Ignore]
        public event Action ChartRequestChanged;

        protected void NotifyChange(bool needNewQuery)
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

        bool token_ShouldAggregateEvent(ChartTokenDN token)
        {
            return GroupResults && ChartUtils.ShouldAggregate(chartResultType, GetTokenName(token));
        }

        string token_PropertyLabeleEvent(ChartTokenDN token)
        {
            var chartLavel = ChartUtils.PropertyLabel(chartType, GetTokenName(token));
            if (chartLavel == null)
                return null;

            return chartLavel.NiceToString();
        }

        bool token_GroupByVisibleEvent(ChartTokenDN token)
        {
            return ChartUtils.CanGroupBy(chartResultType, GetTokenName(token));
        }

        string token_ExternalPropertyValidation(ModifiableEntity sender, PropertyInfo pi)
        {
            ChartTokenDN ct = sender as ChartTokenDN;

            if(ct != null)
            {
                if (pi.Is(() => ct.Token))
                {
                    return ChartUtils.ValidateProperty(chartResultType, GetTokenName(ct), ct.Token);
                }

                if (pi.Is(() => ct.Aggregate))
                {
                    if (groupResults && ChartUtils.ShouldAggregate(chartResultType, GetTokenName((ChartTokenDN)sender)))
                    {
                        if (ct.Aggregate == null)
                            return Resources.ExpressionShouldBeSomeKindOfAggregate;

                        if (ct.Token != null && (ct.Aggregate == AggregateFunction.Sum || ct.Aggregate == AggregateFunction.Average))
                        {
                            var ft = QueryUtils.TryGetFilterType(ct.Token.Type);

                            if (ft == FilterType.Number || ft == FilterType.DecimalNumber)
                                return null;

                            if (ft == FilterType.Boolean && ct.Aggregate == AggregateFunction.Average)
                                return null;

                            return "{0} is not compatible with {1}".Formato(ct.Aggregate.NiceToString(), 
                                ft != null ? ft.NiceToString() : ct.Token.Type.TypeName());
                        }
                    }
                    else
                    {
                        if (ct.Aggregate != null)
                            return "Expression can not be an aggregate";
                    }
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

            token.ChartRequestChanged += NotifyChange;
            token.ExternalPropertyValidation += token_ExternalPropertyValidation;
            token.ShouldAggregateEvent += token_ShouldAggregateEvent;
            token.GroupByVisibleEvent += token_GroupByVisibleEvent;
            token.PropertyLabeleEvent += token_PropertyLabeleEvent;
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

        List<Filter> filters;
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

        public IEnumerable<ChartTokenDN> ChartTokens()
        {
            if (chart.Dimension1 != null)
                yield return chart.Dimension1;

            if (chart.Dimension2 != null)
                yield return chart.Dimension2;

            if (chart.Value1 != null)
                yield return chart.Value1;

            if (chart.Value2 != null)
                yield return chart.Value2;
        }

        public List<CollectionElementToken> Multiplications
        {
            get
            {
                var allTokens = ChartTokens().Select(a => a.Token);

                if (Filters != null)
                    allTokens = allTokens.Concat(Filters.Select(a => a.Token));

                return CollectionElementToken.GetElements(allTokens.ToHashSet());
            }
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

        public override string ToString()
        {
            return displayName;
        }

        internal void ParseData(QueryDescription description)
        {
            if (Filters != null)
                foreach (var f in Filters)
                    f.ParseData(description);

            if (chart.Dimension1 != null)
                chart.Dimension1.ParseData(description);

            if (chart.Dimension2 != null)
                chart.Dimension2.ParseData(description);

            if (chart.Value1 != null)
                chart.Value1.ParseData(description);

            if (chart.Value2 != null)
                chart.Value2.ParseData(description);
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

                Chart =
                {
                    GroupResults = request.Chart.GroupResults,
                    ChartType = request.Chart.ChartType,
                },

                Filters = request.Filters.Select(f => new QueryFilterDN
                {
                    Token = f.Token,
                    Operation = f.Operation,
                    ValueString = FilterValueConverter.ToString(f.Value, f.Token.Type),
                }).ToMList(),
            };

            Assign(result.Chart.Dimension1, request.Chart.Dimension1);
            Assign(result.Chart.Dimension2, request.Chart.Dimension2);
            Assign(result.Chart.Value1, request.Chart.Value1);
            Assign(result.Chart.Value2, request.Chart.Value2);

            return result;
        }

        private static void Assign(ChartTokenDN result, ChartTokenDN request)
        {
            if (request == null || result == null)
                return;

            if (request.Token != null)
                result.Token = request.Token.Clone();

            result.Aggregate = request.Aggregate;

            result.Unit = request.Unit;
            result.Format = request.Format;
            result.DisplayName = request.DisplayName;

            result.OrderPriority = request.OrderPriority;
            result.OrderType = request.OrderType;
        }

        public static ChartRequest ToRequest(UserChartDN uq)
        {
            var result = new ChartRequest(uq.QueryName)
            {
                Chart =
                {
                    GroupResults = uq.Chart.GroupResults,
                    ChartType = uq.Chart.ChartType,
                },

                Filters = uq.Filters.Select(qf => new Filter
                {
                    Token = qf.Token,
                    Operation = qf.Operation,
                    Value = qf.Value
                }).ToList(),
            };

            Assign(result.Chart.Dimension1, uq.Chart.Dimension1);
            Assign(result.Chart.Dimension2, uq.Chart.Dimension2);
            Assign(result.Chart.Value1, uq.Chart.Value1);
            Assign(result.Chart.Value2, uq.Chart.Value2);

            return result;

        }
    }
}
