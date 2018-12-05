import * as React from 'react'
import * as d3 from 'd3'
import D3ChartBase from './D3ChartBase';
import * as ChartClient from '../ChartClient';
import * as ChartUtils from '../Templates/ChartUtils';
import { translate, scale, rotate, skewX, skewY, matrix, scaleFor, rule, ellipsis } from '../Templates/ChartUtils';
import ReactChartBase from './ReactChartBase';
import { ChartTable, ChartColumn } from '../ChartClient';
import { XKeyTicks, YScaleTicks } from './Components/Ticks';
import Legend from './Components/Legend';
import { XAxis, YAxis } from './Components/Axis';


export default class MultiColumnsChart extends ReactChartBase {

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

    var keyValues = ChartUtils.completeValues(keyColumn, pivot.rows.map(v => v.rowValue), data.parameters['CompleteValues'], ChartUtils.insertPoint(keyColumn, valueColumn0));

    var x = d3.scaleBand()
      .domain(keyValues.map(v => keyColumn.getKey(v)))
      .range([0, xRule.size('content')]);

    var allValues = pivot.rows.flatMap(r => pivot.columns.map(function (c) { return r.values[c.key] && r.values[c.key].value; }));

    var y = scaleFor(valueColumn0, allValues, 0, yRule.size('content'), data.parameters["Scale"]);

    var interMagin = 2;

    var xSubscale = d3.scaleBand()
      .domain(pivot.columns.map(s => s.key))
      .range([interMagin, x.bandwidth() - interMagin]);

    var color = d3.scaleOrdinal(ChartUtils.getColorScheme(data.parameters["ColorCategory"], parseInt(data.parameters["ColorCategorySteps"]))).domain(pivot.columns.map(s => s.key));


    return (
      <svg direction="rtl" width={width} height={height}>
        <XKeyTicks xRule={xRule} yRule={yRule} keyValues={keyValues} keyColumn={keyColumn} x={x} />
        <YScaleTicks xRule={xRule} yRule={yRule} valueColumn={valueColumn0} y={y} />

        {pivot.columns.map(s => <g key={s.key} className="shape-serie"
          transform={translate(xRule.start('content'), yRule.start('content'))} >

          {pivot.rows
            .filter(r => r.values[s.key] != undefined)
            .map(r => <rect key={keyColumn.getKey(r.rowValue)} className="shape"
              stroke={xSubscale.bandwidth() > 4 ? '#fff' : undefined}
              fill={s.color || color(s.key)}
              x={x(keyColumn.getKey(r.rowValue))!}
              transform={translate(xSubscale(s.key)!, 0)}
              width={xSubscale.bandwidth()}
              y={yRule.size('content') - y(r.values[s.key] && r.values[s.key].value)}
              height={y(r.values[s.key] && r.values[s.key].value)}
              onClick={e => this.props.onDrillDown(r.values[s.key].rowClick)}
              cursor="pointer">
              <title>
                {r.values[s.key].valueTitle}
              </title>
            </rect>)}

          {x.bandwidth() > 15 && parseFloat(data.parameters["NumberOpacity"]) > 0 &&
            pivot.rows
              .filter(r => r.values[s.key] != undefined && y(r.values[s.key] && r.values[s.key].value) > 10)
              .map(r => <text key={keyColumn.getKey(r.rowValue)}
                className="number-label"
                x={x(keyColumn.getKey(r.rowValue))! + xSubscale.bandwidth() / 2}
                y={yRule.size('content') - y(r.values[s.key] && r.values[s.key].value) / 2}
                transform={translate(xSubscale(s.key)!, 0)}
                opacity={data.parameters["NumberOpacity"]}
                fill={data.parameters["NumberColor"]}
                dominantBaseline="central"
                textAnchor="middle"
                fontWeight="bold">
                {r.values[s.key].value}
                <title>
                  {r.values[s.key].valueTitle}
                </title>
              </text>)
          }
        </g>
        )}

        <Legend pivot={pivot} xRule={xRule} yRule={yRule} color={color} />

        <XAxis xRule={xRule} yRule={yRule} />
        <YAxis xRule={xRule} yRule={yRule} />
      </svg>
    );
  }
}
