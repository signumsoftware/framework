import * as React from 'react'
import * as d3 from 'd3'
import * as ChartUtils from './Components/ChartUtils';
import { translate, scale, rotate, skewX, skewY, matrix, scaleFor } from './Components/ChartUtils';
import { ChartTable, ChartColumn, ChartScriptProps, ChartRow } from '../ChartClient';
import TextEllipsis from './Components/TextEllipsis';
import { XKeyTicks, YScaleTicks, XTitle } from './Components/Ticks';
import { XAxis, YAxis } from './Components/Axis';
import { Rule } from './Components/Rule';
import InitialMessage from './Components/InitialMessage';
import type { ChartScriptHorizontalProps } from './Line';
import TextIfFits from './Components/TextIfFits';


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
        <YScaleTicks xRule={xRule} yRule={yRule} valueColumn={valueColumn} y={y}  />
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
        {keyValues.map(k => {
          var key = keyColumn.getKey(k);

          var row: ChartRow | undefined = rowsByKey[key];

          var active = detector?.(row);

          const posy = y(row ? valueColumn.getValue(row) : 0)!;

          return (
            <g className="shadow-group" key={key}>
              {row && <rect className="shape sf-transition shadow"
                opacity={active == false ? .5 : undefined}
                transform={(initialLoad ? scale(1, 0) : scale(1, 1)) + translate(x(key)!, -y(valueColumn.getValue(row))!)}
                height={y(valueColumn.getValue(row))}
                width={bandwidth}
                fill={keyColumn.getValueColor(row) ?? color(key)}
                cursor="pointer"
                onClick={e => onDrillDown(row!, e)}>
                <title>
                  {keyColumn.getValueNiceName(row) + ': ' + valueColumn.getValueNiceName(row)}
                </title>
              </rect>}
              {bandwidth > 15 &&
                (isMargin ?
                  <g className="x-label" transform={translate(0, labelMargin)} >
                    <TextEllipsis maxWidth={yRule.size('labels')} padding={labelMargin} className="x-label sf-transition"
                      transform={translate(x(keyColumn.getKey(key))! + bandwidth / 2, 0) + rotate(-90)}
                      dominantBaseline="middle"
                      fontWeight="bold"
                      fill={(keyColumn.getColor(key) ?? color(keyColumn.getKey(key)))}
                      textAnchor="end"
                      cursor="pointer"
                      onClick={e => onDrillDown({ c1: key }, e)}>
                      {keyColumn.getNiceName(key)}
                    </TextEllipsis>
                  </g> :
                  isInside ?
                    <g className="x-label" >
                      <TextEllipsis
                        maxWidth={posy >= size / 2 ? posy : size - posy} padding={labelMargin} className="x-label sf-transition"
                        transform={translate(x(keyColumn.getKey(key))! + bandwidth / 2, -posy) + rotate(-90)}
                        dominantBaseline="middle"
                        fontWeight="bold"
                        fill={posy >= size / 2 ? '#fff' : (keyColumn.getColor(key) ?? color(keyColumn.getKey(key)))}
                        dx={posy >= size / 2 ? -labelMargin : labelMargin}
                        textAnchor={posy >= size / 2 ? 'end' : 'start'}
                        onClick={e => onDrillDown({ c0: key }, e)}
                        cursor="pointer">
                        {keyColumn.getNiceName(key)}
                      </TextEllipsis>
                    </g> : null
                )}
              {parseFloat(parameters["NumberOpacity"]) > 0 && bandwidth > 15 && row &&
                <g className="numbers-label" >
                  <TextIfFits className="number-label sf-transition"
                    transform={translate(x(keyColumn.getValueKey(row))! + bandwidth / 2, -y(valueColumn.getValue(row))! / 2) + rotate(-90)}
                    maxWidth={y(valueColumn.getValue(row))!}
                    fill={parameters["NumberColor"] ?? "#000"}
                    dominantBaseline="middle"
                    opacity={parameters["NumberOpacity"]}
                    textAnchor="middle"
                    fontWeight="bold"
                    cursor="pointer"
                    onClick={e => onDrillDown(row!, e)}>
                    {valueColumn.getValueNiceName(row)}
                  </TextIfFits>
                </g>}
            </g>
          );
        })}
      </g>
    </>
  );
}
