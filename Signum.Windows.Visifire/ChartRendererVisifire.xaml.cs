using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Visifire.Charts;
using Signum.Entities.DynamicQuery;
using Signum.Entities.Chart;
using Signum.Utilities;
using System.Runtime.Serialization;
using Signum.Entities;
using System.Globalization;
using Signum.Windows;

namespace Signum.Windows.Chart
{
    /// <summary>
    /// Interaction logic for ChartRenderer.xaml
    /// </summary>
    public partial class ChartRendererVisifire : ChartRendererBase
    {
        public ChartRendererVisifire()
        {
            InitializeComponent();
        }

        public override void DrawChart()
        {
            chart.Series.Clear();

            foreach (var ds in CreateSerie())
            {
                chart.Series.Add(ds);
            }

            chart.AxesX.Clear();
            chart.AxesY.Clear();

            if (ChartRequest.Chart.ChartType != ChartType.Pie && ChartRequest.Chart.ChartType != ChartType.Doughnout)
            {
                chart.AxesX.Add(new Axis { Title = ChartRequest.Chart.Dimension1.GetTitle() });

                if (ChartRequest.Chart.ChartResultType == ChartResultType.TypeValue || ChartRequest.Chart.ChartResultType == ChartResultType.TypeTypeValue)
                    chart.AxesY.Add(new Axis { Title = ChartRequest.Chart.Value1.GetTitle() });
                else
                    chart.AxesY.Add(new Axis { Title = ChartRequest.Chart.Dimension2.GetTitle() });
            }
        }

        static object NullValue = "- None -"; 

        private DataSeries[] CreateSerie()
        {
            //LabelText = "#AxisXLabel, #YValue",

            switch (ChartRequest.Chart.ChartResultType)
            {
                case ChartResultType.TypeValue:
                    {
                        return new[]
                        {
                            new DataSeries 
                            { 
                                RenderAs = ToRenderAs(ChartRequest.Chart.ChartType),
                            }
                            .AddPoints(ResultTable.Rows.Select(r => 
                                new DataPoint 
                                { 
                                    AxisXLabel = Format(r[0] ?? NullValue, ChartRequest.Chart.Dimension1.Token.Format), 
                                    YValue = ToDouble(r[1], ChartTokenName.Value1),
                                    Tag = r,
                                }.Do(p=>p.MouseLeftButtonDown+=dp_MouseDoubleClick) 
                             ))
                        };
                    }
                case ChartResultType.TypeTypeValue:
                    {
                        List<object> series = ResultTable.Rows.Select(r => r[0] ?? NullValue).Distinct().ToList();


                        Dictionary<object, Dictionary<object, ResultRow>> dic1dic0 = ResultTable.Rows.AgGroupToDictionary(r => r[1] ?? NullValue, gr => gr.ToDictionary(r => r[0] ?? NullValue));

                        //List<object> subSeries = ResultTable.Rows.Select(r => r[1] ?? NullValue).Distinct().ToList();

                        //double?[,] array = ResultTable.Rows.ToArray(
                        //    r => (double?)ToDouble(r[2], ChartTokenName.Value1), 
                        //    r => series.IndexOf(r[0] ?? NullValue), 
                        //    r => subSeries.IndexOf(r[1] ?? NullValue), series.Count, subSeries.Count);

                        return dic1dic0.Select((ss, j) =>
                            new DataSeries
                            {
                                LegendText = ss.Key.ToString(),
                                RenderAs = ToRenderAs(ChartRequest.Chart.ChartType),
                                //YValueFormatString = ChartRequest.FirstValue.Format
                            }.AddPoints(series.Select((s, i) =>
                            {
                                DataPoint p = new DataPoint()
                                {
                                    AxisXLabel = Format(s, ChartRequest.Chart.Dimension1.Token.Format)
                                };

                                ResultRow row = ss.Value.TryGetC(s);
                                if (row == null)
                                {
                                    if (ChartRequest.Chart.ChartType == ChartType.StackedAreas || ChartRequest.Chart.ChartType == ChartType.TotalAreas)
                                        p.YValue = 0;

                                    return p;
                                }

                                p.YValue = ToDouble(row[2], ChartTokenName.Value1);

                                p.Tag = row;

                                p.MouseLeftButtonDown += dp_MouseDoubleClick;

                                return p;
                            }))).ToArray();
                    }
                case ChartResultType.Points:
                    return new[]{ new DataSeries
                    { 
                        RenderAs = ToRenderAs(ChartRequest.Chart.ChartType),
                        //MaxWidth = this.Width
                        //XValueFormatString = ChartRequest.FirstDimension.Format,
                        //YValueFormatString = ChartRequest.SecondDimension.Format,
                    }
                    .AddPoints(ResultTable.Rows.Select(r => new DataPoint
                    {
                         XValue = ToDouble(r[0], ChartTokenName.Dimension1),
                         YValue = ToDouble(r[1], ChartTokenName.Dimension2),
                         Color = ToColor(r[2]),
                         Tag = r,
                    }.Do(p=>p.MouseLeftButtonDown+=dp_MouseDoubleClick) 
                    ))
                    };
                case ChartResultType.Bubbles:
                    return new[]{ 
                        new DataSeries
                        { 
                            RenderAs = ToRenderAs(ChartRequest.Chart.ChartType),
                            //XValueFormatString = ChartRequest.FirstDimension.Format,
                            //YValueFormatString = ChartRequest.SecondDimension.Format,
                            //ZValueFormatString = ChartRequest.SecondValue.Format,
                        }
                        .AddPoints(ResultTable.Rows.Select(r => new DataPoint
                        {
                             XValue = ToDouble(r[0], ChartTokenName.Dimension1),
                             YValue = ToDouble(r[1], ChartTokenName.Dimension2),
                             Color = ToColor(r[2]),
                             ZValue = ToDouble(r[3], ChartTokenName.Value2),
                             Tag = r,
                        }.Do(p=>p.MouseLeftButtonDown+=dp_MouseDoubleClick) 
                    ))
                    };
            }

            throw new InvalidOperationException();
            
        }

