using Drawing = DocumentFormat.OpenXml.Drawing;
using Presentation = DocumentFormat.OpenXml.Presentation;
using Charts = DocumentFormat.OpenXml.Drawing.Charts;
using Signum.Utilities;
using System;
using System.Collections.Generic;
using Data = System.Data;
using System.Linq;
using DocumentFormat.OpenXml;
using Signum.Entities.UserQueries;
using Signum.Entities.Chart;
using Signum.Engine.UserQueries;
using Signum.Entities.UserAssets;
using Signum.Entities.DynamicQuery;
using Signum.Engine.Chart;
using Signum.Entities;
using DocumentFormat.OpenXml.Packaging;
using System.Globalization;
using System.Reflection;
using Signum.Engine.Templating;
using Signum.Entities.Word;
using System.Threading;
using Signum.Engine.Basics;

namespace Signum.Engine.Word
{
    public static class TableBinder
    {
        internal static void ValidateTables(OpenXmlPart part, WordTemplateEntity template, List<TemplateError> errors)
        {
            var graphicFrames = part.RootElement.Descendants().Where(a => a.LocalName == "graphicFrame").ToList();
            foreach (var item in graphicFrames)
            {
                var nonVisualProps = item.Descendants().SingleOrDefaultEx(a => a.LocalName == "cNvPr");
                var title = GetTitle(nonVisualProps);

                if (title != null)
                {
                    var prefix = title.TryBefore(":");
                    if (prefix != null)
                    {
                        var provider = WordTemplateLogic.ToDataTableProviders.TryGetC(prefix);
                        if (provider == null)
                            errors.Add(new TemplateError(false, "No DataTableProvider '{0}' found (Possibilieties {1})".FormatWith(prefix, WordTemplateLogic.ToDataTableProviders.Keys.CommaOr())));
                        else
                        {
                            var error = provider.Validate(title.After(":"), template);
                            if (error != null)
                                errors.Add(new TemplateError(false, error));
                        }
                    }
                    
                }
            }
        }

        internal static void ProcessTables(OpenXmlPart part, WordTemplateParameters parameters)
        {
            var graphicFrames = part.RootElement.Descendants().Where(a => a.LocalName == "graphicFrame").ToList();
            foreach (var item in graphicFrames)
            {
                var nonVisualProps = item.Descendants().SingleEx(a => a.LocalName == "cNvPr");
                var title = GetTitle(nonVisualProps);

                Data.DataTable? dataTable = title != null ? GetDataTable(parameters, title) : null;
                if (dataTable != null)
                {
                    var chartRef = item.Descendants<Charts.ChartReference>().SingleOrDefaultEx();
                    if (chartRef != null)
                    {
                        OpenXmlPart chartPart = part.GetPartById(chartRef.Id.Value);
                        var chart = chartPart.RootElement.Descendants<Charts.Chart>().SingleEx();
                        ReplaceChart(chart, dataTable);
                    }
                    else
                    {
                        var table = item.Descendants<Drawing.Table>().SingleOrDefaultEx();
                        if (table != null)
                        {
                            ReplaceTable(table, dataTable);
                        }
                    }
                }
            }
        }

        static string? GetTitle(OpenXmlElement? nonVisualProps)
        {
            if (nonVisualProps is Drawing.NonVisualDrawingProperties draw)
                return draw.Title?.Value;

            if (nonVisualProps is Presentation.NonVisualDrawingProperties pres)
                return pres.Title?.Value;
            
            throw new NotImplementedException("Imposible to get the Title from " + nonVisualProps?.GetType().FullName);
        }

        static void SynchronizeNodes<N, T>(List<N> nodes, List<T> data, Action<N, T, int, bool> apply)
            where N : OpenXmlElement
        {
            for (int i = 0; i < data.Count; i++)
            {
                if (i < nodes.Count)
                {
                    apply(nodes[i], data[i], i, false);
                }
                else
                {
                    var last = nodes[nodes.Count - 1];
                    var clone = (N)last.CloneNode(true);
                    last.Parent.InsertAfter(clone, last);
                    apply(clone, data[i], i, true);
                }
            }

            for (int i = data.Count; i < nodes.Count; i++)
            {
                nodes[i].Remove();
            }
        }

