using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities.DynamicQuery;
using Signum.Utilities;
using System.Linq.Expressions;
using System.Reflection;
using Signum.Utilities.ExpressionTrees;
using System.ComponentModel;
using Signum.Entities.Reports;

namespace Signum.Entities.Chart
{
    public enum AggregateFunction
    {
        Count,
        Average,
        Sum,
        Min,
        Max,
    }

    [Serializable]
    public class ChartTokenDN : QueryTokenDN
    {
        static ChartTokenDN()
        {
            Validator.GetOrCreatePropertyPack((ChartTokenDN ct) => ct.Token).Validators.Clear();
            Validator.GetOrCreatePropertyPack((ChartTokenDN ct) => ct.TokenString).Validators.Clear();
        }

        public ChartTokenDN()
        {

        }

        protected override void TokenChanged()
        {
            NotifyChange(true);

            if (Token != null)
            {
                if (Token is IntervalQueryToken)
                    ((IntervalQueryToken)Token).PropertyChanged += (s, e) => NotifyChange(true);

                Format = Token.Format;
                Unit = Token.Unit;
                Title = Token.NiceName();
            }
            else
            {
                Format = null;
                Unit = null;
                Title = aggregate.NiceToString();
            }

            Notify(() => Aggregate);   
        }

        AggregateFunction? aggregate;
        public AggregateFunction? Aggregate
        {
            get { return aggregate; }
            set
            {
                if (Set(ref aggregate, value, () => Aggregate))
                {
                    if (aggregate == AggregateFunction.Count)
                        Token = null;

                    NotifyChange(true);

                    Notify(() => Token);
                }
            }
        }

        public Type Type
        {

            get
            {
                if (aggregate == AggregateFunction.Count)
                    return typeof(int);

                if (Token == null)
                    return null;

                if (aggregate == AggregateFunction.Average && QueryUtils.GetFilterType(Token.Type) == FilterType.Number)
                    return Token.Type.IsNullable() ? typeof(double?) : typeof(double);

                return Token.Type;
            }
        }

        string format;
        public string Format
        {
            get { return format; }
            set { if (Set(ref format, value, () => Format))NotifyChange(false); }
        }

        string unit;
        public string Unit
        {
            get { return unit; }
            set { if (Set(ref unit, value, () => Unit))NotifyChange(false); }
        }

        string title;
        public string Title
        {
            get { return title; }
            set { if (Set(ref title, value, () => Title)) NotifyChange(false); }
        }

        [field: NonSerialized, Ignore]
        internal event Func<ChartTokenDN, bool> GroupByVisibleEvent;
        public bool GroupByVisible { get { return GroupByVisibleEvent(this); } }

        [field: NonSerialized, Ignore]
        internal event Func<ChartTokenDN, bool> ShouldAggregateEvent;
        public bool ShouldAggregate { get { return ShouldAggregateEvent(this); } }

        [field: NonSerialized, Ignore]
        internal event Func<ChartTokenDN, string> PropertyLabeleEvent;
        public string PropertyLabel { get { return PropertyLabeleEvent(this); } }

        [field: NonSerialized, Ignore]
        public event Action<bool> ChartRequestChanged;

        public void NotifyChange(bool needNewQuery)
        {
            if (ChartRequestChanged != null)
                ChartRequestChanged(needNewQuery);
        }

        internal void NotifyExternal<T>(Expression<Func<T>> property)
        {
            Notify(property);
        }

        internal void NotifyAll()
        {
            Notify(() => Token);
            Notify(() => GroupByVisible);
            Notify(() => PropertyLabel);
            Notify(() => ShouldAggregate);
            Notify(() => Aggregate);
        }

        internal void NotifyGroup()
        {
            Notify(() => GroupByVisible);
            Notify(() => ShouldAggregate);
        }

        public override string ToString()
        {
            return " ".Combine(Aggregate, Token);
        }

        public Expression BuildExpression(Expression expression)
        {
            if (aggregate == null)
                return Token.BuildExpression(expression);

            Type groupType = expression.Type.GetGenericInterfaces(typeof(IEnumerable<>)).Single("expression should be a IEnumerable").GetGenericArguments()[0];

            if (aggregate.Value == AggregateFunction.Count)
                return Expression.Call(typeof(Enumerable), "Count", new[] { groupType }, new[] { expression });


            ParameterExpression a = Expression.Parameter(groupType, "a");
            LambdaExpression lambda = Expression.Lambda(Token.BuildExpression(a), a);

            if (aggregate.Value == AggregateFunction.Min || aggregate.Value == AggregateFunction.Max)
                return Expression.Call(typeof(Enumerable), aggregate.ToString(), new[] { groupType, lambda.Body.Type }, new[] { expression, lambda });
            else
                return Expression.Call(typeof(Enumerable), aggregate.ToString(), new[] { groupType }, new[] { expression, lambda });
        }

        protected override string PropertyValidation(PropertyInfo pi)
        {
            if(pi.Is(()=>Token) && Token is IDataErrorInfo)
            {
                return ((IDataErrorInfo)Token).Error;
            }

            return base.PropertyValidation(pi);
        }

        public string GetTitle()
        {
            return Title + (Unit.HasText() ? " ({0})".Formato(Unit) : null);
        }

        protected override void PreSaving(ref bool graphModified)
        {
            tokenString = token == null ? null : token.FullKey();
        }

        public override void PostRetrieving(QueryDescription queryDescription)
        {
            Token = tokenString.HasText() ? QueryUtils.Parse(tokenString, token => SubTokensChart(token, queryDescription.StaticColumns)) : null;
            Modified = false;
        }

        static readonly QueryToken[] Empty = new QueryToken[0];

        public static QueryToken[] SubTokensChart(QueryToken token, IEnumerable<StaticColumn> StaticColumns)
        {
            var result = QueryUtils.SubTokens(token, StaticColumns);

            if (token != null)
            {
                FilterType? ft = QueryUtils.TryGetFilterType(token.Type);

                if (ft == FilterType.Number || ft == FilterType.DecimalNumber)
                {
                    return (result ?? Empty).And(new IntervalQueryToken(token)).ToArray();
                }
            }

            return result;       
        }
    }
}
