import * as React from 'react'
import * as d3 from 'd3'
import * as ChartUtils from '../Templates/ChartUtils';
import { translate, scale, rotate, skewX, skewY, matrix, scaleFor, rule, ellipsis } from '../Templates/ChartUtils';
import { ChartTable, ChartColumn } from '../ChartClient';
import ReactChartBase from './ReactChartBase';
import { TextEllipsis } from './Line';


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
      <svg direction="rtl" width={width} height={height}>
        <g className="x-lines" transform={translate(xRule.start('content'), yRule.start('content'))}>
          {xTicks.map(t => <line className="y-lines"
            x1={x(t)}
            x2={x(t)}
            y1={yRule.size('content')}
            stroke="LightGray" />)}
        </g>

        <g className="x-tick" transform={translate(xRule.start('content'), yRule.start('ticks'))}>
          {xTicks.map(t => <line className="x-tick"
            x1={x(t)}
            x2={x(t)}
            y2={yRule.size('ticks')}
            stroke="Black" />)}
        </g>
        
        <g className="x-label" transform={translate(xRule.start('content'), yRule.end('labels'))}>
          {xTicks.map(t => <text className="x-label"
            x={x(t)}
            textAnchor="middle">
            {xTickFormat}
          </text>)}
        </g>
        
        <g className="x-title" transform={translate(xRule.middle('content'), yRule.middle('title'))}>
          <text className="x-title" textAnchor="middle" dominantBaseline="middle">
            {pivot.title}
          </text>
        </g>
        
        <g className="y-tick" transform={translate(xRule.start('ticks'), yRule.start('content') + (y.bandwidth() / 2))}>
          {pivot.rows.map(v => <line className="y-tick"
            x2={xRule.size('ticks')}
            y1={y(keyColumn.getKey(v.rowValue))!}
            y2={y(keyColumn.getKey(v.rowValue))!}
            stroke="Black" />)}
        </g>

        {y.bandwidth() > 15 && pivot.columns.length > 0 &&
          <g className="y-label" transform={translate(xRule.end('labels'), yRule.start('content') + (y.bandwidth() / 2))}>
            {pivot.rows.map(v => <TextEllipsis maxWidth={xRule.size('labels')} className="y-label"
              y={y(keyColumn.getKey(v.rowValue))!}
              dominantBaseline="middle"
              textAnchor="end">
              {keyColumn.getNiceName(v.rowValue)}
            </TextEllipsis>)}
          </g>
        }


        <g className="y-label" transform={translate(xRule.middle('title'), yRule.middle('content')) + rotate(270)}>
          <text className="y-label" textAnchor="middle" dominantBaseline="middle">
            {keyColumn.title}
          </text>
        </g>


        {pivot.columns.map(s => <g className="shape-serie"
          transform={translate(xRule.start('content'), yRule.start('content'))} >

          {pivot.rows
            .filter(r => r.values[s.key] != undefined)
            .map(r => <rect className="shape"
              stroke={ySubscale.bandwidth() > 4 ? '#fff' : undefined}
              fill={s.color || color(s.key)}
              y={ySubscale(s.key)!}
              transform="translate(0, ' + y(keyColumn.getKey(r.rowValue))! + ')"
              height={ySubscale.bandwidth()}
              width={x(r.values[s.key] && r.values[s.key].value)}
              onClick={e => me.props.onDrillDown(r.values[s.key].rowClick)}
              cursor="pointer">
              <title>
                {r.values[s.key].valueTitle}
              </title>
            </rect>)}

          {
            ySubscale.bandwidth() > 15 && parseFloat(data.parameters["NumberOpacity"]) > 0 &&
            pivot.rows
              .filter(r => r.values[s.key] != undefined && x(r.values[s.key] && r.values[s.key].value) > 16)
              .map(r => <text className="number-label"
                y={ySubscale(s.key)! + ySubscale.bandwidth() / 2}
                x={x(r.values[s.key] && r.values[s.key].value) / 2}
                transform="translate(0, ' + y(keyColumn.getKey(r.rowValue)) + ')"
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
        </g>)}
      
      </svg>
    );
    

    

    //chart.append('svg:g').attr('class', 'y-label').attr('transform', translate(xRule.middle('title'), yRule.middle('content')) + rotate(270))
    //  .append('svg:text').attr('class', 'y-label')
    //  .attr('text-anchor', 'middle')
    //  .attr('dominant-baseline', 'middle')
    //  .text(keyColumn.title);

    
    var me = this;

    //PAINT GRAPH
    chart.enterData(pivot.columns, 'g', 'shape-serie').attr('transform', translate(xRule.start('content'), yRule.start('content')))
      .each(function (s) {

        d3.select(this).enterData(pivot.rows, 'rect', 'shape')
          .filter(r => r.values[s.key] != undefined)
          .attr('stroke', ySubscale.bandwidth() > 4 ? '#fff' : null)
          .attr('fill', r => s.color || color(s.key))
          .attr('y', r => ySubscale(s.key)!)
          .attr('transform', r => 'translate(0, ' + y(keyColumn.getKey(r.rowValue))! + ')')
          .attr('height', ySubscale.bandwidth())
          .attr('width', r => x(r.values[s.key] && r.values[s.key].value))
          .on('click', v => me.props.onDrillDown(v.values[s.key].rowClick))
          .style("cursor", "pointer")
          .append('svg:title')
          .text(v => v.values[s.key].valueTitle);


        if (ySubscale.bandwidth() > 15 && parseFloat(data.parameters["NumberOpacity"]) > 0) {
          d3.select(this).enterData(pivot.rows, 'text', 'number-label')
            .filter(r => r.values[s.key] != undefined && x(r.values[s.key] && r.values[s.key].value) > 16)
            .attr('y', r => ySubscale(s.key)! + ySubscale.bandwidth() / 2)
            .attr('x', r => x(r.values[s.key] && r.values[s.key].value) / 2)
            .attr('transform', r => 'translate(0, ' + y(keyColumn.getKey(r.rowValue)) + ')')
            .attr('opacity', data.parameters["NumberOpacity"])
            .attr('fill', data.parameters["NumberColor"])
            .attr('dominant-baseline', 'central')
            .attr('text-anchor', 'middle')
            .attr('font-weight', 'bold')
            .text(v => v.values[s.key].value)
            .on('click', v => me.props.onDrillDown(v.values[s.key].rowClick))
            .style("cursor", "pointer")
            .append('svg:title')
            .text(v => v.values[s.key].valueTitle);
        }
      });

    //paint color legend
    var legendScale = d3.scaleBand()
      .domain(pivot.columns.map((s, i) => i.toString()))
      .range([0, xRule.size('content')]);

    if (legendScale.bandwidth() > 50) {

      var legendMargin = yRule.size('legend') + 4;

      chart.append('svg:g').attr('class', 'color-legend').attr('transform', translate(xRule.start('content'), yRule.start('legend')))
        .enterData(pivot.columns, 'rect', 'color-rect')
        .attr('x', (e, i) => legendScale(i.toString())!)
        .attr('width', yRule.size('legend'))
        .attr('height', yRule.size('legend'))
        .attr('fill', s => s.color || color(s.key));

      chart.append('svg:g').attr('class', 'color-legend').attr('transform', translate(xRule.start('content') + legendMargin, yRule.middle('legend') + 1))
        .enterData(pivot.columns, 'text', 'color-text')
        .attr('x', (e, i) => legendScale(i.toString())!)
        .attr('dominant-baseline', 'middle')
        .text(s => s.niceName!)
        .each(function (s) { ellipsis(this as SVGTextElement, legendScale.bandwidth() - legendMargin); });
    }

    chart.append('svg:g').attr('class', 'x-axis').attr('transform', translate(xRule.start('content'), yRule.end('content')))
      .append('svg:line')
      .attr('class', 'x-axis')
      .attr('x2', xRule.size('content'))
      .style('stroke', 'Black');

    chart.append('svg:g').attr('class', 'y-axis').attr('transform', translate(xRule.start('content'), yRule.start('content')))
      .append('svg:line')
      .attr('class', 'y-axis')
      .attr('y2', yRule.size('content'))
      .style('stroke', 'Black');
  }

  renderLegend(pivot: ChartUtils.PivotTable, xRule: ChartUtils.Rule, yRule: ChartUtils.Rule) {

    var legendScale = d3.scaleBand()
      .domain(pivot.columns.map((s, i) => i.toString()))
      .range([0, xRule.size('content')]);


    if (legendScale.bandwidth() <= 50)
      return null;

    var legendMargin = yRule.size('legend') + 4;

    return (
      <g>

        <g className="color-legend" transform={translate(xRule.start('content'), yRule.start('legend'))}>
          {pivot.columns.map((s, i) => <rect className="color-rect"
            x={legendScale(i.toString())!}
            width={yRule.size('legend')}
            height={yRule.size('legend')}
            fill={s.color || color(s.key)} />)}
        </g>
        

        <g className="color-legend" transform={translate(xRule.start('content') + legendMargin, yRule.middle('legend') + 1)}>
          {pivot.columns.map((s, i) => <TextEllipsis maxWidth={legendScale.bandwidth() - legendMargin} className="color-text"
            x={legendScale(i.toString())!}
            dominantBaseline="middle">
            {s.niceName!}
          </TextEllipsis>)}
        </g>
      </g>
    ):


  }
}
