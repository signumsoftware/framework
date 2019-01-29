import * as React from 'react'
import * as d3 from 'd3'
import * as ChartUtils from './Components/ChartUtils';
import { translate, scale, rotate, skewX, skewY, matrix, scaleFor } from './Components/ChartUtils';
import { PivotRow, toPivotTable, groupedPivotTable } from './Components/PivotTable';
import { ChartTable, ChartColumn, ChartScriptProps } from '../ChartClient';
import Legend from './Components/Legend';
import TextEllipsis from './Components/TextEllipsis';
import { XScaleTicks, YKeyTicks } from './Components/Ticks';
import { XAxis, YAxis } from './Components/Axis';
import { Rule } from './Components/Rule';
import InitialMessage from './Components/InitialMessage';


export default function renderMultiBars({ data, width, height, parameters, loading, onDrillDown, initialLoad }: ChartScriptProps): React.ReactElement<any> {

  var xRule = new Rule({
    _1: 5,
    title: 15,
    _2: 10,
    labels: parseInt(parameters["LabelMargin"]),
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
    labels: 10,
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

  var allValues = pivot.rows.flatMap(r => pivot.columns.map(function (c) { return r.values[c.key] && r.values[c.key].value; }));

  var x = scaleFor(valueColumn0, allValues, 0, xRule.size('content'), parameters["Scale"]);

  var keyValues = ChartUtils.completeValues(keyColumn, pivot.rows.map(r => r.rowValue), parameters['CompleteValues'], ChartUtils.insertPoint(keyColumn, valueColumn0));

  var y = d3.scaleBand()
    .domain(keyValues.map(v => keyColumn.getKey(v)))
    .range([0, yRule.size('content')]);

  var interMagin = 2;

  var columnsInOrder = pivot.columns.orderBy(a => a.key);
  var rowsInOrder = pivot.rows.orderBy(r => keyColumn.getKey(r.rowValue));
  var color = ChartUtils.colorCategory(parameters, columnsInOrder.map(s => s.key));

  var ySubscale = d3.scaleBand()
    .domain(pivot.columns.map(s => s.key))
    .range([interMagin, y.bandwidth() - interMagin]);

  return (
    <svg direction="ltr" width={width} height={height}>

      <XScaleTicks xRule={xRule} yRule={yRule} valueColumn={valueColumn0} x={x} />
      <YKeyTicks xRule={xRule} yRule={yRule} keyValues={keyValues} keyColumn={keyColumn} y={y} showLabels={true} />

      {columnsInOrder.map(s => <g key={s.key} className="shape-serie"
        transform={translate(xRule.start('content'), yRule.end('content'))} >

        {
          rowsInOrder
            .filter(r => r.values[s.key] != undefined)
            .map(r => <rect key={keyColumn.getKey(r.rowValue)} className="shape sf-transition"
              stroke={ySubscale.bandwidth() > 4 ? '#fff' : undefined}
              fill={s.color || color(s.key)}
              transform={translate(0, -y(keyColumn.getKey(r.rowValue))! - ySubscale(s.key)! - ySubscale.bandwidth()) + (initialLoad ? scale(0, 1) : scale(1, 1))}
              height={ySubscale.bandwidth()}
              width={x(r.values[s.key] && r.values[s.key].value)}
              onClick={e => onDrillDown(r.values[s.key].rowClick)}
              cursor="pointer">
              <title>
                {r.values[s.key].valueTitle}
              </title>
            </rect>)
        }

        {
          ySubscale.bandwidth() > 15 && parseFloat(parameters["NumberOpacity"]) > 0 &&
          rowsInOrder
            .filter(r => r.values[s.key] != undefined && x(r.values[s.key] && r.values[s.key].value) > 16)
            .map(r => <text key={keyColumn.getKey(r.rowValue)} className="number-label sf-transition"
              transform={translate(
                x(r.values[s.key] && r.values[s.key].value) / 2,
                -y(keyColumn.getKey(r.rowValue))! - ySubscale(s.key)! - ySubscale.bandwidth() / 2
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
      </g>)}

      <Legend pivot={pivot} xRule={xRule} yRule={yRule} color={color} />

      <InitialMessage data={data} x={xRule.middle("content")} y={yRule.middle("content")} loading={loading} />
      <XAxis xRule={xRule} yRule={yRule} />
      <YAxis xRule={xRule} yRule={yRule} />
    </svg>
  );
}
