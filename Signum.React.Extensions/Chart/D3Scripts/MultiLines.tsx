import * as React from 'react'
import * as d3 from 'd3'
import * as ChartClient from '../ChartClient';
import * as ChartUtils from '../Templates/ChartUtils';
import { translate, scale, rotate, skewX, skewY, matrix, scaleFor, rule, ellipsis, PivotRow, PivotColumn } from '../Templates/ChartUtils';
import { ChartTable, ChartColumn } from '../ChartClient';
import { XKeyTicks, YScaleTicks } from './Components/Ticks';
import Legend from './Components/Legend';
import { XAxis, YAxis } from './Components/Axis';
import ReactChartBase from './ReactChartBase';


export default class MultiLineChart extends ReactChartBase {

  renderChart(data: ChartTable, width: number, height: number): React.ReactElement<any> {

    var c = data.columns;
    var keyColumn = c.c0 as ChartColumn<unknown>;
    var valueColumn0 = c.c2 as ChartColumn<number>;

    var pivot = c.c1 == null ?
      ChartUtils.toPivotTable(data, c.c0!, [c.c2, c.c3, c.c4, c.c5, c.c6].filter(cn => cn != undefined) as ChartColumn<number>[]) :
      ChartUtils.groupedPivotTable(data, c.c0!, c.c1, c.c2 as ChartColumn<number>);

    var xRule = rule({
      _1: 5,
      title: 15,
      _2: 10,
      labels: parseInt(data.parameters["UnitMargin"]),
      _3: 5,
      ticks: 4,
      content: '*',
      _4: 10,
    }, width);
    //xRule.debugX(chart)

    var yRule = rule({
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

    var keyValues = ChartUtils.completeValues(keyColumn, pivot.rows.map(r => r.rowValue), data.parameters['CompleteValues'], ChartUtils.insertPoint(keyColumn, valueColumn0));

    var x = d3.scaleBand()
      .domain(keyValues.map(v => keyColumn.getKey(v)))
      .range([0, xRule.size('content')]);

    var allValues = pivot.rows.flatMap(r => pivot.columns.map(function (c) { return r.values[c.key] && r.values[c.key].value; }));

    var y = scaleFor(valueColumn0, allValues, 0, yRule.size('content'), data.parameters["Scale"]);

    var color = d3.scaleOrdinal(ChartUtils.getColorScheme(data.parameters["ColorCategory"], parseInt(data.parameters["ColorCategorySteps"]))).domain(pivot.columns.map(s => s.key));

    var pInterpolate = data.parameters["Interpolate"];

    return (
      <svg direction="ltr" width={width} height={height}>
        <XKeyTicks xRule={xRule} yRule={yRule} keyValues={keyValues} keyColumn={keyColumn} x={x} />
        <YScaleTicks xRule={xRule} yRule={yRule} valueColumn={valueColumn0} y={y} />


        {pivot.columns.map(s =>
          <g key={s.key} className="shape-serie"
            transform={translate(xRule.start('content') + x.bandwidth() / 2, yRule.end('content'))} >
            <path className="shape"
              stroke={s.color || color(s.key)}
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
            {pivot.rows
              .filter(r => r.values[s.key] != undefined)
              .map(r => <circle key={keyColumn.getKey(r.rowValue)} className="hover"
                cx={x(keyColumn.getKey(r.rowValue))!}
                cy={-y(r.values[s.key] && r.values[s.key].value)}
                r={15}
                fill="#fff"
                fillOpacity={0}
                stroke="none"
                onClick={e => this.props.onDrillDown(r.values[s.key].rowClick)}
                cursor="pointer">
                <title>
                  {r.values[s.key].value}
                </title>
              </circle>)}
            {pivot.rows
              .filter(r => r.values[s.key] != undefined)
              .map(r => <circle key={keyColumn.getKey(r.rowValue)} className="point"
                fill={s.color || color(s.key)}
                r={5}
                cx={x(keyColumn.getKey(r.rowValue))!}
                cy={-y(r.values[s.key] && r.values[s.key].value)}
                shapeRendering="initial"
                onClick={e => this.props.onDrillDown(r.values[s.key].rowClick)}
                cursor="pointer">
                <title>
                  {r.values[s.key].valueTitle}
                </title>
              </circle>)}
            {parseFloat(data.parameters["NumberOpacity"]) > 0 &&
              pivot.rows
                .filter(r => r.values[s.key] != undefined)
                .map(r => <text key={keyColumn.getKey(r.rowValue)} className="point-label"
                  textAnchor="middle"
                  opacity={data.parameters["NumberOpacity"]}
                  x={x(keyColumn.getKey(r.rowValue))!}
                  y={-y(r.values[s.key] && r.values[s.key].value) - 8}
                  onClick={e => this.props.onDrillDown(r.values[s.key].rowClick)}
                  cursor="pointer"
                  shapeRendering="initial">
                  {r.values[s.key].value}
                </text>)
            }
          </g>)}

        <Legend pivot={pivot} xRule={xRule} yRule={yRule} color={color} />

        <XAxis xRule={xRule} yRule={yRule} />
        <YAxis xRule={xRule} yRule={yRule} />
      </svg>
    );
  }
}
