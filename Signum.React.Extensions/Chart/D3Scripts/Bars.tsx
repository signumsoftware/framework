import * as React from 'react'
import * as d3 from 'd3'
import * as ChartClient from '../ChartClient';
import * as ChartUtils from './Components/ChartUtils';
import { translate, scale, rotate, skewX, skewY, matrix, scaleFor } from './Components/ChartUtils';
import { ChartRow, ChartScriptProps } from '../ChartClient';
import TextEllipsis from './Components/TextEllipsis';
import { XKeyTicks, YScaleTicks, YKeyTicks, XScaleTicks } from './Components/Ticks';
import { XAxis, YAxis } from './Components/Axis';
import { Rule } from './Components/Rule';
import InitialMessage from './Components/InitialMessage';


export default function renderBars({ data, width, height, parameters, loading, onDrillDown, initialLoad }: ChartScriptProps): React.ReactElement<any> {

  var xRule = new Rule({
    _1: 5,
    title: 15,
    _2: 10,
    labels: parameters["Labels"] == "Margin" ? parseInt(parameters["LabelsMargin"]) : 0,
    _3: parameters["Labels"] == "Margin" ? 5 : 0,
    ticks: 4,
    content: '*',
    _4: 5,
  }, width);

  var yRule = new Rule({
    _1: 5,
    content: '*',
    ticks: 4,
    _2: 5,
    labels: 10,
    _3: 10,
    title: 15,
    _4: 5,
  }, height);

  if (data == null || data.rows.length == 0)
    return (
      <svg direction="ltr" width={width} height={height}>
        <InitialMessage data={data} x={xRule.middle("content")} y={yRule.middle("content")} loading={loading} />
        <XAxis xRule={xRule} yRule={yRule} />
        <YAxis xRule={xRule} yRule={yRule} />
      </svg>
    );

  var keyColumn = data.columns.c0!;
  var valueColumn = data.columns.c1! as ChartClient.ChartColumn<number>;


  var x = scaleFor(valueColumn, data.rows.map(r => valueColumn.getValue(r)), 0, xRule.size('content'), parameters['Scale']);

  var keyValues = ChartUtils.completeValues(keyColumn, data.rows.map(r => keyColumn.getValue(r)), parameters['CompleteValues'], ChartUtils.insertPoint(keyColumn, valueColumn));

  var y = d3.scaleBand()
    .domain(keyValues.map(v => keyColumn.getKey(v)))
    .range([0, yRule.size('content')]);

  var orderedRows = data.rows.orderBy(r => keyColumn.getValueKey(r));
  var color = ChartUtils.colorCategory(parameters, orderedRows.map(r => keyColumn.getValueKey(r)));

  var size = xRule.size('content');
  var labelMargin = 10;

  return (
    <svg direction="ltr" width={width} height={height}>

      <XScaleTicks xRule={xRule} yRule={yRule} valueColumn={valueColumn} x={x} />
      <YKeyTicks xRule={xRule} yRule={yRule} keyValues={keyValues} keyColumn={keyColumn} y={y} showLabels={false} />

      {/*PAINT GRAPH*/}
      <g className="shape" transform={translate(xRule.start('content'), yRule.start('content'))}>
        {orderedRows.map(r => <rect key={keyColumn.getValueKey(r)} className="shape sf-transition"
          transform={translate(0, y(keyColumn.getValueKey(r))!) + (initialLoad ? scale(0, 1) : scale(1,1))}
          width={x(valueColumn.getValue(r))}
          height={y.bandwidth()}
          fill={keyColumn.getValueColor(r) || color(keyColumn.getValueKey(r))}
          stroke={y.bandwidth() > 4 ? '#fff' : undefined}
          onClick={e => onDrillDown(r)}
          cursor="pointer">
          <title>
            {keyColumn.getValueNiceName(r) + ': ' + valueColumn.getValueNiceName(r)}
          </title>
        </rect>)}
      </g>

      {y.bandwidth() > 15 &&
        (parameters["Labels"] == "Margin" ?
        <g className="y-label" transform={translate(xRule.end('labels'), yRule.start('content') + y.bandwidth() / 2)}>
            {orderedRows.map(r => <TextEllipsis key={keyColumn.getValueKey(r)}
              transform={translate(0, y(keyColumn.getValueKey(r))!)}
              maxWidth={xRule.size('labels')}
              padding={labelMargin}
              className="y-label sf-transition"
              fill={(keyColumn.getValueColor(r) || color(keyColumn.getValueKey(r)))}
              dominantBaseline="middle"
              textAnchor="end"
              fontWeight="bold"
              onClick={e => onDrillDown(r)}
              cursor="pointer">
              {keyColumn.getValueNiceName(r)}
            </TextEllipsis>)}
          </g> :
          parameters["Labels"] == "Inside" ?
            <g className="y-label" transform={translate(xRule.start('content') + labelMargin, yRule.start('content') + y.bandwidth() / 2)}>
              {orderedRows.map(r => {
                var posx = x(valueColumn.getValue(r));
                return (
                  <TextEllipsis key={keyColumn.getValueKey(r)}
                    transform={translate(posx >= size / 2 ? 0 : posx, y(keyColumn.getValueKey(r))!)}
                    maxWidth={posx >= size / 2 ? posx : size - posx}
                    padding={labelMargin}
                    className="y-label sf-transition"
                    fill={x(valueColumn.getValue(r)) >= size / 2 ? '#fff' : (keyColumn.getValueColor(r) || color(keyColumn.getValueKey(r)))}
                    dominantBaseline="middle"
                    fontWeight="bold"
                    onClick={e => onDrillDown(r)}
                    cursor="pointer">
                    {keyColumn.getValueNiceName(r)}
                  </TextEllipsis>
                );
              })}
            </g> : null
        )}

      {parseFloat(parameters["NumberOpacity"]) > 0 &&
        <g className="numbers-label" transform={translate(xRule.start('content'), yRule.start('content'))}>
          {orderedRows
            .filter(r => x(valueColumn.getValue(r)) > 20)
            .map(r => {
              var posx = x(valueColumn.getValue(r));

              return (<TextEllipsis key={keyColumn.getValueKey(r)}
                transform={translate(x(valueColumn.getValue(r)) / 2, y(keyColumn.getValueKey(r))! + y.bandwidth() / 2)}
                maxWidth={posx >= size / 2 ? posx : size - posx}
                padding={labelMargin}
                className="number-label sf-transition"
                fill={parameters["NumberColor"] || "#000"}
                dominantBaseline="middle"
                opacity={parameters["NumberOpacity"]}
                textAnchor="middle"
                fontWeight="bold"
                onClick={e => onDrillDown(r)}
                cursor="pointer">
                {valueColumn.getValueNiceName(r)}
              </TextEllipsis>);
            })}
        </g>
      }

      <InitialMessage data={data} x={xRule.middle("content")} y={yRule.middle("content")} loading={loading} />
      <XAxis xRule={xRule} yRule={yRule} />
      <YAxis xRule={xRule} yRule={yRule} />
    </svg>
  );
}
