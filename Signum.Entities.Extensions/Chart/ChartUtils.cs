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
					DateTimePrecissionValidatorAttribute datetimePrecission = pp.Validators.OfType<DateTimePrecissionValidatorAttribute>().SingleOrDefaultEx();

					if (datetimePrecission != null && datetimePrecission.Precision == DateTimePrecision.Days)
						return true;

				}
			}

			return false;
		}

		
		
		public static bool SynchronizeColumns(this ChartScriptEntity chartScript, IChartBase chart)
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
				if (chart.Parameters.Select(a => a.Name).OrderBy().SequenceEqual(chartScript.Parameters.Select(a => a.Name).OrderBy()))
				{
					foreach (var cp in chart.Parameters)
					{
						var sp = chartScript.Parameters.FirstEx(a => a.Name == cp.Name);

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
					foreach (var sp in chartScript.Parameters)
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

							cp.Value = sp.DefaultValue(sp.GetToken(chart));
						}

						chart.Parameters.Add(cp);
					}
				}
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

		public static UserChartEntity ToUserChart(this ChartRequest request)
		{
			var result = new UserChartEntity
			{
				Owner = UserQueryUtils.DefaultOwner(),

				QueryName = request.QueryName,

				GroupResults = request.GroupResults,
				ChartScript = request.ChartScript,

				Filters = request.Filters.Select(f => new QueryFilterEmbedded
				{
					Token = new QueryTokenEmbedded(f.Token),
					Operation = f.Operation,
					ValueString = FilterValueConverter.ToString(f.Value, f.Token.Type, allowSmart: true),
				}).ToMList(),

				Orders = request.Orders.Select(o => new QueryOrderEmbedded
				{
					Token = new QueryTokenEmbedded(o.Token),
					OrderType = o.OrderType
				}).ToMList()
			};

			result.Columns.ZipForeach(request.Columns, (u, r) =>
			{
				u.Token = r.Token;
				u.DisplayName = r.DisplayName;
			});

			result.Parameters.ForEach(u =>
			{
				u.Value = request.Parameters.FirstOrDefault(r => r.Name == u.Name).Value;
			});

			return result;
		}

		public static ChartRequest ToRequest(this UserChartEntity uq)
		{
			var result = new ChartRequest(uq.QueryName)
			{
				GroupResults = uq.GroupResults,
				ChartScript = uq.ChartScript,
				Filters = uq.Filters.Select(qf => new Filter(qf.Token.Token, qf.Operation,
					FilterValueConverter.Parse(qf.ValueString, qf.Token.Token.Type, qf.Operation.IsList(), allowSmart: true))).ToList(),
				Orders = uq.Orders.Select(o => new Order(o.Token.Token, o.OrderType)).ToList(),
			};

			result.Columns.ZipForeach(uq.Columns, (r, u) =>
			{
				r.Token = u.Token;
				r.DisplayName = u.DisplayName;
			});

			result.Parameters.ForEach(r =>
			{
				r.Value = uq.Parameters.FirstOrDefault(u => u.Name == r.Name)?.Value ?? r.ScriptParameter.DefaultValue(r.ScriptParameter.GetToken(uq));
			});
			return result;
		}


		public static Func<Type, PrimaryKey, Color?> GetChartColor = (type, id) => null;

		//Manual Json printer for performance and pretty print
		public static object DataJson(ChartRequest request, ResultTable resultTable)
		{
            int index = 0;
            var cols = request.Columns.Select((c, i) => new
			{
				name = "c" + i,
				displayName = c.ScriptColumn.DisplayName,
				title = c.GetTitle(),
				token = c.Token?.Token.FullKey(),
				type = c.Token?.Token.GetChartColumnType().ToString(),               
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
					isGroupKey = (bool?)true,
					converter = new Func<ResultRow, object>(r => r.Entity.Key())
				});
			}

			var parameters = request.Parameters.ToDictionary(p=> p.Name, p => p.Value);

			return new
			{
				columns = cols.ToDictionary(a => a.name, a => new
				{
					a.title,
					a.displayName,
					a.token,
					a.isGroupKey,
					a.type,
				}),
				parameters = request.ChartScript.Parameters.ToDictionary(a => a.Name, a => parameters.TryGetC(a.Name) ?? a.DefaultValue(a.GetToken(request))),
				rows = resultTable.Rows.Select(r => cols.ToDictionary(a => a.name, a => a.converter == null ? null : a.converter(r))).ToList()
			};
		}

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
				};;
		}

		public static List<List<ChartScriptEntity>> PackInGroups(IEnumerable<ChartScriptEntity> scripts, int rowWidth)
		{
			var heigth = (scripts.Count() + rowWidth - 1) / rowWidth; //round-up division

			var result = 0.To(heigth).Select(a => new List<ChartScriptEntity>()).ToList();

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
		Preview,
	}

}