        private static void ReplaceTable(Drawing.Table table, Data.DataTable dataTable)
        {
            var tableGrid = table.Descendants<Drawing.TableGrid>().SingleEx();
            SynchronizeNodes(
                tableGrid.Descendants<Drawing.GridColumn>().ToList(),
                dataTable.Columns.Cast<Data.DataColumn>().ToList(),
                (gc, dc, i, isCloned) => { });

            var rows = table.Descendants<Drawing.TableRow>().ToList();
            SynchronizeNodes(
               rows.FirstEx().Descendants<Drawing.TableCell>().ToList(),
               dataTable.Columns.Cast<Data.DataColumn>().ToList(),
               (gc, dc, i, isCloned) =>
               {
                   var text = gc.Descendants<Drawing.Text>().SingleOrDefaultEx();

                   if (text != null)
                       text.Text = dc.Caption ?? dc.ColumnName;
               });           
            
            SynchronizeNodes(
                rows.Skip(1).ToList(),
                dataTable.Rows.Cast<Data.DataRow>().ToList(),
                (tr, dr, j, isCloned) => 
                {
                    SynchronizeNodes(
                        tr.Descendants<Drawing.TableCell>().ToList(),
                        dataTable.Columns.Cast<Data.DataColumn>().Select(dc => dr[dc]).ToList(),
                        (gc, val, i, isCloned2) => { gc.Descendants<Drawing.Text>().SingleEx().Text = ToStringLocal(val); });
                });
        }

        public static void ReplaceChart(Charts.Chart chart, Data.DataTable table)
        {
            var plotArea = chart.Descendants<Charts.PlotArea>().SingleEx();
            var series = plotArea.Descendants<OpenXmlCompositeElement>().Where(a => a.LocalName == "ser").ToList();

            SynchronizeNodes(series, table.Rows.Cast<Data.DataRow>().ToList(),
                (ser, row, i, isCloned) =>
                {
                    if (isCloned)
                        ser.Descendants<Drawing.SchemeColor>().ToList().ForEach(f => f.Remove());

                    BindSerie(ser, row, i);
                });
        }


        private static void BindSerie(OpenXmlCompositeElement serie, Data.DataRow dataRow, int index)
        {
            serie.Descendants<Charts.Formula>().ToList().ForEach(f => f.Remove());

            serie.GetFirstChild<Charts.Index>().Val = new UInt32Value((uint)index);
            serie.GetFirstChild<Charts.Order>().Val = new UInt32Value((uint)index);

            var setTxt = serie.Descendants<Charts.SeriesText>().SingleEx();
            setTxt.StringReference.Descendants<Charts.NumericValue>().SingleEx().Text = dataRow[0]?.ToString();

            {
                var cat = serie.Descendants<Charts.CategoryAxisData>().SingleEx();
                cat.Descendants<Charts.PointCount>().SingleEx().Val = new UInt32Value((uint)dataRow.Table.Columns.Count - 1);
                var catValues = cat.Descendants<Charts.StringPoint>().ToList();
                SynchronizeNodes(catValues, dataRow.Table.Columns.Cast<Data.DataColumn>().Skip(1).ToList(),
                  (sp, col, i, isCloned) =>
                  {
                      sp.Index = new UInt32Value((uint)i);
                      sp.Descendants<Charts.NumericValue>().Single().Text = col.ColumnName;
                  });
            }

            {
                var vals = serie.Descendants<Charts.Values>().SingleEx();
                vals.Descendants<Charts.PointCount>().SingleEx().Val = new UInt32Value((uint)dataRow.Table.Columns.Count - 1);
                var valsValues = vals.Descendants<Charts.NumericPoint>().ToList();
                SynchronizeNodes(valsValues, dataRow.ItemArray.Skip(1).ToList(),
                  (sp, val, i, isCloned) =>
                  {
                      sp.Index = new UInt32Value((uint)i);
                      sp.Descendants<Charts.NumericValue>().Single().Text = ToStringLocal(val);
                  });
            }
        }

        private static string? ToStringLocal(object val)
        {
            return val == null ? null :
                (val is IFormattable) ? ((IFormattable)val).ToString(null, CultureInfo.InvariantCulture) :
                val.ToString();
        }

        private static Data.DataTable? GetDataTable(WordTemplateParameters parameters, string title)
        {
            var key = title.TryBefore(":");

            if (key == null)
                return null;

            var provider = WordTemplateLogic.ToDataTableProviders.GetOrThrow(key);

            var table = provider.GetDataTable(title.After(":"), new WordTemplateLogic.WordContext(parameters.Template, (Entity?)parameters.Entity, parameters.Model));

            return table;
        }
    }

