using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities.DynamicQuery;
using System.Reflection;
using Signum.Entities;
using Signum.Utilities;
using Signum.Entities.Extensions.Properties;

namespace Signum.Entities.Chart
{
    public static class ChartUtils
    {
        public static List<ChartTypePosition> ChartTypePosition = new List<ChartTypePosition>()
        {
            new ChartTypePosition(ChartType.Pie, 0, 0),
            new ChartTypePosition(ChartType.Doughnout, 0, 1),
            new ChartTypePosition(ChartType.Points, 0, 2),
            new ChartTypePosition(ChartType.Bubbles, 0, 3),

            new ChartTypePosition(ChartType.Columns, 1, 0),
            new ChartTypePosition(ChartType.MultiColumns, 1, 1),
            new ChartTypePosition(ChartType.StackedColumns, 1, 2),
            new ChartTypePosition(ChartType.TotalColumns, 1, 3),

            new ChartTypePosition(ChartType.Bars, 2, 0),
            new ChartTypePosition(ChartType.MultiBars, 2, 1),
            new ChartTypePosition(ChartType.StackedBars, 2, 2),
            new ChartTypePosition(ChartType.TotalBars, 2, 3),
            
            new ChartTypePosition(ChartType.Lines, 3, 0),          
            new ChartTypePosition(ChartType.MultiLines, 3, 1),           
            new ChartTypePosition(ChartType.StackedAreas, 3, 2),
            new ChartTypePosition(ChartType.TotalAreas, 3, 3),
        };


        static Dictionary<ChartType, ChartResultType> ResultMapping = new Dictionary<ChartType, ChartResultType>()
        {
            { ChartType.Pie, ChartResultType.TypeValue},
            { ChartType.Doughnout, ChartResultType.TypeValue},

            { ChartType.Columns, ChartResultType.TypeValue },
            { ChartType.Bars, ChartResultType.TypeValue },
            { ChartType.Lines, ChartResultType.TypeValue },
        
            { ChartType.MultiColumns, ChartResultType.TypeTypeValue },
            { ChartType.MultiBars, ChartResultType.TypeTypeValue },
            { ChartType.MultiLines, ChartResultType.TypeTypeValue },
        
            { ChartType.StackedColumns, ChartResultType.TypeTypeValue },
            { ChartType.StackedBars, ChartResultType.TypeTypeValue },
            { ChartType.StackedAreas, ChartResultType.TypeTypeValue },

            { ChartType.TotalColumns, ChartResultType.TypeTypeValue },
            { ChartType.TotalBars, ChartResultType.TypeTypeValue },
            { ChartType.TotalAreas, ChartResultType.TypeTypeValue },

            { ChartType.Points, ChartResultType.Points},
            { ChartType.Bubbles, ChartResultType.Bubbles},  
        };


        public static ChartResultType GetChartResultType(ChartType type)
        {
            return ResultMapping[type];
        }

