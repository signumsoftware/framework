using System;
using System.Collections.Generic;
using System.Linq;
using Signum.Entities.DynamicQuery;
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

            var type = token.GetChartColumnType();

            if (type == null)
                return false;

            return Flag(ct, type.Value);
        }

        public static ChartColumnType? GetChartColumnType(this QueryToken token)
        {
            switch (QueryUtils.TryGetFilterType(token.Type))
            {
                case FilterType.Lite: return ChartColumnType.Lite;
                case FilterType.Boolean:
                case FilterType.Enum: return ChartColumnType.Enum;
                case FilterType.String:
                case FilterType.Guid: return ChartColumnType.String;
                case FilterType.Integer: return ChartColumnType.Integer;
                case FilterType.Decimal: return token.IsGroupable ? ChartColumnType.RealGroupable : ChartColumnType.Real;
                case FilterType.DateTime: return token.IsGroupable ? ChartColumnType.Date : ChartColumnType.DateTime;
            }

            return null;
        }

        public static bool Flag(ChartColumnType ct, ChartColumnType flag)
        {
            return (ct & flag) == flag;
        }

        public static bool IsDateOnly(QueryToken token)
        {
            if ((token is DatePartStartToken dt && (dt.Name == QueryTokenMessage.MonthStart || dt.Name == QueryTokenMessage.WeekStart)) ||
                token is DateToken)
                return true;

            PropertyRoute route = token.GetPropertyRoute();

            if (route != null && route.PropertyRouteType == PropertyRouteType.FieldOrProperty)
            {
                var pp = Validator.TryGetPropertyValidator(route);
                if (pp != null)
                {
                    DateTimePrecisionValidatorAttribute datetimePrecision = pp.Validators.OfType<DateTimePrecisionValidatorAttribute>().SingleOrDefaultEx();

                    if (datetimePrecision != null && datetimePrecision.Precision == DateTimePrecision.Days)
                        return true;

                }
            }

            return false;
        }
        
        public static bool SynchronizeColumns(this ChartScript chartScript, IChartBase chart)
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
                    chart.Columns.Add(new ChartColumnEmbedded());
                    result = true;
                }

                chart.Columns[i].parentChart = chart;
                chart.Columns[i].ScriptColumn = chartScript.Columns[i];

                if (!result)
                    result = chart.Columns[i].IntegrityCheck() != null;
            }

            if (chart.Columns.Count > chartScript.Columns.Count)
            {
                chart.Columns.RemoveRange(chartScript.Columns.Count, chart.Columns.Count - chartScript.Columns.Count);
                result = true;
            }

            if (chart.Parameters.Modified != ModifiedState.Sealed)
            {
                var chartScriptParameters = chartScript.AllParameters().ToList();

                if (chart.Parameters.Select(a => a.Name).OrderBy().SequenceEqual(chartScriptParameters.Select(a => a.Name).OrderBy()))
                {
                    foreach (var cp in chart.Parameters)
                    {
                        var sp = chartScriptParameters.FirstEx(a => a.Name == cp.Name);

                        cp.parentChart = chart;
                        cp.ScriptParameter = sp;
                        //if (cp.PropertyCheck(() => cp.Value).HasText())
                        //    cp.Value = sp.DefaultValue(cp.GetToken());
                    }
                }
                else
                {
                    var byName = chart.Parameters.ToDictionary(a => a.Name);
                    chart.Parameters.Clear();
                    foreach (var sp in chartScriptParameters)
                    {
                        var cp = byName.TryGetC(sp.Name);

                        if (cp != null)
                        {
                            cp.parentChart = chart;
                            cp.ScriptParameter = sp;

                            //if (cp.PropertyCheck(() => cp.Value).HasText())
                            //    cp.Value = sp.DefaultValue(cp.GetToken());
                        }
                        else
                        {
                            cp = new ChartParameterEmbedded
                            {
                                Name = sp.Name,
                                parentChart = chart,
                                ScriptParameter = sp,
                            };

                            cp.Value = sp.ValueDefinition.DefaultValue(sp.GetToken(chart));
                        }

                        chart.Parameters.Add(cp);
                    }
                }
            }

            return result;
        }
        
        public static ChartRequestModel ToRequest(this UserChartEntity uq)
        {
            var result = new ChartRequestModel(uq.QueryName)
            {
                ChartScript = uq.ChartScript,
                Filters = uq.Filters.ToFilterList(),
            };

            result.Columns.ZipForeach(uq.Columns, (r, u) =>
            {
                r.Token = u.Token;
                r.DisplayName = u.DisplayName;
                r.OrderByIndex = u.OrderByIndex;
                r.OrderByType = u.OrderByType;
            });

            result.Parameters.ForEach(r =>
            {
                r.Value = uq.Parameters.FirstOrDefault(u => u.Name == r.Name)?.Value ?? r.ScriptParameter.DefaultValue(r.ScriptParameter.GetToken(uq));
            });
            return result;
        }

        public static Func<Type, PrimaryKey, Color?> GetChartColor = (type, id) => null;

        

        private static Func<ResultRow, object> Converter(this ChartColumnEmbedded ct, int columnIndex)
        {
            if (ct == null || ct.Token == null)
                return null;

            var type = ct.Token.Token.Type.UnNullify();

            if (type.IsLite())
            {
                return r =>
                {
                    Lite<Entity> l = (Lite<Entity>)r[columnIndex];
                    return new
                    {
                        key = l?.Key(),
                        toStr = l?.ToString(),
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
                        key = e?.ToString(),
                        toStr = e?.NiceToString(),
                        color = e == null ? "#555" : GetChartColor(enumEntity, Convert.ToInt32(e)).TryToHtml(),
                    };
                };
            }
            else if (typeof(DateTime) == type)
            {
                var format = ct.Token.Token.Format;

                return r =>
                {
                    DateTime? e = (DateTime?)r[columnIndex];
                    if (e != null)
                        e = e.Value.ToUserInterface();

                    return new
                    {
                        key = e,
                        keyForFilter = e?.ToString("s"),
                        toStr = format.HasText() ? e?.ToString(format) : e?.ToString()
                    };
                };
            }
            else if (ct.Token.Token.Format.HasText())
            {
                var format = ct.Token.Token.Format;

                return r =>
                {
                    IFormattable e = (IFormattable)r[columnIndex];
                    return new
                    {
                        key = e,
                        toStr = format.HasText() ? e?.ToString(format, null) : e?.ToString()
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
                }; ;
        }


        internal static void FixParameters(IChartBase chart, ChartColumnEmbedded chartColumn)
        {
            int index = chart.Columns.IndexOf(chartColumn);

            foreach (var p in chart.Parameters.Where(p => p.ScriptParameter.ColumnIndex == index))
            {
                if (p.PropertyCheck(() => p.Value).HasText())
                    p.Value = p.ScriptParameter.DefaultValue(chartColumn.Token?.Token);
            }
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
        DrawChart,
        [Description("Group")]
        Chart_Group,
        [Description("Query {0} is not allowed")]
        Chart_Query0IsNotAllowed,
        [Description("Toggle info")]
        Chart_ToggleInfo,
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
        Preview,
    }

}
