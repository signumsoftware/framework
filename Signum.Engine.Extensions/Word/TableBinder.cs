using DocumentFormat.OpenXml.Drawing.Charts;
using C = DocumentFormat.OpenXml.Drawing.Charts;
using Signum.Utilities;
using System;
using System.Collections.Generic;
using D = System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Drawing;
using Signum.Entities.UserQueries;
using Signum.Entities.Chart;
using Signum.Engine.UserQueries;
using Signum.Entities.UserAssets;
using Signum.Entities.DynamicQuery;
using Signum.Engine.DynamicQuery;
using Signum.Engine.Chart;

namespace Signum.Engine.Word
{
    public static class TableBinder
    {
        public static void ReplaceChartSpace(C.ChartSpace item, WordTemplateParameters parameters)
        {
            var chart = item.Descendants<C.Chart>().SingleEx();

            var plotArea = chart.Descendants<PlotArea>().SingleEx();
            var series = plotArea.Descendants<OpenXmlCompositeElement>().Where(a => a.LocalName == "ser").ToList();


            WordTemplateLogic.

            D.DataTable table = new D.DataTable();
            table.Columns.Add("Ciudad", typeof(string));
            table.Columns.Add("Junio", typeof(int));
            table.Columns.Add("Julio", typeof(int));
            table.Columns.Add("Agosto", typeof(int));
            table.Columns.Add("Septiembre", typeof(int));

            table.Rows.Add("Milán", 1, 2, 3,4 );
            table.Rows.Add("Roma", 3, 2, 1, 5);
            table.Rows.Add("Paris", 2, 5, 4, 6);
            table.Rows.Add("London", 6, 2, 1, 2);
            table.Rows.Add("Berlin", 2, 5, 4, 1);
            table.Rows.Add("Amsterdam",  5,2, 4, 3);


            SynchronizeNodes(series, table.Rows.Cast<D.DataRow>().ToList(),
                (ser, row, i, isCloned) =>
                {
                    if(isCloned)
                        ser.Descendants<SchemeColor>().ToList().ForEach(f => f.Remove());

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
                    apply(last, data[i], i, true);
                }
            }

            for (int i = data.Count; i < nodes.Count; i++)
            {
                nodes[i].Remove();
            }
        }

        private static void BindData(OpenXmlCompositeElement serie, D.DataRow dataRow, int index)
        {
            serie.Descendants<Formula>().ToList().ForEach(f => f.Remove());

            serie.GetFirstChild<Index>().Val = new UInt32Value((uint)index);
            serie.GetFirstChild<Order>().Val = new UInt32Value((uint)index);

            var setTxt = serie.Descendants<SeriesText>().SingleEx();
            setTxt.StringReference.Descendants<NumericValue>().SingleEx().Text = (string)dataRow[0];

            {
                var cat = serie.Descendants<CategoryAxisData>().SingleEx();
                cat.Descendants<PointCount>().SingleEx().Val = new UInt32Value((uint)dataRow.Table.Columns.Count - 1);
                var catValues = cat.Descendants<StringPoint>().ToList();
                SynchronizeNodes(catValues, dataRow.Table.Columns.Cast<D.DataColumn>().Skip(1).ToList(),
                  (sp, col, i, isCloned) =>
                  {
                      sp.Index = new UInt32Value((uint)i);
                      sp.Descendants<NumericValue>().Single().Text = col.ColumnName;
                  });
            }

            {
                var vals = serie.Descendants<Values>().SingleEx();
                vals.Descendants<PointCount>().SingleEx().Val = new UInt32Value((uint)dataRow.Table.Columns.Count - 1);
                var valsValues = vals.Descendants<NumericPoint>().ToList();
                SynchronizeNodes(valsValues, dataRow.ItemArray.Skip(1).ToList(),
                  (sp, val, i, isCloned) =>
                  {
                      sp.Index = new UInt32Value((uint)i);
                      sp.Descendants<NumericValue>().Single().Text = val.ToString();
                  });
            }
        }

        internal static D.DataTable UserQueryToDataTable(UserQueryEntity userQuery, WordTemplateLogic.WordContext ctx)
        {
            using (CurrentEntityConverter.SetCurrentEntity(ctx.Entity))
            {
                var request = UserQueryLogic.ToQueryRequest(userQuery);
                ResultTable resultTable = DynamicQueryManager.Current.ExecuteQuery(request);
                var dataTable = resultTable.ToDataTable();
                return dataTable;
            }
        }

        internal static D.DataTable UserChartToDataTable(UserChartEntity userChart, WordTemplateLogic.WordContext ctx)
        {
            using (CurrentEntityConverter.SetCurrentEntity(ctx.Entity))
            {
                var chartRequest = UserChartLogic.ToChartRequest(userChart);
                ResultTable result = ChartLogic.ExecuteChart(chartRequest);
                var dataTable = result.ToDataTable();
                var tokens = chartRequest.Columns.Where(a => a.Token != null).ToList();
                if (chartRequest.GroupResults && tokens.Count(a => a.IsGroupKey.Value) == 2 && tokens.Count(a => !a.IsGroupKey.Value) == 1)
                {
                    var firstKeyIndex = tokens.FindIndex(a => a.IsGroupKey.Value);
                    var secondKeyIndex = tokens.FindIndex(firstKeyIndex + 1, a => a.IsGroupKey.Value);
                    var valueIndex = tokens.FindIndex(a => !a.IsGroupKey.Value);
                    dataTable = PivotTable(dataTable, firstKeyIndex, secondKeyIndex, valueIndex);
                }
                 

                return result.ToDataTable();
            }
        }

        public static string Null = "- NULL -";

        private static D.DataTable PivotTable(D.DataTable dataTable, int firstKeyIndex, int secondKeyIndex, int valueIndex)
        {
            Dictionary<object, Dictionary<object, object>> dictionary = dataTable.Rows.Cast<D.DataRow>()
                .AgGroupToDictionary(
                row => row[firstKeyIndex] ?? Null,
                gr => gr.ToDictionaryEx(row => row[secondKeyIndex] ?? Null, row => row[valueIndex]));

            var secondKeys = dictionary.Values.SelectMany(d=>d.Keys).Distinct();

            var firstColumn = dataTable.Columns[firstKeyIndex];
            var valueColumn = dataTable.Columns[valueIndex];

            var result = new D.DataTable();
            result.Columns.Add(new D.DataColumn(firstColumn.ColumnName, firstColumn.DataType));
            foreach (var item in secondKeys)
                result.Columns.Add(new D.DataColumn(item.ToString(), valueColumn.DataType));

            foreach (var kvp in dictionary)
            {
                result.Rows.Add(secondKeys.Select(k => kvp.Value.TryGetC(k)).PreAnd(kvp.Key).ToArray());
            }

            return result;
        }
    }
}
