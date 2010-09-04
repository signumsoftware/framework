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

namespace Signum.Entities.Chart
{
    [Serializable]
    public abstract class ChartBase : IdentifiableEntity
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

        ChartTokenDN firstDimension;
        public ChartTokenDN FirstDimension
        {
            get { return firstDimension; }
        }

        ChartTokenDN secondDimension;
        public ChartTokenDN SecondDimension
        {
            get { return secondDimension; }
        }

        ChartTokenDN firstValue;
        public ChartTokenDN FirstValue
        {
            get { return firstValue; }
        }

        ChartTokenDN secondValue;
        public ChartTokenDN SecondValue
        {
            get { return secondValue; }
        }


        void UpdateGroup()
        {
            UpdateTokenGroup(firstDimension);
            UpdateTokenGroup(secondDimension);
            UpdateTokenGroup(firstValue);
            UpdateTokenGroup(secondValue);
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
            SetToken(ref firstDimension, ChartUtils.IsVisible(chartResultType, ChartTokenName.FirstDimension), () => FirstDimension);
            SetToken(ref secondDimension, ChartUtils.IsVisible(chartResultType, ChartTokenName.SecondDimension), () => SecondDimension);
            SetToken(ref firstValue, ChartUtils.IsVisible(chartResultType, ChartTokenName.FirstValue), () => FirstValue);
            SetToken(ref secondValue, ChartUtils.IsVisible(chartResultType, ChartTokenName.SecondValue), () => SecondValue);
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

        

        protected abstract void NotifyChange(bool needNewQuery);

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

        string token_ExternalPropertyValidation(ModifiableEntity sender, PropertyInfo pi, object propertyValue)
        {
            if (pi.Is((ChartTokenDN ct) => ct.Token))
            {
                ChartTokenDN ct = (ChartTokenDN)sender;
                if (ct.Aggregate == AggregateFunction.Count)
                {
                    if (propertyValue != null)
                        return "Expression should be null if count is selected";
                    else
                        return null;
                }
                else
                {
                    if (propertyValue == null)
                        return "Expression is null";
                }

                return ChartUtils.ValidateProperty(chartResultType, GetTokenName(ct), (QueryToken)propertyValue);

            }

            if (pi.Is((ChartTokenDN ct) => ct.Aggregate))
            {
                if (groupResults && ChartUtils.ShouldAggregate(chartResultType, GetTokenName((ChartTokenDN)sender)))
                {
                    if (propertyValue == null)
                        return Resources.ExpressionShouldBeSomeKindOfAggregate;

                    ChartTokenDN ct = (ChartTokenDN)sender;
                    if (ct.Token != null && (ct.Aggregate == AggregateFunction.Sum || ct.Aggregate == AggregateFunction.Average))
                    {
                        var ft = QueryUtils.TryGetFilterType(ct.Token.Type);

                        if (ft != FilterType.Number && ft != FilterType.DecimalNumber)
                            return "{0} is not compatible with {1}".Formato(ct.Aggregate.NiceToString(), ft.NiceToString());
                    }
                }
                else
                {
                    if (propertyValue != null)
                        return "Expression can not be an aggregate";
                }
            }

            return null;
        }

        protected override void RebindEvents()
        {
            base.RebindEvents();

            RebindEvents(firstDimension);
            RebindEvents(firstValue);

            RebindEvents(secondDimension);
            RebindEvents(secondValue);             
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

        ChartTokenName GetTokenName(ChartTokenDN token)
        {
            if (token == null)
                throw new ArgumentNullException("token");

            if (token == firstDimension) return ChartTokenName.FirstDimension;
            if (token == secondDimension) return ChartTokenName.SecondDimension;
            if (token == firstValue) return ChartTokenName.FirstValue;
            if (token == secondValue) return ChartTokenName.SecondValue;

            throw new InvalidOperationException("token not found");
        }

        public ChartTokenDN GetToken(ChartTokenName chartTokenName)
        {
            switch (chartTokenName)
            {
                case ChartTokenName.FirstDimension: return firstDimension;
                case ChartTokenName.SecondDimension: return secondDimension;
                case ChartTokenName.FirstValue: return firstValue;
                case ChartTokenName.SecondValue: return secondValue;
            }

            return null;
        }
    }

    [Serializable]
    public class ChartRequest : ChartBase
    {
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

        public ChartRequest(object queryName)
        {
            this.queryName = queryName;
        }

        [NonSerialized]
        bool needNewQuery;
        public bool NeedNewQuery
        {
            get { return needNewQuery; }
            set { Set(ref needNewQuery, value, () => NeedNewQuery); }
        }

        [field: NonSerialized]
        public event Action ChartRequestChanged;

        protected override void NotifyChange(bool needNewQuery)
        {
            if (needNewQuery)
                this.NeedNewQuery = true;

            if (ChartRequestChanged != null)
                ChartRequestChanged();
        }
    }
    
    [Serializable]
    public class UserChartDN : ChartBase
    {
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
        MList<QueryFilterDN> filters;
        public MList<QueryFilterDN> Filters
        {
            get { return filters; }
            set { Set(ref filters, value, () => Filters); }
        }

        protected override void NotifyChange(bool needNewQuery)
        {
        }

        public override string ToString()
        {
            return displayName;
        }
    }
}