    public class ModelDataTableProvider : IWordDataTableProvider
    {
        public Data.DataTable GetDataTable(string suffix, WordTemplateLogic.WordContext ctx)
        {
            MethodInfo mi = GetMethod(ctx.Template, suffix);

            object? result;
            try
            {
                result = mi.Invoke(ctx.Model, null);
            }
            catch (TargetInvocationException e)
            {
                e.InnerException!.PreserveStackTrace();

                throw e.InnerException!;
            }

            if (!(result is Data.DataTable dt))
                throw new InvalidOperationException($"Method '{suffix}' on '{ctx.Model!.GetType().Name}' did not return a DataTable");

            return dt;
        }

        private static MethodInfo GetMethod(WordTemplateEntity template, string method)
        {
            if (template.Model == null)
                throw new InvalidOperationException($"No WordModel found in template '{template}' to call '{method}'");

            var type = template.Model.ToType();
            var mi = type.GetMethod(method);
            if (mi == null)
                throw new InvalidOperationException($"No Method with name '{method}' found in type '{type.Name}'");


            return mi;
        }

        public string? Validate(string suffix, WordTemplateEntity template)
        {
            try
            {
                GetMethod(template, suffix);
                return null;
            }
            catch (Exception e)
            {
                return e.Message;
            }
        }
    }

    public class UserQueryDataTableProvider : IWordDataTableProvider
    {
        public Data.DataTable GetDataTable(string suffix, WordTemplateLogic.WordContext context)
        {
            var userQuery = Database.Query<UserQueryEntity>().SingleOrDefault(a => a.Guid == Guid.Parse(suffix));

            using (CurrentEntityConverter.SetCurrentEntity(context.Entity))
            {
                var request = UserQueryLogic.ToQueryRequest(userQuery);
                ResultTable resultTable = QueryLogic.Queries.ExecuteQuery(request);
                var dataTable = resultTable.ToDataTable();
                return dataTable;
            }
        }

        public string? Validate(string suffix, WordTemplateEntity template)
        {
            if (!Guid.TryParse(suffix, out Guid guid))
                return "Impossible to convert '{0}' in a GUID for a UserQuery".FormatWith(suffix);

            if (!Database.Query<UserQueryEntity>().Any(a => a.Guid == guid))
                return "No UserQuery with GUID={0} found".FormatWith(guid);

            return null;
        }
    }

    public class UserChartDataTableProvider : IWordDataTableProvider
    {
        public Data.DataTable GetDataTable(string suffix, WordTemplateLogic.WordContext context)
        {
            return GetDataTable(suffix, context.Entity!);
        }

        public Data.DataTable GetDataTable(string suffix, Entity entity)
        {
            var userChart = Database.Query<UserChartEntity>().SingleOrDefault(a => a.Guid == Guid.Parse(suffix));

            using (CurrentEntityConverter.SetCurrentEntity(entity))
            {
                var chartRequest = UserChartLogic.ToChartRequest(userChart);
                ResultTable result = ChartLogic.ExecuteChartAsync(chartRequest, CancellationToken.None).Result;
                var tokens = chartRequest.Columns.Select(a => a.Token).NotNull().ToList();

                //TODO: Too specific. Will be better if controlled by some parameters. 
                if (chartRequest.HasAggregates() && tokens.Count(a => !(a.Token is AggregateToken)) == 2 && tokens.Count(a => a.Token is AggregateToken) == 1)
                {
                    var firstKeyIndex = tokens.FindIndex(a => !(a.Token is AggregateToken));
                    var secondKeyIndex = tokens.FindIndex(firstKeyIndex + 1, a => !(a.Token is AggregateToken));
                    var valueIndex = tokens.FindIndex(a => a.Token is AggregateToken);
                    return result.ToDataTablePivot(secondKeyIndex, firstKeyIndex, valueIndex);
                }
                else
                    return result.ToDataTable();
            }
        }

        public string? Validate(string suffix, WordTemplateEntity template)
        {
            if (!Guid.TryParse(suffix, out Guid guid))
                return "Impossible to convert '{0}' in a GUID for a UserChart".FormatWith(suffix);

            if (!Database.Query<UserChartEntity>().Any(a => a.Guid == guid))
                return "No UserChart with GUID={0} found".FormatWith(guid);

            return null;
        }
    }
}
