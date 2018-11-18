import * as React from 'react'
import * as d3 from 'd3'
import D3ChartBase from './D3ChartBase';
import * as ChartClient from '../ChartClient';
import * as ChartUtils from '../Templates/ChartUtils';
import { translate, scale, rotate, skewX, skewY, matrix, scaleFor, rule, ellipsis, PivotRow } from '../Templates/ChartUtils';
import { ChartTable, ChartColumn } from '../ChartClient';


export default class StackedBarsChart extends D3ChartBase {

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
      labels: data.parameters["Labels"] == "Margin" ? parseInt(data.parameters["LabelsMargin"]) : 0,
      _3: data.parameters["Labels"] == "Margin" ? 5 : 0,
      ticks: 4,
      content: '*',
      _4: 5,
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


    var y = d3.scaleBand()
      .domain(pivot.rows.map(r => keyColumn.getKey(r.rowValue)))
      .range([0, yRule.size('content')]);
    var pStack = data.parameters["Stack"];

    var stack = d3.stack<PivotRow>()
      .offset(ChartUtils.getStackOffset(pStack)!)
      .order(ChartUtils.getStackOrder(data.parameters["Order"])!)
      .keys(pivot.columns.map(d => d.key))
      .value((r, k) => {
        var v = r.values[k];
        return v && v.value || 0;
      });

    var stackedSeries = stack(pivot.rows);

    var max = d3.max(stackedSeries, s => d3.max(s, function (v) { return v[1]; }))!;
    var min = d3.min(stackedSeries, s => d3.min(s, function (v) { return v[0]; }))!;

    var x = d3.scaleLinear()
      .domain([min, max])
      .range([0, xRule.size('content')]);

    var xTicks = x.ticks(width / 60);

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

    var formatter = pStack == "expand" ? d3.format(".0%") :
      pStack == "zero" ? d3.format("") :
        (n: number) => d3.format("")(n) + "?";

    chart.append('svg:g').attr('class', 'x-label').attr('transform', translate(xRule.start('content'), yRule.end('labels')))
      .enterData(xTicks, 'text', 'x-label')
      .attr('x', x)
      .attr('text-anchor', 'middle')
      .text(formatter);

    chart.append('svg:g').attr('class', 'x-title').attr('transform', translate(xRule.middle('content'), yRule.middle('title')))
      .append('svg:text').attr('class', 'x-title')
      .attr('text-anchor', 'middle')
      .attr('dominant-baseline', 'middle')
      .text(pivot.title);


    chart.append('svg:g').attr('class', 'y-tick').attr('transform', translate(xRule.start('ticks'), yRule.end('content')))
      .enterData(pivot.rows, 'line', 'y-tick')
      .attr('x2', xRule.size('ticks'))
      .attr('y1', r => -y(keyColumn.getKey(r.rowValue))!)
      .attr('y2', r => -y(keyColumn.getKey(r.rowValue))!)
      .style('stroke', 'Black');

    chart.append('svg:g')
      .attr('class', 'y-title')
      .attr('transform', translate(xRule.middle('title'), yRule.middle('content')) + rotate(270))
      .append('svg:text').attr('class', 'y-title')
      .attr('text-anchor', 'middle')
      .attr('dominant-baseline', 'middle')
      .text(keyColumn.title);

    var color = d3.scaleOrdinal(ChartUtils.getColorScheme(data.parameters["ColorScheme"], parseInt(data.parameters["ColorSchemeSteps"]))).domain(pivot.columns.map(s => s.key));

