using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities.DynamicQuery;
using System.Reflection;
using Signum.Utilities;
using System.Linq.Expressions;
using Signum.Utilities.DataStructures;

namespace Signum.Entities.Chart
{
    public enum ChartType
    {
        Column,
        Bar,
        Line,
        Area,

        MultiColumn,
        MultiBar,
        MultiLine,

        StackedColumn,
        StackedBar,
        StackedArea,
        
        TotalColumn,
        TotalBar,
        TotalArea,

        Points,
        Bubbles,

        Pie,
        Doughnout
    }

    public enum ChartResultType
    {
        TypeValue,
        TypeTypeValue,

        Points,
        Bubbles,
    }

    [Serializable]
    public class ChartRequest : EmbeddedEntity
    {
        object queryName;
        public object QueryName
        {
            get { return queryName; }
            set { Set(ref queryName, value, () => QueryName); }
        }

        List<Filter> filters;
        public List<Filter> Filters
        {
            get { return filters; }
            set { Set(ref filters, value, () => Filters); }
        }

        ChartType chartType;
        public ChartType ChartType
        {
            get { return chartType; }
            set { Set(ref chartType, value, () => ChartType); }
        }

        ChartResultType chartResultType;
        public ChartResultType ChartResultType
        {
            get { return chartResultType; }
            internal set { Set(ref chartResultType, value, () => ChartResultType); }
        }

        bool groupResults;
        public bool GroupResults
        {
            get { return groupResults; }
            set { Set(ref groupResults, value, () => GroupResults); }
        }

        QueryToken firstDimension;
        public QueryToken FirstDimension
        {
            get { return firstDimension; }
            set { Set(ref firstDimension, value, () => FirstDimension); }
        }

        QueryToken secondDimension;
        public QueryToken SecondDimension
        {
            get { return secondDimension; }
            set { Set(ref secondDimension, value, () => SecondDimension); }
        }

        QueryToken firstValue;
        public QueryToken FirstValue
        {
            get { return firstValue; }
            set { Set(ref firstValue, value, () => FirstValue); }
        }

        QueryToken secondValue;
        public QueryToken SecondValue
        {
            get { return secondValue; }
            set { Set(ref secondValue, value, () => SecondValue); }
        }

        protected override string PropertyValidation(PropertyInfo pi)
        {
            string error = staticValidator.Validate(this, pi);

            if (error.HasText())
                return error;

            switch (chartResultType)
            {
                case ChartResultType.TypeValue:
                    if (!IsDiscrete(FirstDimension))
                        return "error";

                    if (IsDiscrete(FirstValue))
                        return "error";
                    break;
                case ChartResultType.TypeTypeValue:
                    if (!IsDiscrete(FirstDimension))
                        return "error";

                    if (!IsDiscrete(SecondDimension))
                        return "error";

                    if (IsDiscrete(FirstValue))
                        return "error";
                    
                    break;
                case ChartResultType.Points:
                    if (IsDiscrete(FirstDimension))
                        return "error";

                    if (IsDiscrete(SecondDimension))
                        return "error";


                    //Color could be discrete or not
                    break;
                case ChartResultType.Bubbles:
                    if (IsDiscrete(FirstDimension))
                        return "error";

                    if (IsDiscrete(SecondDimension))
                        return "error";

                    //Color could be discrete or not

                    if (IsDiscrete(secondValue))
                        return "error";
                        break;
                default:
                    break;
            }

            return base.PropertyValidation(pi);
        }

        public bool IsDiscrete(QueryToken token)
        {
            switch (QueryUtils.GetFilterType(token.Type))
            {
                case FilterType.Number:
                case FilterType.DateTime: return false; 

                case FilterType.String: 
                case FilterType.Lite:
                case FilterType.Entity:
                case FilterType.Embedded:
                case FilterType.Boolean:
                case FilterType.Enum: return true;
            }
            return false; 
        }

        static StateValidator<ChartRequest, ChartResultType> staticValidator = new StateValidator<ChartRequest, ChartResultType>
        (a => a.ChartResultType,          a => a.FirstDimension, a => a.SecondDimension, a => a.FirstValue, a=>a.SecondValue){
        { ChartResultType.TypeValue,      true,                  false,                  true,             false },
        { ChartResultType.TypeTypeValue,  true,                  true,                   true,             false },
        { ChartResultType.Points,         true,                  true,                   true,             false },
        { ChartResultType.Bubbles,        true,                  true,                   true,             true },
        };


