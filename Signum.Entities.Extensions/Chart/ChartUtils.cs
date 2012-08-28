using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities.DynamicQuery;
using System.Reflection;
using Signum.Entities;
using Signum.Utilities;
using Signum.Entities.Extensions.Properties;
using Signum.Entities.UserQueries;

namespace Signum.Entities.Chart
{
    public static class ChartUtils
    {
        public static bool IsChartColumnType(QueryToken token, ChartColumnType ct)
        {
            if (token == null)
                return false;

            if (token is IntervalQueryToken)
                return ct == ChartColumnType.Groupable;

            switch (QueryUtils.TryGetFilterType(token.Type))
            {
                case FilterType.Lite:
                case FilterType.Boolean:
                case FilterType.Enum:
                    return Flag(ct, ChartColumnType.Entity);
                case FilterType.String:
                case FilterType.Guid:
                    return Flag(ct, ChartColumnType.String);
                case FilterType.Integer:
                    return Flag(ct, ChartColumnType.Integer);
                case FilterType.Decimal:
                    return Flag(ct, ChartColumnType.Decimal);
                case FilterType.DateTime:
                    {
                        if (IsDateOnly(token))
                            return Flag(ct, ChartColumnType.Date);

                        return Flag(ct, ChartColumnType.DateTime);
                    }
            }

            return false;
        }

        public static bool Flag(ChartColumnType ct, ChartColumnType flag)
        {
            return (ct & flag) == flag;
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

        public static List<QueryToken> SubTokensChart(this QueryToken token, IEnumerable<ColumnDescription> columnDescriptions, bool canAggregate)
        {
            var result = QueryUtils.SubTokens(token, columnDescriptions);

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

            return result;
        }
        
        public static bool SyncronizeColumns(this ChartScriptDN chartScript, IChartBase chart)
        {
            bool result = false;

            if (chartScript == null)
            {
                result = true;
                chart.Columns.Clear();
            }

            for (int i = 0; i < chartScript.Columns.Count; i++)
            {
                if (chart.Columns.Count <= i)
                {
                    chart.Columns.Add(new ChartColumnDN());
                    result = true;
                }

                chart.Columns[i].ScriptColumn = chartScript.Columns[i];
                chart.Columns[i].parentChart = chart; 
            }

            if (chart.Columns.Count > chartScript.Columns.Count)
            {
                chart.Columns.RemoveRange(chartScript.Columns.Count, chart.Columns.Count - chartScript.Columns.Count);
                return true;
            }

            return result;
        }

        public static UserChartDN ToUserChart(this ChartRequest request)
        {
            var result = new UserChartDN
            {
                QueryName = request.QueryName,

                GroupResults = request.GroupResults,
                ChartScript = request.ChartScript,
                
                Filters = request.Filters.Select(f => new QueryFilterDN
                {
                    Token = f.Token,
                    Operation = f.Operation,
                    ValueString = FilterValueConverter.ToString(f.Value, f.Token.Type),
                }).ToMList(),

                Orders = request.Orders.Select(o => new QueryOrderDN
                {
                    Token = o.Token,
                    OrderType = o.OrderType
                }).ToMList()
            };

            result.Columns.ZipForeach(request.Columns, (u, r) =>
            {
                u.Token = r.Token;
                u.DisplayName = r.DisplayName;
            });

            return result;
        }

        public static ChartRequest ToRequest(this UserChartDN uq)
        {
            var result = new ChartRequest(uq.QueryName)
            {
                GroupResults = uq.GroupResults,
                ChartScript = uq.ChartScript,
                
                Filters = uq.Filters.Select(qf => new Filter
                {
                    Token = qf.Token,
                    Operation = qf.Operation,
                    Value = qf.Value
                }).ToList(),

                Orders = uq.Orders.Select(o => new Order(o.Token, o.OrderType)).ToList(),
            };

            result.Columns.ZipForeach(uq.Columns, (r, u) =>
            {
                r.Token = u.Token;
                r.DisplayName = u.DisplayName;
            });

            return result;
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
