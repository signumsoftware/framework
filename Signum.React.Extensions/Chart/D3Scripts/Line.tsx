import * as React from 'react'
import * as d3 from 'd3'
import * as ChartUtils from './Components/ChartUtils';
import { translate, scale, rotate, skewX, skewY, matrix, scaleFor } from './Components/ChartUtils';
import { ChartTable, ChartColumn, ChartRow, ChartScriptProps } from '../ChartClient';
import { KeyCodes } from '@framework/Components';
import TextEllipsis from './Components/TextEllipsis';
import { XKeyTicks, YScaleTicks } from './Components/Ticks';
import { XAxis, YAxis } from './Components/Axis';
import { Rule } from './Components/Rule';
import InitialMessage from './Components/InitialMessage';

export default function renderLine({ data, width, height, parameters, loading, onDrillDown, initialLoad }: ChartScriptProps): React.ReactElement<any> {
  
  var xRule = new Rule({
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

  var yRule = new Rule({
    _2: parseFloat(parameters["NumberOpacity"]) > 0 ? 20 : 5,
    content: '*',
    ticks: 4,
    _3: 5,
    labels0: 15,
    labels1: 15,
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

  var keyColumn = data.columns.c0! as ChartColumn<unknown>;
  var valueColumn = data.columns.c1! as ChartColumn<number>;


  var orderedRows = data.rows.orderBy(r => keyColumn.getValueKey(r));
  var keyValues = ChartUtils.completeValues(keyColumn, data.rows.map(r => keyColumn.getValue(r)), parameters['CompleteValues'], ChartUtils.insertPoint(keyColumn, valueColumn));

  var x = d3.scaleBand()
    .domain(keyValues.map(v => keyColumn.getKey(v)))
    .range([0, xRule.size('content')]);

  var y = scaleFor(valueColumn, data.rows.map(r => valueColumn.getValue(r)), 0, yRule.size('content'), parameters["Scale"]);

  var rowByKey = data.rows.toObject(r => keyColumn.getValueKey(r));

  var line = d3.line<unknown>()
    .defined(key => rowByKey[keyColumn.getKey(key)] != null)
    .x(key => x(keyColumn.getKey(key))!)
    .y(key => rowByKey[keyColumn.getKey(key)] && -y(valueColumn.getValue(rowByKey[keyColumn.getKey(key)])))
    .curve(ChartUtils.getCurveByName(parameters["Interpolate"]!)!);//"linear"

  var color = parameters["Color"]!;// 'steelblue'

  return (
    <svg direction="ltr" width={width} height={height}>

      <XKeyTicks xRule={xRule} yRule={yRule} keyValues={keyValues} keyColumn={keyColumn} x={x} showLines={true} />
      <YScaleTicks xRule={xRule} yRule={yRule} valueColumn={valueColumn} y={y} />

      {/*PAINT CHART'*/}
      <g className="shape" transform={translate(xRule.start('content') + (x.bandwidth() / 2), yRule.end('content'))}>
        <path className="shape sf-transition" stroke={color} fill="none" strokeWidth={3} shapeRendering="initial" d={line(keyValues)!} transform={initialLoad ? scale(1, 0) : scale(1, 1)} />
      </g>

      {/*paint graph - hover area trigger*/}
      <g className="hover-trigger" transform={translate(xRule.start('content') + (x.bandwidth() / 2), yRule.end('content'))}>
        {orderedRows
          .map(r => <circle key={keyColumn.getValueKey(r)}
            transform={translate(x(keyColumn.getValueKey(r))!, -y(valueColumn.getValue(r)))}
            className="hover-trigger"
            fill="#fff"
            fillOpacity={0}
            stroke="none"
            cursor="pointer"
            r={15}
            onClick={e => onDrillDown(r)}>
            <title>
              {keyColumn.getValueNiceName(r) + ': ' + valueColumn.getValueNiceName(r)}
            </title>
          </circle>)}
      </g>

      {/*paint graph - points*/}
      <g className="point sf-transition" transform={translate(xRule.start('content') + (x.bandwidth() / 2), yRule.end('content')) + (initialLoad ? scale(1, 0) : scale(1, 1))}>
        {orderedRows
          .map(r => <circle key={keyColumn.getValueKey(r)}
            transform={translate(x(keyColumn.getValueKey(r))!, -y(valueColumn.getValue(r)))}
            className="point sf-transition"
            stroke={color}
            strokeWidth={2}
            fill="white"
            r={5}
            onClick={e => onDrillDown(rowByKey[keyColumn.getValueKey(r)])}
            cursor="pointer"
            shapeRendering="initial">
            <title>
              {keyColumn.getValueNiceName(r) + ': ' + valueColumn.getValueNiceName(r)}
            </title>
          </circle>)}
      </g>

      { /*Point labels*/
        parseFloat(parameters["NumberOpacity"]!) > 0 &&
        <g className="point-label" transform={translate(xRule.start('content') + (x.bandwidth() / 2), yRule.end('content'))}>
          {orderedRows
            .map(r => <text key={keyColumn.getValueKey(r)} transform={translate(x(keyColumn.getValueKey(r))!, -y(valueColumn.getValue(r)) - 10)}
              className="point-label sf-transition"
              r={5}
              opacity={parseFloat(parameters["NumberOpacity"]!)}
              textAnchor="middle"
              onClick={e => onDrillDown(r)}
              cursor="pointer"
              shapeRendering="initial">
              {valueColumn.getValueNiceName(r)}
            </text>)}
        </g>
      }

      <InitialMessage data={data} x={xRule.middle("content")} y={yRule.middle("content")} loading={loading} />
      <XAxis xRule={xRule} yRule={yRule} />
      <YAxis xRule={xRule} yRule={yRule} />
    </svg>
  );
}
