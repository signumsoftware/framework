import * as React from 'react'
import * as d3 from 'd3'
import * as ChartClient from '../ChartClient';
import * as ChartUtils from './Components/ChartUtils';
import { translate, scale, rotate, skewX, skewY, matrix, scaleFor } from './Components/ChartUtils';
import { PivotRow, toPivotTable, groupedPivotTable } from './Components/PivotTable';
import { ChartTable, ChartColumn } from '../ChartClient';
import { XScaleTicks, YKeyTicks } from './Components/Ticks';
import Legend from './Components/Legend';
import { XAxis, YAxis } from './Components/Axis';
import TextEllipsis from './Components/TextEllipsis';
import { Rule } from './Components/Rule';
import InitialMessage from './Components/InitialMessage';


export default function renderStackedBars({ data, width, height, parameters, loading, onDrillDown, initialLoad }: ChartClient.ChartScriptProps): React.ReactElement<any> {

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


  var keyValues = ChartUtils.completeValues(keyColumn, pivot.rows.map(r => r.rowValue), parameters['CompleteValues'], ChartUtils.insertPoint(keyColumn, valueColumn0));

  var y = d3.scaleBand()
    .domain(keyValues.map(v => keyColumn.getKey(v)))
    .range([0, yRule.size('content')]);

  var pStack = parameters["Stack"];

  var stack = d3.stack<PivotRow>()
    .offset(ChartUtils.getStackOffset(pStack)!)
    .order(ChartUtils.getStackOrder(parameters["Order"])!)
    .keys(pivot.columns.map(d => d.key))
    .value((r, k) => {
      var v = r.values[k];
      return v && v.value || 0;
    });

  var stackedSeries = stack(pivot.rows);

  var max = d3.max(stackedSeries, s => d3.max(s, v => v[1]))!;
  var min = d3.min(stackedSeries, s => d3.min(s, v => v[0]))!;

  var x = d3.scaleLinear()
    .domain([min, max])
    .range([0, xRule.size('content')]);

  var rowsInOrder = pivot.rows.orderBy(r => keyColumn.getKey(r.rowValue));
  var color = ChartUtils.colorCategory(parameters, pivot.columns.map(s => s.key));

  var format = pStack == "expand" ? d3.format(".0%") :
    pStack == "zero" ? d3.format("") :
      (n: number) => d3.format("")(n) + "?";

  var labelMargin = 5;

  var size = xRule.size('content');

  return (
    <svg direction="ltr" width={width} height={height}>
      <XScaleTicks xRule={xRule} yRule={yRule} valueColumn={valueColumn0} x={x} format={format} />
      <YKeyTicks xRule={xRule} yRule={yRule} keyValues={keyValues} keyColumn={keyColumn} y={y} showLabels={false} />


      {stackedSeries.orderBy(s => s.key).map(s => <g key={s.key} className="shape-serie"
        transform={translate(xRule.start('content'), yRule.start('content'))}>

        {s.filter(r => r.data.values[s.key] != undefined)
          .orderBy(r => keyColumn.getKey(r.data.rowValue))
          .map(r => <rect key={keyColumn.getKey(r.data.rowValue)} className="shape sf-transition"
            transform={translate(x(r[0]), y(keyColumn.getKey(r.data.rowValue))!) + (initialLoad ? scale(0, 1) : scale(1, 1))}
            stroke={y.bandwidth() > 4 ? '#fff' : undefined}
            fill={color(s.key)}
            height={y.bandwidth()}
            width={x(r[1]) - x(r[0])}
            onClick={e => onDrillDown(r.data.values[s.key].rowClick)}
            cursor="pointer">
            <title>
              {r.data.values[s.key].valueTitle}
            </title>
          </rect>)}

        {y.bandwidth() > 15 && parseFloat(parameters["NumberOpacity"]) > 0 &&
          s.orderBy(r => keyColumn.getKey(r.data.rowValue))
            .filter(r => (x(r[1]) - x(r[0])) > 20)
            .map(r => <text key={keyColumn.getKey(r.data.rowValue)} className="number-label sf-transition"
              transform={translate(
                x(r[0]) * 0.5 + x(r[1]) * 0.5,
                y(keyColumn.getKey(r.data.rowValue))! + y.bandwidth() / 2
              )}
              fill={parameters["NumberColor"]}
              dominantBaseline="middle"
              opacity={parameters["NumberOpacity"]}
              textAnchor="middle"
              fontWeight="bold">
              {r.data.values[s.key].valueNiceName}
              <title>
                {r.data.values[s.key].valueTitle}
              </title>
            </text>)
        }
      </g>)}

      {y.bandwidth() > 15 && pivot.columns.length > 0 && (
        parameters["Labels"] == "Margin" ?
          <g className="y-label" transform={translate(xRule.end('labels'), yRule.start('content') + y.bandwidth() / 2)}>
            {rowsInOrder.map(r => <TextEllipsis key={keyColumn.getKey(r.rowValue)}
              maxWidth={xRule.size('labels')}
              padding={labelMargin}
              className="y-label sf-transition"
              y={y(keyColumn.getKey(r.rowValue))!}
              dominantBaseline="middle"
              textAnchor="end">
              {keyColumn.getNiceName(r.rowValue)}
            </TextEllipsis>)}
          </g> :
          parameters["Labels"] == "Inside" ?
            <g className="y-axis-tick-label" transform={translate(xRule.start('content'), yRule.start('content') + y.bandwidth() / 2)}>
              {pivot.rows.map((r, i) => {
                var maxValue = stackedSeries[stackedSeries.length - 1][i][1];
                var posx = x(maxValue);
                return (<TextEllipsis key={keyColumn.getKey(r.rowValue)}
                  transform={translate(posx >= size / 2 ? 0 : posx, y(keyColumn.getKey(r.rowValue))!)}
                  maxWidth={posx >= size / 2 ? posx : size - posx}
                  padding={labelMargin}
                  className="y-axis-tick-label sf-chart-strong sf-transition"
                  dx={labelMargin}
                  textAnchor="start"
                  fill={posx >= size / 2 ? '#fff' : '#000'}
                  dominantBaseline="middle"
                  fontWeight="bold">
                  {keyColumn.getNiceName(r.rowValue)}
                </TextEllipsis>);
              })}
            </g> : null
      )}

      <Legend pivot={pivot} xRule={xRule} yRule={yRule} color={color} />

      <InitialMessage data={data} x={xRule.middle("content")} y={yRule.middle("content")} loading={loading} />
      <XAxis xRule={xRule} yRule={yRule} />
      <YAxis xRule={xRule} yRule={yRule} />
    </svg>
  );
}
