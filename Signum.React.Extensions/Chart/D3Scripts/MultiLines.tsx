import * as React from 'react'
import * as d3 from 'd3'
import * as ChartClient from '../ChartClient';
import * as ChartUtils from './Components/ChartUtils';
import { translate, scale, rotate, skewX, skewY, matrix, scaleFor } from './Components/ChartUtils';
import { PivotRow, toPivotTable, groupedPivotTable } from './Components/PivotTable';
import { ChartTable, ChartColumn, ChartScriptProps } from '../ChartClient';
import { XKeyTicks, YScaleTicks } from './Components/Ticks';
import Legend from './Components/Legend';
import { XAxis, YAxis } from './Components/Axis';
import { Rule } from './Components/Rule';
import InitialMessage from './Components/InitialMessage';


export default function renderMultiLines({ data, width, height, parameters, loading, onDrillDown, initialLoad }: ChartScriptProps): React.ReactElement<any> {
  
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
    _1: 5,
    legend: 15,
    _2: 20,
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


  var c = data.columns;
  var keyColumn = c.c0 as ChartColumn<unknown>;
  var valueColumn0 = c.c2 as ChartColumn<number>;

  var pivot = c.c1 == null ?
    toPivotTable(data, c.c0!, [c.c2, c.c3, c.c4, c.c5, c.c6].filter(cn => cn != undefined) as ChartColumn<number>[]) :
    groupedPivotTable(data, c.c0!, c.c1, c.c2 as ChartColumn<number>);


  var keyValues = ChartUtils.completeValues(keyColumn, pivot.rows.map(r => r.rowValue), parameters['CompleteValues'], ChartUtils.insertPoint(keyColumn, valueColumn0));

  var x = d3.scaleBand()
    .domain(keyValues.map(v => keyColumn.getKey(v)))
    .range([0, xRule.size('content')]);

  var allValues = pivot.rows.flatMap(r => pivot.columns.map(function (c) { return r.values[c.key] && r.values[c.key].value; }));

  var y = scaleFor(valueColumn0, allValues, 0, yRule.size('content'), parameters["Scale"]);

  var columnsInOrder = pivot.columns.orderBy(a => a.key);
  var rowsInOrder = pivot.rows.orderBy(r => keyColumn.getKey(r.rowValue));
  var color = ChartUtils.colorCategory(parameters, columnsInOrder.map(s => s.key));

  var pInterpolate = parameters["Interpolate"];

  return (
    <svg direction="ltr" width={width} height={height}>
      <XKeyTicks xRule={xRule} yRule={yRule} keyValues={keyValues} keyColumn={keyColumn} x={x} />
      <YScaleTicks xRule={xRule} yRule={yRule} valueColumn={valueColumn0} y={y} />


      {columnsInOrder.map(s =>
        <g key={s.key} className="shape-serie sf-transition"
          transform={translate(xRule.start('content') + x.bandwidth() / 2, yRule.end('content'))} >
          <path className="shape sf-transition"
            stroke={s.color || color(s.key)}
            transform={initialLoad ? scale(1, 0) : scale(1, 1)}
            fill="none"
            strokeWidth={3}
            shapeRendering="initial"
            d={d3.line<PivotRow>()
              .x(r => x(keyColumn.getKey(r.rowValue))!)
              .y(r => -y(r.values[s.key] && r.values[s.key].value))
              .defined(r => r.values[s.key] && r.values[s.key].value != null)
              .curve(ChartUtils.getCurveByName(pInterpolate)!)
              (pivot.rows)!}
          />
          {/*paint graph - hover area trigger*/}
          {rowsInOrder
            .filter(r => r.values[s.key] != undefined)
            .map(r => <circle key={keyColumn.getKey(r.rowValue)} className="hover"
              transform={translate(x(keyColumn.getKey(r.rowValue))!, -y(r.values[s.key] && r.values[s.key].value))}
              r={15}
              fill="#fff"
              fillOpacity={0}
              stroke="none"
              onClick={e => onDrillDown(r.values[s.key].rowClick)}
              cursor="pointer">
              <title>
                {r.values[s.key].valueNiceName}
              </title>
            </circle>)}
          {rowsInOrder
            .filter(r => r.values[s.key] != undefined)
            .map(r => <circle key={keyColumn.getKey(r.rowValue)} className="point sf-transition"
              stroke={s.color || color(s.key)}
              strokeWidth={2}
              fill="white"
              transform={(initialLoad ? scale(1, 0) : scale(1, 1)) + translate(x(keyColumn.getKey(r.rowValue))!, -y(r.values[s.key] && r.values[s.key].value))}
              r={5}
              shapeRendering="initial"
              onClick={e => onDrillDown(r.values[s.key].rowClick)}
              cursor="pointer">
              <title>
                {r.values[s.key].valueTitle}
              </title>
            </circle>)}
          {parseFloat(parameters["NumberOpacity"]) > 0 &&
            rowsInOrder
              .filter(r => r.values[s.key] != undefined)
              .map(r => <text key={keyColumn.getKey(r.rowValue)} className="point-label sf-transition"
                textAnchor="middle"
                opacity={parameters["NumberOpacity"]}
                transform={translate(x(keyColumn.getKey(r.rowValue))!, -y(r.values[s.key] && r.values[s.key].value) - 8)}
                onClick={e => onDrillDown(r.values[s.key].rowClick)}
                cursor="pointer"
                shapeRendering="initial">
                {r.values[s.key].valueNiceName}
              </text>)
          }
        </g>)}

      <Legend pivot={pivot} xRule={xRule} yRule={yRule} color={color} />

      <InitialMessage data={data} x={xRule.middle("content")} y={yRule.middle("content")} loading={loading} />
      <XAxis xRule={xRule} yRule={yRule} />
      <YAxis xRule={xRule} yRule={yRule} />
    </svg>
  );
}