        void dp_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount >= 2)
            {
                DataPoint dp = (DataPoint)sender;

                var cw = this.VisualParents().OfType<ChartWindow>().FirstOrDefault();

                cw.ShowRow((ResultRow)dp.Tag);
            }
        }


        string Format(object val, string format)
        {
            if (val == null)
                return NullValue.ToString();

            if (val is IFormattable && format.HasText())
                return ((IFormattable)val).ToString(format, CultureInfo.CurrentCulture);

            return val.ToString();
        }

        //Comparison<object> GetComparer(Type type)
        //{
        //    type = type.UnNullify();

        //    if(typeof(IComparable).IsAssignableFrom(type))
        //        return Nullify((IComparable c)=>c); 

        //    if (type.IsLite())
        //        return Nullify((Lite b) => b.ToStr);

        //    return Nullify((object o) => o.ToString());
        //}

        Comparison<object> Nullify<T>(Func<T, IComparable> f)
        {
            return (a, b) =>
            {
                if (a == NullValue)
                    if (b == NullValue)
                        return 0;
                    else
                        return -1;

                if (b == NullValue)
                    return 1;
                return f((T)a).CompareTo(f((T)b));
            };
        }

        double ToDouble(object value, ChartTokenName name)
        {
            if (value == null)
                throw new ChartNullException(name);

            return Convert.ToDouble(value); 
        }

        private Brush ToColor(object value)
        {
            if (value == null)
                return Brushes.Black;

            return null;
        }

        private RenderAs ToRenderAs(ChartType ct)
        {
            switch (ct)
            {
                case ChartType.Pie:
                    return RenderAs.Pie;
                case ChartType.Doughnout:
                    return RenderAs.Doughnut;

                case ChartType.Columns:
                case ChartType.MultiColumns:
                    return RenderAs.Column;

                case ChartType.Bars:
                case ChartType.MultiBars:
                    return RenderAs.Bar;

                case ChartType.Lines:
                case ChartType.MultiLines:
                    return RenderAs.Line;

                case ChartType.StackedColumns:
                    return RenderAs.StackedColumn;
                case ChartType.StackedBars:
                    return RenderAs.StackedBar;
                case ChartType.StackedAreas:
                    return RenderAs.StackedArea;

                case ChartType.TotalColumns:
                    return RenderAs.StackedColumn100;
                case ChartType.TotalBars:
                    return RenderAs.StackedBar100;
                case ChartType.TotalAreas:
                    return RenderAs.StackedArea100;

                case ChartType.Points:
                    return RenderAs.Point;

                case ChartType.Bubbles:
                    return RenderAs.Bubble;
            }

            throw new InvalidOperationException();
        }


    }

    public static class VisifireExtensions
    {
        public static DataSeries AddPoints(this DataSeries dataSeries, IEnumerable<DataPoint> collection)
        {
            dataSeries.DataPoints.Clear();
            foreach (var item in collection)
            {
                dataSeries.DataPoints.Add(item);
            }
            return dataSeries;
        }
    }

}
