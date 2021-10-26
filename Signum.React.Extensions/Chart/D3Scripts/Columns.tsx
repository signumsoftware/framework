import * as React from 'react'
import * as d3 from 'd3'
import * as ChartUtils from './Components/ChartUtils';
import { translate, scale, rotate, skewX, skewY, matrix, scaleFor } from './Components/ChartUtils';
import { ChartTable, ChartColumn, ChartScriptProps } from '../ChartClient';
import TextEllipsis from './Components/TextEllipsis';
import { XKeyTicks, YScaleTicks, XTitle } from './Components/Ticks';
import { XAxis, YAxis } from './Components/Axis';
import { Rule } from './Components/Rule';
import InitialMessage from './Components/InitialMessage';
import type { ChartScriptHorizontalProps } from './Line';


export default function renderColumns({ data, width, height, parameters, loading, onDrillDown, initialLoad, chartRequest, memo, dashboardFilter }: ChartScriptProps): React.ReactElement<any> {

  const isMargin = parameters["Labels"] == "Margin" || parameters["Labels"] == "MarginAll";
  const isInside = parameters["Labels"] == "Inside" || parameters["Labels"] == "InsideAll";
  const isAll = parameters["Labels"] == "MarginAll" || parameters["Labels"] == "InsideAll";

  var xRule = Rule.create({
    _1: 5,
    title: 15,
    _2: 10,
    labels: parseInt(parameters["UnitMargin"]),
    _3: 5,
    ticks: 4,
    content: '*',
    _4: 10,
  }, width);
  //xRule.debugX(chart)

  var yRule = Rule.create({
    _1: 10,
    legend: 15,
    _2: 5,
    content: '*',
    ticks: 4,
    _3: isMargin ? 5 : 0,
    labels: isMargin ? parseInt(parameters["LabelsMargin"]) : 0,
    _4: 10,
    title: 15,
    _5: 5,
  }, height);
  //yRule.debugY(chart);

  if (data == null || data.rows.length == 0)
    return (
      <svg direction="ltr" width={width} height={height}>
        <InitialMessage data={data} x={xRule.middle("content")} y={yRule.middle("content")} loading={loading} />
        <XAxis xRule={xRule} yRule={yRule} />
        <YAxis xRule={xRule} yRule={yRule} />
      </svg>
    );

  var keyColumn = data.columns.c0!;
  var valueColumn = data.columns.c1! as ChartColumn<number>;

  var keyValues = ChartUtils.completeValues(keyColumn, data.rows.map(r => keyColumn.getValue(r)), parameters['CompleteValues'], chartRequest.filterOptions, ChartUtils.insertPoint(keyColumn, valueColumn));

  var x = d3.scaleBand()
    .domain(keyValues.map(v => keyColumn.getKey(v)))
    .range([0, xRule.size('content')]);

  var y = scaleFor(valueColumn, data.rows.map(r => valueColumn.getValue(r)), 0, yRule.size('content'), parameters["Scale"]);

  var detector = dashboardFilter?.getActiveDetector(chartRequest);

  return (
    <svg direction="ltr" width={width} height={height}>
      <g opacity={dashboardFilter ? .5 : undefined}>
        <XTitle xRule={xRule} yRule={yRule} keyColumn={keyColumn} />
        <YScaleTicks xRule={xRule} yRule={yRule} valueColumn={valueColumn} y={y} />
      </g>

      {paintColumns({ xRule, yRule, x, y, keyValues, data, parameters, initialLoad, onDrillDown, colIndex: 0, colCount: 1, memo, detector })}

      <InitialMessage data={data} x={xRule.middle("content")} y={yRule.middle("content")} loading={loading} />
      <g opacity={dashboardFilter ? .5 : undefined}>
        <XAxis xRule={xRule} yRule={yRule} />
        <YAxis xRule={xRule} yRule={yRule} />
      </g>
    </svg>
  );
}


