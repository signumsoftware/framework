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
        public static bool IsChartColumnType(QueryToken token, ChartColumnType ct)
        {
            if (token is IntervalQueryToken)
                return ct == ChartColumnType.Groupable;

            switch (QueryUtils.TryGetFilterType(token.Type))
            {
                case FilterType.String: 
                case FilterType.Lite: 
                case FilterType.Boolean: 
                case FilterType.Enum: 
                    return ct == ChartColumnType.Groupable;
                case FilterType.Integer: 
                    return ct == ChartColumnType.Magnitude || 
                           ct == ChartColumnType.Positionable || 
                           ct == ChartColumnType.Groupable || 
                           ct == ChartColumnType.GroupableAndPositionable;
                case FilterType.Decimal: 
                    return ct == ChartColumnType.Magnitude || 
                           ct == ChartColumnType.Positionable;
                case FilterType.DateTime: 
                {
                    if(IsDateOnly(token))
                        return  ct == ChartColumnType.Positionable || 
                                ct == ChartColumnType.Groupable || 
                                ct == ChartColumnType.GroupableAndPositionable;

                    return ct == ChartColumnType.Positionable;
                }
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
                    DateTimePrecissionValidatorAttribute datetimePrecission = pp.Validators.OfType<DateTimePrecissionValidatorAttribute>().SingleOrDefaultEx();

                    if (datetimePrecission != null && datetimePrecission.Precision == DateTimePrecision.Days)
                        return true;

                }
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
}