        public static ChartLabel? PropertyLabel(ChartType type, ChartTokenName tokenType)
        {
            ChartResultType crt = GetChartResultType(type);
            switch (crt)
            {
                case ChartResultType.TypeValue:
                    if (tokenType == ChartTokenName.FirstDimension)
                        return
                            type == ChartType.Pie || type == ChartType.Doughnout ? ChartLabel.Sections :
                            type == ChartType.Bars ? ChartLabel.VerticalAxis : ChartLabel.HorizontalAxis;

                    if (tokenType == ChartTokenName.FirstValue)
                        return
                            type == ChartType.Pie || type == ChartType.Doughnout ? ChartLabel.Angle :
                            type == ChartType.Bars ? ChartLabel.Width : ChartLabel.Height;
                    break;
                case ChartResultType.TypeTypeValue:
                    bool isBar = type == ChartType.MultiBars || type == ChartType.TotalBars || type == ChartType.StackedBars;

                    if (tokenType == ChartTokenName.FirstDimension)
                        return isBar ? ChartLabel.VerticalAxis : ChartLabel.HorizontalAxis;

                    if (tokenType == ChartTokenName.SecondDimension)
                        return type == ChartType.MultiLines ? ChartLabel.Lines :
                               type == ChartType.StackedAreas || type == ChartType.TotalAreas ? ChartLabel.Areas : ChartLabel.SubGroups;

                    if (tokenType == ChartTokenName.FirstValue)
                        return isBar ? ChartLabel.Width : ChartLabel.Height;

                    break;
                case ChartResultType.Bubbles:
                case ChartResultType.Points:
                    if (tokenType == ChartTokenName.FirstDimension)
                        return ChartLabel.XAxis;

                    if (tokenType == ChartTokenName.SecondDimension)
                        return ChartLabel.YAxis;

                    if (tokenType == ChartTokenName.FirstValue)
                        return ChartLabel.Color;

                    if (crt == ChartResultType.Bubbles && tokenType == ChartTokenName.SecondValue)
                        return ChartLabel.Size;

                    break;
            }

            return null;
        }


        public static string ValidateProperty(ChartResultType crt, ChartTokenName name, QueryToken token)
        {
            switch (crt)
            {
                case ChartResultType.TypeValue:
                    if (name == ChartTokenName.FirstDimension && !IsDiscrete(token))
                        return Resources.ExpressionShouldHaveADiscreteAmountOfValues;

                    if (name == ChartTokenName.FirstValue && !IsContinuous(token))
                        return Resources.ExpressionShouldHaveAContinousAmountOfValues;

                    break;
                case ChartResultType.TypeTypeValue:
                    if (name == ChartTokenName.FirstDimension && !IsDiscrete(token))
                        return Resources.ExpressionShouldHaveADiscreteAmountOfValues;

                    if (name == ChartTokenName.SecondDimension && !IsDiscrete(token))
                        return Resources.ExpressionShouldHaveADiscreteAmountOfValues;

                    if (name == ChartTokenName.FirstValue && !IsContinuous(token))
                        return Resources.ExpressionShouldHaveAContinousAmountOfValues;

                    break;
                case ChartResultType.Points:
                    if (name == ChartTokenName.FirstDimension && !IsContinuous(token))
                        return Resources.ExpressionShouldHaveAContinousAmountOfValues;

                    if (name == ChartTokenName.SecondDimension && !IsContinuous(token))
                        return Resources.ExpressionShouldHaveAContinousAmountOfValues;

                    //Color could be discrete or not

                    break;
                case ChartResultType.Bubbles:
                    if (name == ChartTokenName.FirstDimension && !IsContinuous(token))
                        return Resources.ExpressionShouldHaveAContinousAmountOfValues;

                    if (name == ChartTokenName.SecondDimension && !IsContinuous(token))
                        return Resources.ExpressionShouldHaveAContinousAmountOfValues;

                    //Color could be discrete or not

                    if (name == ChartTokenName.SecondValue && !IsContinuous(token))
                        return Resources.ExpressionShouldHaveAContinousAmountOfValues;

                    break;
            }

            return null;
        }

        public static bool IsDiscrete(QueryToken token)
        {
            if (token is IntervalQueryToken)
                return true;

            switch (QueryUtils.TryGetFilterType(token.Type))
            {
                case FilterType.String:
                case FilterType.Lite:
                case FilterType.Embedded:
                case FilterType.Boolean:
                case FilterType.Enum:
                case FilterType.Number: return true;
                case FilterType.DateTime: return IsDateOnly(token);
            }
            return false;
        }

