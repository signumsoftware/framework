import * as React from 'react'
import * as d3 from 'd3'
import D3ChartBase from './D3ChartBase';
import * as ChartClient from '../ChartClient';
import * as ChartUtils from '../Templates/ChartUtils';
import { translate, scale, rotate, skewX, skewY, matrix, scaleFor, rule, ellipsis, PivotRow } from '../Templates/ChartUtils';
import { ChartTable, ChartColumn } from '../ChartClient';


export default class StackedColumnsChart extends D3ChartBase {

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
      _3: data.parameters["Labels"] == "Margin" ? 5 : 0,
      labels: data.parameters["Labels"] == "Margin" ? parseInt(data.parameters["LabelsMargin"]) : 0,
      _4: 10,
      title: 15,
      _5: 5,
    }, height);
    //yRule.debugY(chart);

    var keyValues = ChartUtils.completeValues(keyColumn, pivot.rows.map(r => r.rowValue), data.parameters['CompleteValues'], ChartUtils.insertPoint(keyColumn, valueColumn0));

    var x = d3.scaleBand()
      .domain(keyValues.map(v => keyColumn.getKey(v)))
      .range([0, xRule.size('content')]);

    var pStack = data.parameters["Stack"];

    var stack = d3.stack<PivotRow>()
      .offset(ChartUtils.getStackOffset(pStack)!)
      .order(ChartUtils.getStackOrder(data.parameters["Order"])!)
      .keys(pivot.columns.map(d => d.key))
      .value(function (r, k) {
        var v = r.values[k];
        return v && v.value || 0;
      });

    var stackedSeries = stack(pivot.rows);

    var max = d3.max(stackedSeries, s => d3.max(s, vs => vs[1]))!;
    var min = d3.min(stackedSeries, s => d3.min(s, vs => vs[0]))!;

    var y = d3.scaleLinear()
      .domain([min, max])
      .range([0, yRule.size('content')]);

    chart.append('svg:g')
      .attr('class', 'x-title')
      .attr('transform', translate(xRule.middle('content'), yRule.middle('title')))
      .append('svg:text').attr('class', 'x-title')
      .attr('text-anchor', 'middle')
      .attr('dominant-baseline', 'middle')
      .text(keyColumn.title);


    var yTicks = y.ticks(10);
    chart.append('svg:g').attr('class', 'y-line').attr('transform', translate(xRule.start('content'), yRule.end('content')))
      .enterData(yTicks, 'line', 'y-line')
      .attr('x2', xRule.size('content'))
      .attr('y1', t => -y(t))
      .attr('y2', t => -y(t))
      .style('stroke', 'LightGray');

    chart.append('svg:g').attr('class', 'y-tick').attr('transform', translate(xRule.start('ticks'), yRule.end('content')))
      .enterData(yTicks, 'line', 'y-tick')
      .attr('x2', xRule.size('ticks'))
      .attr('y1', t => -y(t))
      .attr('y2', t => -y(t))
      .style('stroke', 'Black');

    var formatter = pStack == "expand" ? d3.format(".0%") :
      pStack == "zero" ? d3.format("") :
        (n: number) => d3.format("")(n) + "?";

    chart.append('svg:g').attr('class', 'y-label').attr('transform', translate(xRule.end('labels'), yRule.end('content')))
      .enterData(yTicks, 'text', 'y-label')
      .attr('y', t => -y(t))
      .attr('dominant-baseline', 'middle')
      .attr('text-anchor', 'end')
      .text(formatter);

    chart.append('svg:g').attr('class', 'y-label').attr('transform', translate(xRule.middle('title'), yRule.middle('content')) + rotate(270))
      .append('svg:text').attr('class', 'y-label')
      .attr('text-anchor', 'middle')
      .attr('dominant-baseline', 'middle')
      .text(pivot.title);

    var color = d3.scaleOrdinal(ChartUtils.getColorScheme(data.parameters["ColorCategory"], parseInt(data.parameters["ColorCategorySteps"])))
      .domain(pivot.columns.map(c => c.key));

    const me = this;
    //PAINT CHART
    chart.enterData(stackedSeries, 'g', 'shape-serie').attr('transform', translate(xRule.start('content'), yRule.end('content')))
      .each(function (s) {

        d3.select(this).enterData(s, 'rect', 'shape')
          .filter(r => r.data.values[s.key] != undefined)
          .attr('stroke', x.bandwidth() > 4 ? '#fff' : null)
          .attr('fill', r => color(s.key))
          .attr('x', r => x(keyColumn.getKey(r.data.rowValue))!)
          .attr('width', x.bandwidth())
          .attr('height', r => y(r[1]) - y(r[0]))
          .attr('y', r => -y(r[1]))
          .on('click', r => me.props.onDrillDown(r.data.values[s.key].rowClick))
          .style("cursor", "pointer")
          .append('svg:title')
          .text(r => r.data.values[s.key].valueTitle);

        if (parseFloat(data.parameters["NumberOpacity"]) > 0) {
          d3.select(this).enterData(s, 'text', 'number-label')
            .filter(r => (y(r[1]) - y(r[0])) > 10)
            .attr('x', r => x(keyColumn.getKey(r.data.rowValue))! + x.bandwidth() / 2)
            .attr('y', r => -y(r[0]) * 0.5 - y(r[1]) * 0.5)
            .attr('fill', data.parameters["NumberColor"])
            .attr('dominant-baseline', 'central')
            .attr('opacity', data.parameters["NumberOpacity"])
            .attr('text-anchor', 'middle')
            .attr('font-weight', 'bold')
            .text(r => valueColumn0.getNiceName(r.data.values[s.key].value))
            .on('click', r => me.props.onDrillDown(r.data.values[s.key].rowClick))
            .style("cursor", "pointer")
            .append('svg:title')
            .text(r => r.data.values[s.key].valueTitle);
        }

      });


    if (x.bandwidth() > 15) {

      if (data.parameters["Labels"] == "Margin") {
        chart.append('svg:g').attr('class', 'x-label').attr('transform', translate(xRule.start('content'), yRule.start('labels')))
          .enterData(pivot.rows, 'text', 'x-label')
          .attr('transform', r => translate(x(keyColumn.getKey(r.rowValue))! + x.bandwidth() / 2, 0) + rotate(-90))
          .attr('dominant-baseline', 'middle')
          .attr('fill', 'black')
          .attr('shape-rendering', 'geometricPrecision')
          .attr('text-anchor', "end")
          .text(r => keyColumn.getNiceName(r.rowValue))
          .each(function (r) { ellipsis(this as SVGTextElement, yRule.size('labels'), labelMargin); });
      }
      else if (data.parameters["Labels"] == "Inside") {
        const maxValue = (rowIndex: number) => stackedSeries[stackedSeries.length - 1][rowIndex][1];

        var labelMargin = 10;
        var size = yRule.size('content');

        chart.append('svg:g').attr('class', 'x-label').attr('transform', translate(xRule.start('content'), yRule.end('content')))
          .enterData(pivot.rows, 'text', 'x-label')
          .attr('transform', (r, i) => translate(x(keyColumn.getKey(r.rowValue))! + x.bandwidth() / 2, y(maxValue(i)) >= size / 2 ? 0 : -y(maxValue(i))) + rotate(-90))
          .attr('dominant-baseline', 'middle')
          .attr('font-weight', 'bold')
          .attr('fill', (r, i) => y(maxValue(i)) >= size / 2 ? '#fff' : '#000')
          .attr('dx', (r, i) => labelMargin)
          .attr('text-anchor', (r, i) =>  'start')
          .text(r => keyColumn.getNiceName(r.rowValue))
          .each(function (r, i) { var posy = y(maxValue(i)); ellipsis(this as SVGTextElement, posy >= size / 2 ? posy : size - posy, labelMargin); });
      }
    }

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