export function paintColumns({ xRule, yRule, x, y, keyValues, data, parameters, initialLoad, onDrillDown, colIndex, colCount, memo, detector }: ChartScriptHorizontalProps & {
  colIndex: number, colCount: number
}) {

  const isMargin = parameters["Labels"] == "Margin" || parameters["Labels"] == "MarginAll";
  const isInside = parameters["Labels"] == "Inside" || parameters["Labels"] == "InsideAll";
  const isAll = parameters["Labels"] == "MarginAll" || parameters["Labels"] == "InsideAll";

  var keyColumn = data.columns.c0!;
  var valueColumn = data.columns.c1! as ChartColumn<number>;

  var orderedRows = data.rows.orderBy(r => keyColumn.getValueKey(r));
  var color = parameters["ForceColor"] ? () => parameters["ForceColor"] :
    ChartUtils.colorCategory(parameters, orderedRows.map(r => keyColumn.getValueKey(r)!), memo);

  var size = yRule.size('content');
  var labelMargin = 10;

  const bandMargin = x.bandwidth() > 20 ? 2 : 0;

  const bandwidth = ((x.bandwidth() - bandMargin * 2) / colCount);
  const bandOffset = bandwidth * colIndex + bandMargin;

  var rowsByKey = data.rows.toObject(r => keyColumn.getValueKey(r));


  return (
    <>
      <g className="shape" transform={translate(xRule.start('content') + bandOffset, yRule.end('content'))}>
        {orderedRows.map(r => {
          var active = detector?.(r);
          var key = keyColumn.getValueKey(r);

          return (
            <rect key={key} className="shape sf-transition"
              opacity={active == false ? .5 : undefined}
              stroke={active == true ? "black" : bandwidth > 4 ? '#fff' : undefined}
              strokeWidth={active == true ? 3 : undefined}
              transform={(initialLoad ? scale(1, 0) : scale(1, 1)) + translate(x(key)!, -y(valueColumn.getValue(r))!)}
              height={y(valueColumn.getValue(r))}
              width={bandwidth}
              fill={keyColumn.getValueColor(r) ?? color(key)}
              cursor="pointer"
              onClick={e => onDrillDown(r, e)}>
              <title>
                {keyColumn.getValueNiceName(r) + ': ' + valueColumn.getValueNiceName(r)}
              </title>
            </rect>
          );
        })}
      </g>

      {bandwidth > 15 &&
        (isMargin ?
          <g className="x-label" transform={translate(xRule.start('content') + bandOffset, yRule.start('labels'))}>
            {(isAll ? keyValues : orderedRows.map(r => keyColumn.getValue(r))).map(k => < TextEllipsis key={keyColumn.getKey(k)} maxWidth={yRule.size('labels')} padding={labelMargin} className="x-label sf-transition"
              transform={translate(x(keyColumn.getKey(k))! + bandwidth / 2, 0) + rotate(-90)}
              dominantBaseline="middle"
              fontWeight="bold"
              fill={(keyColumn.getColor(k) ?? color(keyColumn.getKey(k)))}
              textAnchor="end"
              cursor="pointer"
              onClick={e => onDrillDown({ c1: k }, e)}>
              {keyColumn.getNiceName(k)}
            </TextEllipsis>)}
          </g> :
          isInside ?
            <g className="x-label" transform={translate(xRule.start('content') + bandOffset, yRule.end('content'))}>
              {(isAll ? keyValues : orderedRows.map(r => keyColumn.getValue(r))).map(k => {
                const row = rowsByKey[keyColumn.getKey(k)];
                const posy = y(row ? valueColumn.getValue(row) : 0)!;
                return (
                  <TextEllipsis key={keyColumn.getKey(k)} maxWidth={posy >= size / 2 ? posy : size - posy} padding={labelMargin} className="x-label sf-transition"
                    transform={translate(x(keyColumn.getKey(k))! + bandwidth / 2, -posy) + rotate(-90)}
                    dominantBaseline="middle"
                    fontWeight="bold"
                    fill={posy >= size / 2 ? '#fff' : (keyColumn.getColor(k) ?? color(keyColumn.getKey(k)))}
                    dx={posy >= size / 2 ? -labelMargin : labelMargin}
                    textAnchor={posy >= size / 2 ? 'end' : 'start'}
                    onClick={e => onDrillDown({ c0: k }, e)}
                    cursor="pointer">
                    {keyColumn.getNiceName(k)}
                  </TextEllipsis>);
              })}
            </g> : null
        )}

      {parseFloat(parameters["NumberOpacity"]) > 0 && bandwidth > 15 &&
        <g className="numbers-label" transform={translate(xRule.start('content') + bandOffset, yRule.end('content'))}>
          {orderedRows
            .filter(r => y(valueColumn.getValue(r))! > 10)
            .map(r => <text key={keyColumn.getValueKey(r)} className="number-label sf-transition"
              transform={translate(x(keyColumn.getValueKey(r))! + bandwidth / 2, -y(valueColumn.getValue(r))! / 2) + rotate(-90)}
              fill={parameters["NumberColor"] ?? "#000"}
              dominantBaseline="middle"
              opacity={parameters["NumberOpacity"]}
              textAnchor="middle"
              fontWeight="bold"
              cursor="pointer"
              onClick={e => onDrillDown(r, e)}>
              {valueColumn.getValueNiceName(r)}
            </text>)}
        </g>}

    </>
  );
}