        public static bool IsDateOnly(QueryToken token)
        {
            if (token is MonthStartToken || token is DateToken)
                return true;

            PropertyRoute route = token.GetPropertyRoute();

            if (route != null && route.PropertyRouteType == PropertyRouteType.FieldOrProperty)
            {

                var pp = Validator.GetOrCreatePropertyPack(route);
                if (pp != null)
                {
                    DateTimePrecissionValidatorAttribute datetimePrecission = pp.Validators.OfType<DateTimePrecissionValidatorAttribute>().SingleOrDefault();

                    if (datetimePrecission != null && datetimePrecission.Precision == DateTimePrecision.Days)
                        return true;

                }
            }

            return false;
        }

        public static bool IsContinuous(QueryToken token)
        {
            switch (QueryUtils.TryGetFilterType(token.Type))
            {
                case FilterType.Number: 
                case FilterType.DecimalNumber: return true; 
            }

            return false;
        }


        public static bool IsVisible(ChartResultType crt, ChartTokenName name)
        {
            switch (crt)
            {
                case ChartResultType.TypeValue: return
                    name == ChartTokenName.FirstDimension ||
                    name == ChartTokenName.FirstValue;

                case ChartResultType.TypeTypeValue:
                case ChartResultType.Points: return
                    name == ChartTokenName.FirstDimension ||
                    name == ChartTokenName.SecondDimension ||
                    name == ChartTokenName.FirstValue;
                case ChartResultType.Bubbles: return
                    name == ChartTokenName.FirstDimension ||
                    name == ChartTokenName.SecondDimension ||
                    name == ChartTokenName.FirstValue ||
                    name == ChartTokenName.SecondValue;
            }
            throw new InvalidOperationException();
        }

        public static bool ShouldAggregate(ChartResultType crt, ChartTokenName name)
        {
            switch (crt)
            {
                case ChartResultType.TypeValue:
                case ChartResultType.TypeTypeValue:
                    return name == ChartTokenName.FirstValue;

                case ChartResultType.Points:
                    return
                        name == ChartTokenName.FirstDimension ||
                        name == ChartTokenName.SecondDimension;

                case ChartResultType.Bubbles:
                    return
                        name == ChartTokenName.FirstDimension ||
                        name == ChartTokenName.SecondDimension ||
                        name == ChartTokenName.SecondValue;
            }

            return false;
        }

        public static bool CanGroupBy(ChartResultType crt, ChartTokenName name)
        {
            switch (crt)
            {
                case ChartResultType.TypeValue:
                    return name == ChartTokenName.FirstDimension;

                case ChartResultType.TypeTypeValue:
                    return name == ChartTokenName.FirstDimension || name == ChartTokenName.SecondDimension;

                case ChartResultType.Bubbles:
                case ChartResultType.Points:
                    return name == ChartTokenName.FirstValue;
            }

            return false;
        }
    }

    public enum ChartLabel
    {
        HorizontalAxis,
        Height,
        VerticalAxis,
        Width,
        SubGroups,
        Lines,
        Areas,
        XAxis,
        YAxis,
        Color,
        Size,
        Sections,
        Angle,
    }

    public class ChartTypePosition
    {
        public ChartType ChartType { get; private set; }
        public int Column { get; private set; }
        public int Row { get; private set; }

        public ChartResultType ChartResultType
        {
            get { return ChartUtils.GetChartResultType(ChartType); }
        }

        public ChartTypePosition(ChartType chartType, int column, int row)
        {
            this.ChartType = chartType;
            this.Column = column;
            this.Row = row;
        }
    }

    public enum ChartTokenName
    {
        FirstDimension,
        SecondDimension,
        FirstValue,
        SecondValue
    }


    public enum ChartType
    {
        Pie,
        Doughnout,

        Columns,
        Bars,
        Lines,

        MultiColumns,
        MultiBars,
        MultiLines,

        StackedColumns,
        StackedBars,
        StackedAreas,

        TotalColumns,
        TotalBars,
        TotalAreas,

        Points,
        Bubbles,

    }

    public enum ChartResultType
    {
        TypeValue,
        TypeTypeValue,

        Points,
        Bubbles,
    }
}
