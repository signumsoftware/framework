using Drawing = DocumentFormat.OpenXml.Drawing;
using Presentation = DocumentFormat.OpenXml.Presentation;
using Charts = DocumentFormat.OpenXml.Drawing.Charts;
using Signum.Utilities;
using System;
using System.Collections.Generic;
using Data = System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DocumentFormat.OpenXml;
using Signum.Entities.UserQueries;
using Signum.Entities.Chart;
using Signum.Engine.UserQueries;
using Signum.Entities.UserAssets;
using Signum.Entities.DynamicQuery;
using Signum.Engine.DynamicQuery;
using Signum.Engine.Chart;
using Signum.Entities;
using DocumentFormat.OpenXml.Packaging;
using System.Globalization;

namespace Signum.Engine.Word
{
    public static class TableBinder
    {
        internal static void ProcessTables(OpenXmlPart part, WordTemplateParameters parameters)
        {
            var graphicFrames = part.RootElement.Descendants().Where(a => a.LocalName == "graphicFrame").ToList();
            foreach (var item in graphicFrames)
            {
                var nonVisualProps = item.Descendants().SingleOrDefaultEx(a => a.LocalName == "cNvPr");
                var title = GetTitle(nonVisualProps);
                
                Data.DataTable table = GetDataTable(parameters, title);
                if (table != null)
                {
                    var chartRef = item.Descendants<Charts.ChartReference>().SingleEx();
                    OpenXmlPart chartPart = part.GetPartById(chartRef.Id.Value);
                    var chart = chartPart.RootElement.Descendants<Charts.Chart>().SingleEx();
                    TableBinder.ReplaceChart(chart, table);
                }
            }
        }

        static string GetTitle(OpenXmlElement nonVisualProps)
        {
            if (nonVisualProps is Drawing.NonVisualDrawingProperties)
                return ((Drawing.NonVisualDrawingProperties)nonVisualProps).Title.Value;

            if (nonVisualProps is Presentation.NonVisualDrawingProperties)
                return ((Presentation.NonVisualDrawingProperties)nonVisualProps).Title.Value;
            
            throw new NotImplementedException("Imposible to get the Title from " + nonVisualProps.GetType().FullName);
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

                        BindData(ser, row, i);
                    });
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

        private static void BindData(OpenXmlCompositeElement serie, Data.DataRow dataRow, int index)
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
                      sp.Descendants<Charts.NumericValue>().Single().Text = 
                        val == null ? null: 
                        (val is IFormattable) ? ((IFormattable) val).ToString(null, CultureInfo.InvariantCulture) : 
                        val.ToString();
                  });
            }
        }



        private static Data.DataTable GetDataTable(WordTemplateParameters parameters, string title)
        {
            var tableSource = title.TryAfter("TableSource:");
            if (tableSource != null)
            {
                var ts = parameters.Template.TableSources.Single(a => a.Key == tableSource);
                var table = WordTemplateLogic.ToDataTable.Invoke(ts.Source.Retrieve(), new WordTemplateLogic.WordContext
                {
                    Entity = (Entity)parameters.Entity,
                    SystemWordTemplate = parameters.SystemWordTemplate,
                    Template = parameters.Template
                });

                return table;
            }

            return null;
        }


        internal static Data.DataTable UserQueryToDataTable(UserQueryEntity userQuery, WordTemplateLogic.WordContext ctx)
        {
            using (CurrentEntityConverter.SetCurrentEntity(ctx.Entity))
            {
                var request = UserQueryLogic.ToQueryRequest(userQuery);
                ResultTable resultTable = DynamicQueryManager.Current.ExecuteQuery(request);
                var dataTable = resultTable.ToDataTable();
                return dataTable;
            }
        }

        internal static Data.DataTable UserChartToDataTable(UserChartEntity userChart, WordTemplateLogic.WordContext ctx)
        {
            using (CurrentEntityConverter.SetCurrentEntity(ctx.Entity))
            {
                var chartRequest = UserChartLogic.ToChartRequest(userChart);
                ResultTable result = ChartLogic.ExecuteChart(chartRequest);
                var tokens = chartRequest.Columns.Where(a => a.Token != null).ToList();

                if (chartRequest.GroupResults && tokens.Count(a => a.IsGroupKey.Value) == 2 && tokens.Count(a => !a.IsGroupKey.Value) == 1)
                {
                    var firstKeyIndex = tokens.FindIndex(a => a.IsGroupKey.Value);
                    var secondKeyIndex = tokens.FindIndex(firstKeyIndex + 1, a => a.IsGroupKey.Value);
                    var valueIndex = tokens.FindIndex(a => !a.IsGroupKey.Value);
                    return result.ToDataTablePivot(secondKeyIndex, firstKeyIndex, valueIndex);
                }
                else
                    return result.ToDataTable();
            }
        }

      
    }
}
