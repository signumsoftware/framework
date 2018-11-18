import * as React from 'react'
import * as d3 from 'd3'
import D3ChartBase from './D3ChartBase';
import * as ChartClient from '../ChartClient';
import * as ChartUtils from '../Templates/ChartUtils';
import { translate, scale, rotate, skewX, skewY, matrix, scaleFor, rule, ellipsis, } from '../Templates/ChartUtils';
import { ChartRow } from '../ChartClient';


export default class BarsChart extends D3ChartBase {

  drawChart(data: ChartClient.ChartTable, chart: d3.Selection<SVGElement, {}, null, undefined>, width: number, height: number) {

    var keyColumn = data.columns.c0!;
    var valueColumn = data.columns.c1! as ChartClient.ChartColumn<number>;

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
      content: '*',
      ticks: 4,
      _2: 5,
      labels: 10,
      _3: 10,
      title: 15,
      _4: 5,
    }, height);
    //yRule.debugY(chart);


    var x = scaleFor(valueColumn, data.rows.map(r => valueColumn.getValue(r)), 0, xRule.size('content'), data.parameters['Scale']);

    var y = d3.scaleBand()
      .domain(data.rows.map(r => keyColumn.getValueKey(r)))
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
      .attr('dominant-baseline', 'central')
      .text(valueColumn.title || "");

    chart.append('svg:g').attr('class', 'y-tick').attr('transform', translate(xRule.start('ticks'), yRule.end('content')))
      .enterData(data.rows, 'line', 'y-tick')
      .attr('x2', xRule.size('ticks'))
      .attr('y1', r => -y(keyColumn.getValueKey(r))!)
      .attr('y2', r => -y(keyColumn.getValueKey(r))!)
      .style('stroke', 'Black');

    chart.append('svg:g').attr('class', 'y-title').attr('transform', translate(xRule.middle('title'), yRule.middle('content')) + rotate(270))
      .append('svg:text').attr('class', 'y-title')
      .attr('text-anchor', 'middle')
      .attr('dominant-baseline', 'central')
      .text(keyColumn.title || "");

    var color = d3.scaleOrdinal(ChartUtils.getColorScheme(data.parameters["ColorScheme"], parseInt(data.parameters["ColorSchemeSteps"]!)))
      .domain(data.rows.map(r => keyColumn.getValueKey(r)));

    //PAINT GRAPH
    chart.append('svg:g').attr('class', 'shape').attr('transform', translate(xRule.start('content'), yRule.start('content')))
      .enterData(data.rows, 'rect', 'shape')
      .attr('width', r => x(valueColumn.getValue(r)))
      .attr('height', y.bandwidth)
      .attr('y', r => y(keyColumn.getValueKey(r))!)
      .attr('fill', r => keyColumn.getValueColor(r) || color(keyColumn.getValueKey(r)))
      .attr('stroke', y.bandwidth() > 4 ? '#fff' : null)
      .on('click', r => this.props.onDrillDown(r))
      .style("cursor", "pointer")
      .append('svg:title')
      .text(r => keyColumn.getValueNiceName(r) + ': ' + valueColumn.getValueNiceName(r));

    if (y.bandwidth() > 15) {
      if (data.parameters["Labels"] == "Margin") {
        chart.append('svg:g').attr('class', 'y-label').attr('transform', translate(xRule.end('labels'), yRule.start('content') + y.bandwidth() / 2))
          .enterData(data.rows, 'text', 'y-label')
          .attr('y', r => y(keyColumn.getValueKey(r))!)
          .attr('fill', r => (keyColumn.getValueColor(r) || color(keyColumn.getValueKey(r))))
          .attr('dominant-baseline', 'central')
          .attr('text-anchor', 'end')
          .attr('font-weight', 'bold')
          .on('click', r => this.props.onDrillDown(r))
          .style("cursor", "pointer")
          .text(r => keyColumn.getValueNiceName(r))
          .each(function (r) { var posx = x(valueColumn.getValue(r)); ChartUtils.ellipsis(this as SVGTextElement, xRule.size('labels'), labelMargin); });
      }
      else if (data.parameters["Labels"] == "Inside") {
        var size = xRule.size('content');
        var labelMargin = 10;
        chart.append('svg:g').attr('class', 'y-label').attr('transform', translate(xRule.start('content') + labelMargin, yRule.start('content') + y.bandwidth() / 2))
          .enterData(data.rows, 'text', 'y-label')
          .attr('x', r => { var posx = x(valueColumn.getValue(r)); return posx >= size / 2 ? 0 : posx; })
          .attr('y', r => y(keyColumn.getValueKey(r))!)
          .attr('fill', r => x(valueColumn.getValue(r)) >= size / 2 ? '#fff' : (keyColumn.getValueColor(r) || color(keyColumn.getValueKey(r))))
          .attr('dominant-baseline', 'central')
          .attr('font-weight', 'bold')
          .on('click', r => this.props.onDrillDown(r))
          .style("cursor", "pointer")
          .text(r => keyColumn.getValueNiceName(r))
          .each(function (r) { var posx = x(valueColumn.getValue(r)); ChartUtils.ellipsis(this as SVGTextElement, posx >= size / 2 ? posx : size - posx, labelMargin); });
      }


      if (parseFloat(data.parameters["NumberOpacity"]) > 0) {
        chart.append('svg:g').attr('class', 'numbers-label').attr('transform', translate(xRule.start('content'), yRule.start('content')))
          .enterData(data.rows, 'text', 'number-label')
          .filter(r => x(valueColumn.getValue(r)) > 20)
          .attr('y', r => y(keyColumn.getValueKey(r))! + y.bandwidth() / 2)
          .attr('x', r => x(valueColumn.getValue(r)) / 2)
          .attr('fill', data.parameters["NumberColor"] || "#000")
          .attr('dominant-baseline', 'central')
          .attr('opacity', data.parameters["NumberOpacity"])
          .attr('text-anchor', 'middle')
          .attr('font-weight', 'bold')
          .on('click', r => this.props.onDrillDown(r))
          .style("cursor", "pointer")
          .text(r => valueColumn.getValueNiceName(r))
          .each(function (r) { var posx = x(valueColumn.getValue(r)); ChartUtils.ellipsis(this as SVGTextElement, posx >= size / 2 ? posx : size - posx, labelMargin); });

      }
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
