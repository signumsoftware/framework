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
using System.Drawing;

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

                if (!result)
                    result = chart.Columns[i].IntegrityCheck().HasText();
            }

            if (chart.Columns.Count > chartScript.Columns.Count)
            {
                chart.Columns.RemoveRange(chartScript.Columns.Count, chart.Columns.Count - chartScript.Columns.Count);
                result = true;
            }

            if (chartScript.GroupBy == GroupByChart.Always && chart.GroupResults == false)
            {
                chart.GroupResults = true;
                result = true;
            }
            else if (chartScript.GroupBy == GroupByChart.Never && chart.GroupResults == true)
            {
                chart.GroupResults = false;
                result = true;
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
                u.Scale = r.Scale;
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
                r.Scale = u.Scale;
            });

            return result;
        }


        public static Func<Type, int, Color?> GetChartColor = (type, id) => null;

        //Manual Json printer for performance and pretty print
        public static object DataJson(ChartRequest request, ResultTable resultTable)
        {
            var cols = request.Columns.Select((c, i) => new
            {
                name = "c" + i,
                title = c.GetTitle(),
                token = c.TokenString,
                type = c.TypeName(),
                scale = c.Scale,
                isGroupKey = c.IsGroupKey,
                converter = c.Converter(i)
            }).ToList();

            if (!request.GroupResults)
            {
                cols.Insert(0, new
                {
                    name = "entity",
                    title = "",
                    token = "Entity",
                    type = "entity",
                    scale = ColumnScale.Elements,
                    isGroupKey = (bool?)true,
                    converter = new Func<ResultRow, object>(r => r.Entity.Key())
                });
            }

            return new
            {
                columns = cols.ToDictionary(a => a.name, a => new
                {
                    a.title,
                    a.token,
                    a.isGroupKey,
                    a.type,
                    scale = a.scale.ToString()
                }),
                rows = resultTable.Rows.Select(r => cols.ToDictionary(a => a.name, a => a.converter(r))).ToList()
            };
        }

        private static Func<ResultRow, object> Converter(this ChartColumnDN ct, int columnIndex)
        {
            if (ct == null)
                return null;

            var type = ct.Token.Type.UnNullify();

            if (typeof(Lite).IsAssignableFrom(type))
            {
                return r =>
                {
                    Lite l = (Lite)r[columnIndex];
                    return new
                    {
                        key = l.TryCC(li => li.Key()),
                        toStr = l.TryCC(li => li.ToString()),
                        color = l == null ? "#555" : GetChartColor(l.RuntimeType, l.Id).TryToHtml(),
                    };
                };
            }
            else if (type.IsEnum)
            {
                var enumProxy = EnumProxy.Generate(type);

                return r =>
                {
                    Enum e = (Enum)r[columnIndex];
                    return new
                    {
                        key = e.TryToString(),
                        toStr = e.TryCC(en => en.NiceToString()),
                        color = e == null ? "#555" : GetChartColor(enumProxy, Convert.ToInt32(e)).TryToHtml(),
                    };
                };
            }
            else if (typeof(DateTime) == type)
            {
                return r =>
                {
                    DateTime? e = (DateTime?)r[columnIndex];
                    if (e != null)
                        e = e.Value.ToUserInterface();
                    return new
                    {
                        key = e,
                        keyForFilter = e.TryToString("s"),
                        toStr = ct.Token.Format.HasText() ? e.TryToString(ct.Token.Format) : r[columnIndex].TryToString()
                    };
                };
            }
            else
                return r =>
                {
                    object value = r[columnIndex];
                    return new
                    {
                        key = value,
                        toStr = value,
                    };
                };;
        }

        private static string TypeName(this ChartColumnDN ct)
        {
            if (ct == null || ct.Token == null)
                return null;

            var type = ct.Token.Type.UnNullify();

            switch (QueryUtils.GetFilterType(type))
            {
                case FilterType.Integer: return "integer";
                case FilterType.Decimal: return "decimal";
                case FilterType.String: return "string";
                case FilterType.DateTime: return ct.Token.Format == "d" ? "date" : "datetime";
                case FilterType.Lite: return "entity";
                case FilterType.Embedded: return "embedded";
                case FilterType.Boolean: return "bool";
                case FilterType.Enum: return "enum";
                case FilterType.Guid: return "guid";
                default: return null;
            }
        }
    }
}
