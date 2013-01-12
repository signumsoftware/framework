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

            var type =  token.GetChartColumnType();

            if(type == null)
                return false;

            return Flag(ct, type.Value);
        }

        public static ChartColumnType? GetChartColumnType(this QueryToken token)
        {
            switch (QueryUtils.TryGetFilterType(token.Type))
            {
                case FilterType.Lite:return ChartColumnType.Lite;
                case FilterType.Boolean:
                case FilterType.Enum: return ChartColumnType.Enum;
                case FilterType.String:
                case FilterType.Guid: return ChartColumnType.String;
                case FilterType.Integer: return ChartColumnType.Integer;
                case FilterType.Decimal: return ChartColumnType.Real;
                case FilterType.DateTime:
                    {
                        if (IsDateOnly(token))
                            return ChartColumnType.Date;

                        return ChartColumnType.DateTime;
                    }
            }

            return null;
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
        
        public static bool SyncronizeColumns(this ChartScriptDN chartScript, IChartBase chart, bool changeParameters)
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

                chart.Columns[i].parentChart = chart;
                chart.Columns[i].ScriptColumn = chartScript.Columns[i];
                if (changeParameters)
                    chart.Columns[i].SetDefaultParameters();
             
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
                u.Parameter1 = r.Parameter1;
                u.Parameter2 = r.Parameter2;
                u.Parameter3 = r.Parameter3;
            });

            return result;
        }

        public static ChartRequest ToRequest(this UserChartDN uq)
        {
            var result = new ChartRequest(uq.QueryName)
            {
                GroupResults = uq.GroupResults,
                ChartScript = uq.ChartScript,
                Filters = uq.Filters.Select(qf => new Filter(qf.Token, qf.Operation, qf.Value)).ToList(),
                Orders = uq.Orders.Select(o => new Order(o.Token, o.OrderType)).ToList(),
            };

            result.Columns.ZipForeach(uq.Columns, (r, u) =>
            {
                r.Token = u.Token;
                r.DisplayName = u.DisplayName;
                r.Parameter1 = u.Parameter1;
                r.Parameter2 = u.Parameter2;
                r.Parameter3 = u.Parameter3;
            });

            return result;
        }


        public static Func<Type, int, Color?> GetChartColor = (type, id) => null;

        //Manual Json printer for performance and pretty print
        public static object DataJson(ChartRequest request, ResultTable resultTable)
        {
            int index = 0;
            var cols = request.Columns.Select((c, i) => new
            {
                name = "c" + i,
                displayName = request.Columns[i].ScriptColumn.DisplayName,
                title = c.GetTitle(),
                token = c.Token == null? null: c.Token.FullKey(),
                type =  c.Token == null? null: c.Token.GetChartColumnType().ToString(),               
                parameter1 = c.Parameter1,
                parameter2 = c.Parameter2,
                parameter3 = c.Parameter3,
                isGroupKey = c.IsGroupKey,
                converter = c.Token == null ? null : c.Converter(index++)
            }).ToList();

            if (!request.GroupResults)
            {
                cols.Insert(0, new
                {
                    name = "entity",
                    displayName = "Entity",
                    title = "",
                    token = ChartColumnType.Lite.ToString(),
                    type = "entity",
                    parameter1 = (string)null,
                    parameter2 = (string)null,
                    parameter3 = (string)null,
                    isGroupKey = (bool?)true,
                    converter = new Func<ResultRow, object>(r => r.Entity.Key())
                });
            }

            return new
            {
                columns = cols.ToDictionary(a => a.name, a => new
                {
                    a.title,
                    a.displayName,
                    a.token,
                    a.isGroupKey,
                    a.type,
                    parameter1 = a.parameter1,
                    parameter2 = a.parameter2,
                    parameter3 = a.parameter3,
                }),
                rows = resultTable.Rows.Select(r => cols.ToDictionary(a => a.name, a => a.converter == null? null: a.converter(r))).ToList()
            };
        }

        private static Func<ResultRow, object> Converter(this ChartColumnDN ct, int columnIndex)
        {
            if (ct == null || ct.Token == null)
                return null;

            var type = ct.Token.Type.UnNullify();

            if (type.IsLite())
            {
                return r =>
                {
                    Lite<IdentifiableEntity> l = (Lite<IdentifiableEntity>)r[columnIndex];
                    return new
                    {
                        key = l.TryCC(li => li.Key()),
                        toStr = l.TryCC(li => li.ToString()),
                        color = l == null ? "#555" : GetChartColor(l.EntityType, l.Id).TryToHtml(),
                    };
                };
            }
            else if (type.IsEnum)
            {
                var enumEntity = EnumEntity.Generate(type);

                return r =>
                {
                    Enum e = (Enum)r[columnIndex];
                    return new
                    {
                        key = Convert.ToInt32(e),
                        toStr = e.TryCC(en => en.NiceToString()),
                        color = e == null ? "#555" : GetChartColor(enumEntity, Convert.ToInt32(e)).TryToHtml(),
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

        public static List<List<ChartScriptDN>> PackInGroups(List<ChartScriptDN> scripts, int rowWidth)
        {
            var heigth = (scripts.Count() + rowWidth - 1) / rowWidth; //round-up division

            var result = 0.To(heigth).Select(a => new List<ChartScriptDN>()).ToList();

            var groups = scripts
                .OrderBy(s => s.Name)
                .GroupBy(s => s.ColumnsStructure)
                .OrderBy(g => g.First().Columns.Count(s=>!s.IsOptional))
                .ThenByDescending(g => g.Count())
                .ThenBy(g => g.Key)
                .ToList();

            foreach (var gr in groups)
            {
                var count = gr.Count();
                var list = result.FirstOrDefault(ls => ls.Count + count <= rowWidth);
                if (list != null)
                {
                    list.AddRange(gr);
                }
                else
                {
                    var remaining = gr.ToList();
                    foreach (var ls in result)
                    {
                        var available = Math.Min(rowWidth - ls.Count, remaining.Count);
                        if (available > 0)
                        {
                            var range = remaining.GetRange(0, available);
                            remaining.RemoveRange(0, available);
                            ls.AddRange(range);
                            if (remaining.IsEmpty())
                                break;
                        }
                    }
                }
            }

            return result;
        }

        public static void RemoveNotNullValidators()
        {
            Validator.GetOrCreatePropertyPack((ChartColumnDN c) => c.TokenString)
                .Validators.OfType<StringLengthValidatorAttribute>().SingleEx().AllowNulls = true;
        }
    }
}