        static Dictionary<ChartType, ChartResultType> resultMapping = new Dictionary<ChartType, ChartResultType>()
        {
            { ChartType.Column, ChartResultType.TypeValue },
            { ChartType.Bar, ChartResultType.TypeValue },
            { ChartType.Line, ChartResultType.TypeValue },
            { ChartType.Area, ChartResultType.TypeValue },
        
            { ChartType.MultiColumn, ChartResultType.TypeTypeValue },
            { ChartType.MultiBar, ChartResultType.TypeTypeValue },
            { ChartType.MultiLine, ChartResultType.TypeTypeValue },
        
            { ChartType.StackedColumn, ChartResultType.TypeTypeValue },
            { ChartType.StackedBar, ChartResultType.TypeTypeValue },
            { ChartType.StackedArea, ChartResultType.TypeTypeValue },

            { ChartType.TotalColumn, ChartResultType.TypeTypeValue },
            { ChartType.TotalBar, ChartResultType.TypeTypeValue },
            { ChartType.TotalArea, ChartResultType.TypeTypeValue },

            { ChartType.Points, ChartResultType.Points},
            { ChartType.Bubbles, ChartResultType.Bubbles},
        };

    }

    public enum AggregateFunction
    {
        Average,
        Count,
        Min,
        Max,
        Sum,
    }

    public static class QueryTokenExtensions
    {

        public static QueryToken[] SubTokensGroupValue(QueryToken parent)
        {
            FilterType ft = QueryUtils.GetFilterType(token.Type);

            switch (ft)
            {
                case FilterType.Lite:
                case FilterType.Entity:
                case FilterType.Embedded:
                    return parent.SubTokens();
                    break;
                case FilterType.Boolean:
                case FilterType.Enum:
                case FilterType.Number:
                case FilterType.String:
                case FilterType.DateTime:
                    return new Sequence<QueryToken>()
                    {
                        parent.SubTokens(),
                        new AggregateQueryToken(parent, AggregateFunction.Average)
                    }.ToArray();
                    break;
                default:
                    break;
            }
        }
    }

    public class AggregateQueryToken : QueryToken
    {
        AggregateFunction aggregateFunction;

        public AggregateQueryToken(QueryToken parent, AggregateFunction aggregateFunction): base(parent)
        {
            if (parent == null && aggregateFunction != AggregateFunction.Count)
                throw new ArgumentNullException("parent"); 
        }


        public override string ToString()
        {
            return aggregateFunction.ToString();
        }

        public override string NiceName()
        {
            return aggregateFunction.NiceToString();
        }

        public override string Format
        {
            get { return Parent.Format; }
        }

        public override string Unit
        {
            get { return Parent.Format; }
        }

        public override Type Type
        {
            get
            {
                Type type = Parent.Type.UnNullify();
                if (type == typeof(int) || type == typeof(long))
                    return typeof(double?);

                return type.Nullify();
            }
        }

        public override string Key
        {
            get { return "{0}.[{1}]".Formato(Parent.Key, aggregateFunction); }
        }

        protected override QueryToken[] SubTokensInternal()
        {
            return null;
        }

        public override Expression BuildExpression(Expression expression)
        {
            //g=>g.Sum(a=>"base.BuildExpression(a)");

            Type t = expression.GetType();
            if(t.IsInstantiationOf(typeof(IGrouping<,>)))
                throw new InvalidOperationException("expression should be a Grouping expression");

            Type groupType = t.GetGenericArguments()[1];

            if (Parent == null)
            {
                return Expression.Call(typeof(Queryable), aggregateFunction.ToString(), new[] { groupType }, new[] { expression });
            }
            else
            {
                ParameterExpression a = Expression.Parameter(groupType, "a");

                LambdaExpression lambda =  Expression.Lambda(Parent.BuildExpression(a), a);

                return Expression.Call(typeof(Queryable), aggregateFunction.ToString(), new[] { groupType }, new[] { expression, lambda });
            }
        }

        public override PropertyRoute GetPropertyRoute()
        {
            return Parent == null ? null : Parent.GetPropertyRoute();
        }

        public override Implementations Implementations()
        {
            return null;
        }

        public override bool IsAllowed()
        {
            return Parent == null ? true : Parent.IsAllowed();
        }
    }
}
