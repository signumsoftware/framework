import * as React from 'react'
import * as d3 from 'd3'
import * as ChartClient from '../ChartClient';
import * as ChartUtils from './Components/ChartUtils';
import { translate, scale, rotate, skewX, skewY, matrix, scaleFor } from './Components/ChartUtils';
import { ChartRow, ChartTable } from '../ChartClient';
import InitialMessage from './Components/InitialMessage';
import { KeyCodes } from '@framework/Components';
import { TextRectangle } from './StackedLines';

export default function renderPie({ data, width, height, parameters, loading, onDrillDown, initialLoad, memo, chartRequest, dashboardFilter }: ChartClient.ChartScriptProps): React.ReactElement<any> {

  if (data == null || data.rows.length == 0)
    return (
      <svg direction="ltr" width={width} height={height}>
        <InitialMessage data={data} x={width / 2} y={height / 2} loading={loading} />
      </svg>
    );

  var keyColumn = data.columns.c0!;
  var valueColumn = data.columns.c1! as ChartClient.ChartColumn<number>;

  var pInnerRadius = parameters.InnerRadious || "0";
  var pSort = parameters.Sort;
  var pValueAsPercent = parameters.ValueAsNumberOrPercent;
  var pValueAsNumber = parameters.ValueAsNumberOrPercent;
  var dataTotal = data.rows.sum(r => valueColumn.getValue(r));

  var size = d3.scaleLinear()
    .domain([0, d3.max(data.rows, r => valueColumn.getValue(r))!])
    .range([0, 1]);
  var outerRadious = d3.min([width / 2, height])! / 3;
  var rInner = outerRadious * parseFloat(pInnerRadius);
  var color = ChartUtils.colorCategory(parameters, data.rows.map(r => keyColumn.getValueKey(r)), memo);

  var pie = d3.pie<ChartRow>()
    .sort(pSort == "Ascending" ? ((a, b) => d3.descending(size(valueColumn.getValue(a)), size(valueColumn.getValue(b)))) :
      pSort == "Descending" ? ((a, b) => d3.ascending(size(valueColumn.getValue(a)), size(valueColumn.getValue(b)))) :
        (a, b) => 0)
    .value(r => size(valueColumn.getValue(r)));

  var arc = d3.arc<d3.PieArcDatum<ChartRow>>()
    .outerRadius(outerRadious)
    .innerRadius(rInner);

  var legendRadius = 1.1;

  var detector = ChartClient.getActiveDetector(dashboardFilter, chartRequest);

  var orderedPie = pie(data.rows).orderBy(s => keyColumn.getValueKey(s.data));

  return (
    <svg direction="ltr" width={width} height={height}>
      <g className="shape" transform={translate(width / 2, height / 2)}>
        {orderedPie.map(slice => {
          var active = detector?.(slice.data);
          var m = (slice.endAngle + slice.startAngle) / 2;
          var cuadr = Math.floor(12 * m / (2 * Math.PI));
          var active = detector?.(slice.data);

          var isRight = m < Math.PI;

          var textAnchor = isRight ? 'start' : 'end';

          return (
            <g key={slice.index} className="slice hover-group">
              <title>{`${keyColumn.getValueNiceName(slice.data)}: ${valueColumn.getValue(slice.data)}`}</title>
              <path className="shape sf-transition hover-target" d={arc(slice)!}
                opacity={active == false ? .5 : undefined}
                stroke={active == true ? "black" : undefined}
                strokeWidth={active == true ? 3 : undefined}
                transform={initialLoad ? scale(0, 0) : scale(1, 1)}
                fill={keyColumn.getValueColor(slice.data) ?? color(keyColumn.getValueKey(slice.data))}
                shapeRendering="initial"
                onClick={e => onDrillDown(slice.data, e)} cursor="pointer">
              </path>
              <g key={slice.index} className="color-legend">
                <TextRectangle className="color-legend sf-chart-strong sf-transition"
                  rectangleAtts={{ fill: "transparent" }}
                  transform={translate(
                    Math.sin(m) * outerRadious * legendRadius,
                    -Math.cos(m) * outerRadious * legendRadius)}
                  opacity={active == false ? .5 : undefined}
                  textAnchor={textAnchor}
                  dominantBaseline="central"
                  fontWeight={active == true ? "bold" : undefined}
                  fill={keyColumn.getValueColor(slice.data) ?? color(keyColumn.getValueKey(slice.data))}
                  onClick={e => onDrillDown(slice.data, e)} cursor="pointer">
                  {((slice.endAngle - slice.startAngle) < (Math.PI / 16)) ? '' : pValueAsPercent == "Percent" ?
                    `${keyColumn.getValueNiceName(slice.data)} : ${Number(valueColumn.getValue(slice.data) / dataTotal).toLocaleString(undefined, { style: 'percent', minimumFractionDigits: 1 })}` :
                    pValueAsNumber == "Number" ?
                      `${keyColumn.getValueNiceName(slice.data)} : ${Number(valueColumn.getValue(slice.data)).toLocaleString(undefined, { style: 'decimal' })}` :
                      keyColumn.getValueNiceName(slice.data)}
                </TextRectangle>
              </g>
            </g>
          );
        })}
      </g>
      <InitialMessage data={data} x={width / 2} y={height / 2} loading={loading} />
    </svg>
  );
}
