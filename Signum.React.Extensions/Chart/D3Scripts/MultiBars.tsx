import * as React from 'react'
import * as d3 from 'd3'
import * as ChartUtils from '../Templates/ChartUtils';
import { translate, scale, rotate, skewX, skewY, matrix, scaleFor, rule, ellipsis } from '../Templates/ChartUtils';
import { ChartTable, ChartColumn } from '../ChartClient';
import ReactChartBase from './ReactChartBase';
import Legend from './Components/Legend';
import TextEllipsis from './Components/TextEllipsis';
import { XScaleTicks, YKeyTicks } from './Components/Ticks';
import { XAxis, YAxis } from './Components/Axis';


export default class MultiBarsChart extends ReactChartBase {

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
      labels: parseInt(data.parameters["LabelMargin"]),
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
      labels: 10,
      _4: 10,
      title: 15,
      _5: 5,
    }, height);
    //yRule.debugY(chart);

    var allValues = pivot.rows.flatMap(r => pivot.columns.map(function (c) { return r.values[c.key] && r.values[c.key].value; }));

    var x = scaleFor(valueColumn0, allValues, 0, xRule.size('content'), data.parameters["Scale"]);

    var keyValues = ChartUtils.completeValues(keyColumn, pivot.rows.map(r => r.rowValue), data.parameters['CompleteValues'], ChartUtils.insertPoint(keyColumn, valueColumn0));

    var y = d3.scaleBand()
      .domain(keyValues.map(v => keyColumn.getKey(v)))
      .range([0, yRule.size('content')]);

    var xTicks = x.ticks(width / 50);
    var xTickFormat = x.tickFormat(width / 50);

    var interMagin = 2;

    var color = d3.scaleOrdinal(ChartUtils.getColorScheme(data.parameters["ColorCategory"], parseInt(data.parameters["ColorCategorySteps"]))).domain(pivot.columns.map(s => s.key));

    var ySubscale = d3.scaleBand()
      .domain(pivot.columns.map(s => s.key))
      .range([interMagin, y.bandwidth() - interMagin]);

    return (
      <svg direction="ltr" width={width} height={height}>

        <XScaleTicks xRule={xRule} yRule={yRule} valueColumn={valueColumn0} x={x} />
        <YKeyTicks xRule={xRule} yRule={yRule} keyValues={keyValues} keyColumn={keyColumn} y={y} showLabels={true} />

        {pivot.columns.map(s => <g className="shape-serie"
          transform={translate(xRule.start('content'), yRule.start('content'))} >

          {pivot.rows
            .filter(r => r.values[s.key] != undefined)
            .map((r, i) => <rect key={i} className="shape"
              stroke={ySubscale.bandwidth() > 4 ? '#fff' : undefined}
              fill={s.color || color(s.key)}
              y={ySubscale(s.key)!}
              transform={translate(0, y(keyColumn.getKey(r.rowValue))!)}
              height={ySubscale.bandwidth()}
              width={x(r.values[s.key] && r.values[s.key].value)}
              onClick={e => this.props.onDrillDown(r.values[s.key].rowClick)}
              cursor="pointer">
              <title>
                {r.values[s.key].valueTitle}
              </title>
            </rect>)}

          {
            ySubscale.bandwidth() > 15 && parseFloat(data.parameters["NumberOpacity"]) > 0 &&
            pivot.rows
              .filter(r => r.values[s.key] != undefined && x(r.values[s.key] && r.values[s.key].value) > 16)
              .map((r, i) => <text key={i} className="number-label"
                y={ySubscale(s.key)! + ySubscale.bandwidth() / 2}
                x={x(r.values[s.key] && r.values[s.key].value) / 2}
                transform={translate(0, y(keyColumn.getKey(r.rowValue))!)}
                opacity={data.parameters["NumberOpacity"]}
                fill={data.parameters["NumberColor"]}
                dominantBaseline="middle"
                textAnchor="middle"
                fontWeight="bold">
                {r.values[s.key].value}
                <title>
                  {r.values[s.key].valueTitle}
                </title>
              </text>)
          }
        </g>)}

        {
          /*PAINT GRAPH*/
          pivot.columns.map(s => <g className="shape-serie"
            transform={translate(xRule.start('content'), yRule.start('content'))}>
            {pivot.rows
              .filter(r => r.values[s.key] != undefined)
              .map((r, i) => <rect key={i} className="shape"
                stroke={ySubscale.bandwidth() > 4 ? '#fff' : undefined}
                fill={s.color || color(s.key)}
                y={ySubscale(s.key)!}
                transform={translate(0, y(keyColumn.getKey(r.rowValue))!)}
                height={ySubscale.bandwidth()}
                width={x(r.values[s.key] && r.values[s.key].value)}
                onClick={e => this.props.onDrillDown(r.values[s.key].rowClick)}
                cursor="pointer">
                <title>
                  {r.values[s.key].valueTitle}
                </title>
              </rect>)}

            {ySubscale.bandwidth() > 15 && parseFloat(data.parameters["NumberOpacity"]) > 0 &&
              pivot.rows
                .filter(r => r.values[s.key] != undefined && x(r.values[s.key] && r.values[s.key].value) > 16)
                .map((r, i) => <text key={i} className="number-label"
                  y={ySubscale(s.key)! + ySubscale.bandwidth() / 2}
                  x={x(r.values[s.key] && r.values[s.key].value) / 2}
                  transform={translate(0, y(keyColumn.getKey(r.rowValue))!)}
                  opacity={data.parameters["NumberOpacity"]}
                  fill={data.parameters["NumberColor"]}
                  dominantBaseline="middle"
                  textAnchor="middle"
                  fontWeight="bold">
                  {r.values[s.key].value}
                  <title>
                    {r.values[s.key].valueTitle}
                  </title>
                </text>)}
          </g>)
        }

        <Legend pivot={pivot} xRule={xRule} yRule={yRule} color={color} />

        <XAxis xRule={xRule} yRule={yRule} />
        <YAxis xRule={xRule} yRule={yRule} />
      </svg>
    );
  }
}



