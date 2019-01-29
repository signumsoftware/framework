import * as React from 'react'
import * as d3 from 'd3'
import * as ChartClient from '../ChartClient';
import * as ChartUtils from './Components/ChartUtils';
import { translate, scale, rotate, skewX, skewY, matrix, scaleFor } from './Components/ChartUtils';
import { PivotRow, toPivotTable, groupedPivotTable } from './Components/PivotTable';
import { ChartTable, ChartColumn } from '../ChartClient';
import { XKeyTicks, YScaleTicks } from './Components/Ticks';
import Legend from './Components/Legend';
import { XAxis, YAxis } from './Components/Axis';
import { Rule } from './Components/Rule';
import InitialMessage from './Components/InitialMessage';


export default function renderMultiColumns({ data, width, height, parameters, loading, onDrillDown, initialLoad }: ChartClient.ChartScriptProps): React.ReactElement<any> {

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
    _2: 5,
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

  var keyValues = ChartUtils.completeValues(keyColumn, pivot.rows.map(v => v.rowValue), parameters['CompleteValues'], ChartUtils.insertPoint(keyColumn, valueColumn0));

  var x = d3.scaleBand()
    .domain(keyValues.map(v => keyColumn.getKey(v)))
    .range([0, xRule.size('content')]);

  var allValues = pivot.rows.flatMap(r => pivot.columns.map(function (c) { return r.values[c.key] && r.values[c.key].value; }));

  var y = scaleFor(valueColumn0, allValues, 0, yRule.size('content'), parameters["Scale"]);

  var interMagin = 2;

  var xSubscale = d3.scaleBand()
    .domain(pivot.columns.map(s => s.key))
    .range([interMagin, x.bandwidth() - interMagin]);

  var columnsInOrder = pivot.columns.orderBy(a => a.key);
  var rowsInOrder = pivot.rows.orderBy(r => keyColumn.getKey(r.rowValue));
  var color = ChartUtils.colorCategory(parameters, columnsInOrder.map(s => s.key));


  return (
    <svg direction="ltr" width={width} height={height}>
      <XKeyTicks xRule={xRule} yRule={yRule} keyValues={keyValues} keyColumn={keyColumn} x={x} />
      <YScaleTicks xRule={xRule} yRule={yRule} valueColumn={valueColumn0} y={y} />

      {columnsInOrder.map(s => <g key={s.key} className="shape-serie"
        transform={translate(xRule.start('content'), yRule.end('content'))} >

        {rowsInOrder
          .filter(r => r.values[s.key] != undefined)
          .map(r => <rect key={keyColumn.getKey(r.rowValue)}
            className="shape sf-transition"
            stroke={xSubscale.bandwidth() > 4 ? '#fff' : undefined}
            fill={s.color || color(s.key)}
            transform={(initialLoad ? scale(1, 0) : scale(1, 1)) + translate(
              x(keyColumn.getKey(r.rowValue))! + xSubscale(s.key)!,
              - y(r.values[s.key] && r.values[s.key].value)
            )}
            width={xSubscale.bandwidth()}
            height={y(r.values[s.key] && r.values[s.key].value)}
            onClick={e => onDrillDown(r.values[s.key].rowClick)}
            cursor="pointer">
            <title>
              {r.values[s.key].valueTitle}
            </title>
          </rect>)}

        {x.bandwidth() > 15 && parseFloat(parameters["NumberOpacity"]) > 0 &&
          rowsInOrder
            .filter(r => r.values[s.key] != undefined && y(r.values[s.key] && r.values[s.key].value) > 10)
            .map(r => <text key={keyColumn.getKey(r.rowValue)}
              className="number-label sf-transition"
              transform={translate(
                x(keyColumn.getKey(r.rowValue))! + xSubscale.bandwidth() / 2 + xSubscale(s.key)!,
                - y(r.values[s.key] && r.values[s.key].value) / 2
              )}
              opacity={parameters["NumberOpacity"]}
              fill={parameters["NumberColor"]}
              dominantBaseline="middle"
              textAnchor="middle"
              fontWeight="bold">
              {r.values[s.key].valueNiceName}
              <title>
                {r.values[s.key].valueTitle}
              </title>
            </text>)
        }
      </g>
      )}

      <Legend pivot={pivot} xRule={xRule} yRule={yRule} color={color} />
      
      <InitialMessage data={data} x={xRule.middle("content")} y={yRule.middle("content")} loading={loading} />
      <XAxis xRule={xRule} yRule={yRule} />
      <YAxis xRule={xRule} yRule={yRule} />
    </svg>
  );
}