    var me = this;
    //PAINT GRAPH
    chart.enterData(stackedSeries, 'g', 'shape-serie').attr('transform', translate(xRule.start('content'), yRule.start('content')))
      .each(function (s) {

        d3.select(this).enterData(s, 'rect', 'shape')
          .filter(r => r.data.values[s.key] != undefined)
          .attr('stroke', y.bandwidth() > 4 ? '#fff' : null)
          .attr('fill', r => color(s.key))
          .attr('height', y.bandwidth())
          .attr('width', r => x(r[1]) - x(r[0]))
          .attr('x', r => x(r[0]))
          .attr('y', r => y(keyColumn.getKey(r.data.rowValue))!)
          .on('click', r => me.props.onDrillDown(r.data.values[s.key].rowClick))
          .style("cursor", "pointer")
          .append('svg:title')
          .text(r => r.data.values[s.key].valueTitle);

        if (y.bandwidth() > 15 && parseFloat(data.parameters["NumberOpacity"]) > 0) {
          d3.select(this).enterData(s, 'text', 'number-label')
            .filter(p => (x(p[1]) - x(p[0])) > 20)
            .attr('x', p => x(p[0]) * 0.5 + x(p[1]) * 0.5)
            .attr('y', p => y(keyColumn.getKey(p.data.rowValue))! + y.bandwidth() / 2)
            .attr('fill', data.parameters["NumberColor"])
            .attr('dominant-baseline', 'central')
            .attr('opacity', data.parameters["NumberOpacity"])
            .attr('text-anchor', 'middle')
            .attr('font-weight', 'bold')
            .text(p => p.data.values[s.key].value)
            .on('click', p => me.props.onDrillDown(p.data.values[s.key].rowClick))
            .style("cursor", "pointer")
            .append('svg:title')
            .text(p => p.data.values[s.key].valueTitle);
        }
      });


    if (y.bandwidth() > 15 && pivot.columns.length > 0) {

      if (data.parameters["Labels"] == "Margin") {
        chart.append('svg:g').attr('class', 'y-label').attr('transform', translate(xRule.end('labels'), yRule.start('content') + y.bandwidth() / 2))
          .enterData(pivot.rows, 'text', 'y-label')
          .attr('y', v => y(keyColumn.getKey(v.rowValue))!)
          .attr('dominant-baseline', 'central')
          .attr('text-anchor', 'end')
          .text(v => keyColumn.getNiceName(v.rowValue))
          .each(function (v) { ellipsis(this as SVGTextElement, xRule.size('labels'), labelMargin); });
      }
      else if (data.parameters["Labels"] == "Inside") {
        var maxValue = (rowIndex: number) => stackedSeries[stackedSeries.length - 1][rowIndex][1];

        var size = xRule.size('content');
        var labelMargin = 5;
        chart.append('svg:g').attr('class', 'y-axis-tick-label').attr('transform', translate(xRule.start('content'), yRule.start('content') + y.bandwidth() / 2))
          .enterData(pivot.rows, 'text', 'y-axis-tick-label sf-chart-strong')
          .attr('y', r => y(keyColumn.getKey(r.rowValue))!)
          .attr('x', (r, i) => x(maxValue(i)) >= size / 2 ? 0 : x(maxValue(i)))
          .attr('dx', (r, i) => labelMargin)
          .attr('text-anchor', r => 'start')
          .attr('fill', (r, i) => x(maxValue(i)) >= size / 2 ? '#fff' : '#000')
          .attr('dominant-baseline', 'central')
          .attr('font-weight', 'bold')
          .text(r => keyColumn.getNiceName(r.rowValue))
          .each(function (r, i) { var posx = x(maxValue(i)); ellipsis(this as SVGTextElement, posx >= size / 2 ? posx : size - posx, labelMargin); });
      }
    }

    var legendScale = d3.scaleBand()
      .domain(pivot.columns.map((s, i) => i.toString()))
      .range([0, xRule.size('content')]);

    if (legendScale.bandwidth() > 50) {
      var legendMargin = yRule.size('legend') + 4;
      chart.append('svg:g').attr('class', 'color-legend').attr('transform', translate(xRule.start('content'), yRule.start('legend')))
        .enterData(pivot.columns, 'rect', 'color-rect')
        .attr('x', (s, i) => legendScale(i.toString())!)
        .attr('width', yRule.size('legend'))
        .attr('height', yRule.size('legend'))
        .attr('fill', s => s.color || color(s.key));

      chart.append('svg:g').attr('class', 'color-legend').attr('transform', translate(xRule.start('content') + legendMargin, yRule.middle('legend') + 1))
        .enterData(pivot.columns, 'text', 'color-text')
        .attr('x', (s, i) => legendScale(i.toString())!)
        .attr('dominant-baseline', 'middle')
        .text(s => s.niceName!)
        .each(function (s) { ellipsis(this as SVGTextElement, legendScale.bandwidth() - legendMargin); });
    }

    chart.append('svg:g')
      .attr('class', 'x-axis')
      .attr('transform', translate(xRule.start('content'), yRule.end('content')))
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
