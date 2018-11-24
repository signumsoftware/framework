import * as React from 'react'
import * as d3 from 'd3'
import D3ChartBase from './D3ChartBase';
import * as ChartUtils from '../Templates/ChartUtils';
import { translate, scale, rotate, skewX, skewY, matrix, scaleFor, rule, ellipsis } from '../Templates/ChartUtils';
import { ChartTable, ChartColumn } from '../ChartClient';


export default class MultiBarsChart extends D3ChartBase {

  drawChart(data: ChartTable, chart: d3.Selection<SVGElement, {}, null, undefined>, width: number, height: number) {

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

    var y = d3.scaleBand()
      .domain(pivot.rows.map(r => keyColumn.getKey(r.rowValue)))
      .range([0, yRule.size('content')]);

    var xTicks = x.ticks(width / 50);
    var xTickFormat = x.tickFormat(width / 50);
    chart.append('svg:g').attr('class', 'x-lines').attr('transform', translate(xRule.start('content'), yRule.start('content')))
      .enterData(xTicks, 'line', 'y-lines')
      .attr('x1', t => x(t))
      .attr('x2', t => x(t))
      .attr('y1', yRule.size('content'))
      .style('stroke', 'LightGray');

    chart.append('svg:g').attr('class', 'x-tick').attr('transform', translate(xRule.start('content'), yRule.start('ticks')))
      .enterData(xTicks, 'line', 'x-tick')
      .attr('x1', x)
      .attr('x2', x)
      .attr('y2', yRule.size('ticks'))
      .style('stroke', 'Black');

    chart.append('svg:g').attr('class', 'x-label').attr('transform', translate(xRule.start('content'), yRule.end('labels')))
      .enterData(xTicks, 'text', 'x-label')
      .attr('x', x)
      .attr('text-anchor', 'middle')
      .text(xTickFormat);

    chart.append('svg:g').attr('class', 'x-title').attr('transform', translate(xRule.middle('content'), yRule.middle('title')))
      .append('svg:text').attr('class', 'x-title')
      .attr('text-anchor', 'middle')
      .attr('dominant-baseline', 'middle')
      .text(pivot.title);

    chart.append('svg:g').attr('class', 'y-tick').attr('transform', translate(xRule.start('ticks'), yRule.start('content') + (y.bandwidth() / 2)))
      .enterData(pivot.rows, 'line', 'y-tick')
      .attr('x2', xRule.size('ticks'))
      .attr('y1', v => y(keyColumn.getKey(v.rowValue))!)
      .attr('y2', v => y(keyColumn.getKey(v.rowValue))!)
      .style('stroke', 'Black');

    if (y.bandwidth() > 15 && pivot.columns.length > 0) {

      chart.append('svg:g').attr('class', 'y-label').attr('transform', translate(xRule.end('labels'), yRule.start('content') + (y.bandwidth() / 2)))
        .enterData(pivot.rows, 'text', 'y-label')
        .attr('y', v => y(keyColumn.getKey(v.rowValue))!)
        .attr('dominant-baseline', 'middle')
        .attr('text-anchor', 'end')
        .text(v => keyColumn.getNiceName(v.rowValue))
        .each(function (v) { ellipsis(this as SVGTextElement, xRule.size('labels')); });
    }

    chart.append('svg:g').attr('class', 'y-label').attr('transform', translate(xRule.middle('title'), yRule.middle('content')) + rotate(270))
      .append('svg:text').attr('class', 'y-label')
      .attr('text-anchor', 'middle')
      .attr('dominant-baseline', 'middle')
      .text(keyColumn.title);

    var interMagin = 2;

    var color = d3.scaleOrdinal(ChartUtils.getColorScheme(data.parameters["ColorCategory"], parseInt(data.parameters["ColorCategorySteps"]))).domain(pivot.columns.map(s => s.key));

    var ySubscale = d3.scaleBand()
      .domain(pivot.columns.map(s => s.key))
      .range([interMagin, y.bandwidth() - interMagin]);

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
}
