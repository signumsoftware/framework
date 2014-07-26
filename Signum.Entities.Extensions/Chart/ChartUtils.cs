using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities.DynamicQuery;
using System.Reflection;
using Signum.Entities;
using Signum.Utilities;
using Signum.Entities.UserQueries;
using System.Drawing;
using System.ComponentModel;
using Signum.Entities.UserAssets;

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
                var pp = Validator.TryGetPropertyValidator(route);
                if (pp != null)
                {
                    DateTimePrecissionValidatorAttribute datetimePrecission = pp.Validators.OfType<DateTimePrecissionValidatorAttribute>().SingleOrDefaultEx();

                    if (datetimePrecission != null && datetimePrecission.Precision == DateTimePrecision.Days)
                        return true;

                }
            }

            return false;
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
                Owner = UserQueryUtils.DefaultRelated(),

                QueryName = request.QueryName,

                GroupResults = request.GroupResults,
                ChartScript = request.ChartScript,

                Filters = request.Filters.Select(f => new QueryFilterDN
                {
                    Token = new QueryTokenDN(f.Token),
                    Operation = f.Operation,
                    ValueString = FilterValueConverter.ToString(f.Value, f.Token.Type),
                }).ToMList(),

                Orders = request.Orders.Select(o => new QueryOrderDN
                {
                    Token = new QueryTokenDN(o.Token),
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
                Filters = uq.Filters.Select(qf => new Filter(qf.Token.Token, qf.Operation,
                    FilterValueConverter.Parse(qf.ValueString, qf.Token.Token.Type, qf.Operation == FilterOperation.IsIn))).ToList(),
                Orders = uq.Orders.Select(o => new Order(o.Token.Token, o.OrderType)).ToList(),
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
                token = c.Token == null ? null : c.Token.Token.FullKey(),
                type = c.Token == null ? null : c.Token.Token.GetChartColumnType().ToString(),               
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

            var type = ct.Token.Token.Type.UnNullify();

            if (type.IsLite())
            {
                return r =>
                {
                    Lite<IdentifiableEntity> l = (Lite<IdentifiableEntity>)r[columnIndex];
                    return new
                    {
                        key = l.Try(li => li.Key()),
                        toStr = l.Try(li => li.ToString()),
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
                        key = e == null ? (int?)null : Convert.ToInt32(e),
                        toStr = e.Try(en => en.NiceToString()),
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
                        toStr = ct.Token.Token.Format.HasText() ? e.TryToString(ct.Token.Token.Format) : r[columnIndex].TryToString()
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

        public static List<List<ChartScriptDN>> PackInGroups(IEnumerable<ChartScriptDN> scripts, int rowWidth)
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
    }

    public enum ChartMessage
    {
        [Description("{0} can only be created from the chart window")]
        _0CanOnlyBeCreatedFromTheChartWindow,
        [Description("{0} can only be created from the search window")]
        _0CanOnlyBeCreatedFromTheSearchWindow,
        Chart,
        [Description("Chart")]
        ChartToken,
        [Description("Chart settings")]
        Chart_ChartSettings,
        [Description("Dimension")]
        Chart_Dimension,
        [Description("Draw")]
        Chart_Draw,
        [Description("Group")]
        Chart_Group,
        [Description("Query {0} is not allowed")]
        Chart_Query0IsNotAllowed,
        [Description("Toggle info")]
        Chart_ToggleInfo,
        [Description("Edit Script")]
        EditScript,
        [Description("Colors for {0}")]
        ColorsFor0,
        CreatePalette,
        [Description("My Charts")]
        MyCharts,
        CreateNew,
        EditUserChart,
        ViewPalette,
        [Description("Chart for")]
        ChartFor,
        [Description("Chart of {0}")]
        ChartOf0,
        [Description("{0} is key, but {1} is an aggregate")]
        _0IsKeyBut1IsAnAggregate,
        [Description("{0} should be an aggregate")]
        _0ShouldBeAnAggregate,
        [Description("{0} should be set")]
        _0ShouldBeSet,
        [Description("{0} should be null")]
        _0ShouldBeNull,
        [Description("{0} is not {1}")]
        _0IsNot1,
        [Description("{0} is an aggregate, but the chart is not grouping")]
        _0IsAnAggregateButTheChartIsNotGrouping,
        [Description("{0} is not optional")]
        _0IsNotOptional,
        SavePalette,
        NewPalette,
        Data,
        ChooseABasePalette,
        DeletePalette,
    }

}
