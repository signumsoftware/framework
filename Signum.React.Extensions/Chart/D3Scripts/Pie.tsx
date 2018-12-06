import * as React from 'react'
import * as d3 from 'd3'
import * as ChartClient from '../ChartClient';
import * as ChartUtils from '../Templates/ChartUtils';
import { translate, scale, rotate, skewX, skewY, matrix, scaleFor, rule, ellipsis } from '../Templates/ChartUtils';
import { ChartRow, ChartTable } from '../ChartClient';
import ReactChartBase from './ReactChartBase';


export default class PieChart extends ReactChartBase {

  renderChart(data: ChartTable, width: number, height: number): React.ReactElement<any> {

    var keyColumn = data.columns.c0!;
    var valueColumn = data.columns.c1! as ChartClient.ChartColumn<number>;

    var pInnerRadius = data.parameters.InnerRadious || "0";
    var pSort = data.parameters.Sort;

    var size = d3.scaleLinear()
      .domain([0, d3.max(data.rows, r => valueColumn.getValue(r))!])
      .range([0, 1]);

    var outerRadious = d3.min([width / 2, height])! / 3;
    var rInner = outerRadious * parseFloat(pInnerRadius);
    var color = d3.scaleOrdinal(ChartUtils.getColorScheme(data.parameters["ColorCategory"], parseInt(data.parameters["ColorCategorySteps"])))
      .domain(data.rows.map(r => keyColumn.getValueKey(r)));


    var pie = d3.pie<ChartRow>()
      .sort(pSort == "Ascending" ? ((a, b) => d3.descending(size(valueColumn.getValue(a)), size(valueColumn.getValue(b)))) :
        pSort == "Descending" ? ((a, b) => d3.ascending(size(valueColumn.getValue(a)), size(valueColumn.getValue(b)))) : null)
      .value(r => size(valueColumn.getValue(r)));

    var arc = d3.arc<d3.PieArcDatum<ChartRow>>()
      .outerRadius(outerRadious)
      .innerRadius(rInner);

    var cx = (width / 2),
      cy = (height / 2),
      legendRadius = 1.2;



    return (
      <svg direction="ltr" width={width} height={height}>
        <g className="shape" transform={translate(width / 2, height / 2)}>
          {pie(data.rows).map(slice => <g key={slice.index} className="slice">
            <path className="shape" d={arc(slice)!} fill={keyColumn.getValueColor(slice.data) || color(keyColumn.getValueKey(slice.data))} shapeRendering="initial" onClick={e => this.props.onDrillDown(slice.data)} cursor="pointer">
              <title>
                {keyColumn.getValueNiceName(slice.data) + ': ' + valueColumn.getValueNiceName(slice.data)}
              </title>
            </path>
          </g>)}
        </g>
        <g className="color-legend" transform={translate(cx, cy)}>
          {pie(data.rows).map(slice => {

            var m = (slice.endAngle + slice.startAngle) / 2;
            var cuadr = Math.floor(12 * m / (2 * Math.PI));

            return <g key={slice.index} className="color-legend">
              <text className="color-legend sf-chart-strong"
                x={Math.sin(m) * outerRadious * legendRadius}
                y={-Math.cos(m) * outerRadious * legendRadius}
                textAnchor={(1 <= cuadr && cuadr <= 4) ? 'start' : (7 <= cuadr && cuadr <= 10) ? 'end' : 'middle'}
                fill={keyColumn.getValueColor(slice.data) || color(keyColumn.getValueKey(slice.data))}
                onClick={e => this.props.onDrillDown(slice.data)} cursor="pointer">
                {((slice.endAngle - slice.startAngle) >= (Math.PI / 16)) ? keyColumn.getValueNiceName(slice.data) : ''}
              </text>
            </g>;
          })}
        </g>
      </svg>
    );
  }
}
