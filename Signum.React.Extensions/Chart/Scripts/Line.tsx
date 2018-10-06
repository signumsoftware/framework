import * as React from 'react'
import * as D3 from 'd3'
import D3ChartScriptRendererBase from '../ChartRenderer';
import * as ChartClient from '../ChartClient';
import * as ChartUtils from '../Templates/ChartUtils';
import { getClickKeys, translate, scale, rotate, skewX, skewY, matrix, scaleFor, rule, ellipsis } from '../Templates/ChartUtils';


export default class BarsChartScriptRendererBase extends D3ChartScriptRendererBase {

    drawChart(data: ChartClient.ChartTable, chart: D3.Selection<SVGElement, {}, HTMLDivElement, unknown >) {
              
        var xRule = rule({
        _1 : 5,
        title : 15,
        _2 : 10, 
        labels : parseInt(data.parameters["UnitMargin"]),
        _3 : 5,
        ticks: 4,
        content: '*',
        _4: 10,
      }, width);
      //xRule.debugX(chart)
      
      var yRule = rule({
        _2 : data.parameters["NumberOpacity"] > 0 ? 20 : 5,
        content: '*',
        ticks: 4,
        _3 : 5,
        labels0: 15,
        labels1: 15,
        _4 : 10,
        title: 15,
        _5 : 5,
      }, height);
      //yRule.debugY(chart);
     
      
      var x = d3.scaleBand()
          .domain(data.rows.map(function (e) { return e.c0; }))
          .range([0, xRule.size('content')]);
      
      var y = scaleFor(data.columns.c1, data.rows.map(function(r){return r.c1;}), 0, yRule.size('content'), data.parameters["Scale"]);
      
      
      chart.append('svg:g').attr('class', 'x-tick').attr('transform', translate(xRule.start('content')+ (x.bandwidth() / 2), yRule.start('ticks')))
        .enterData(data.rows, 'line', 'x-tick')
          .attr('y2',  function (r, i) { return yRule.start('labels' + (i % 2)) - yRule.start('ticks'); })
          .attr('x1', function (r) { return x(r.c0); })
          .attr('x2', function (r) { return x(r.c0); })
          .style('stroke', 'Black');
      
      if ((x.bandwidth() * 2) > 60) 
      {
        chart.append('svg:g').attr('class', 'x-label').attr('transform', translate(xRule.start('content')+ (x.bandwidth() / 2), yRule.middle('labels0')))
          .enterData(data.rows, 'text', 'x-label')
            .attr('x', function (r) { return x(r.c0); })
            .attr('y', function (r, i) { return yRule.middle('labels' + (i % 2)) - yRule.middle('labels0'); })
            .attr('dominant-baseline', 'middle')
            .attr('text-anchor', 'middle')
            .text(function (r) { return r.c0.niceToString(); })
            .each(function (v) { ellipsis(this, x.bandwidth() * 2); });
      }
      
      chart.append('svg:g').attr('class', 'x-title').attr('transform', translate(xRule.middle('content'), yRule.middle('title')))
        .append('svg:text').attr('class', 'x-title')
        .attr('text-anchor', 'middle')
        .attr('dominant-baseline', 'middle')
        .text(data.columns.c0.title);
      
      var yTicks = y.ticks(height / 50);
      var yTickFormat = y.tickFormat(height / 50);
      chart.append('svg:g').attr('class', 'y-line').attr('transform', translate(xRule.start('content'), yRule.end('content')))
        .enterData(yTicks, 'line', 'y-line')
        .attr('x2', xRule.size('content'))
        .attr('y1', function (t) { return -y(t); })
        .attr('y2', function (t) { return -y(t); })
        .style('stroke', 'LightGray');
      
      chart.append('svg:g').attr('class', 'y-tick').attr('transform', translate(xRule.start('ticks'), yRule.end('content')))
        .enterData(yTicks, 'line', 'y-tick')
        .attr('x2', xRule.size('ticks'))
        .attr('y1', function (t) { return -y(t); })
        .attr('y2', function (t) { return -y(t); })
        .style('stroke', 'Black');
      
      chart.append('svg:g').attr('class', 'y-label').attr('transform',  translate(xRule.end('labels'), yRule.end('content')))
        .enterData(yTicks, 'text', 'y-label')
        .attr('y', function (t) { return -y(t); })
        .attr('dominant-baseline', 'middle')
        .attr('text-anchor', 'end')
        .text(yTickFormat);
      
      chart.append('svg:g').attr('class', 'y-label').attr('transform', translate(xRule.middle('title'), yRule.middle('content')) + rotate(270))
        .append('svg:text').attr('class', 'y-label')
       	.attr('text-anchor', 'middle')
        .attr('dominant-baseline', 'middle')
        .text(data.columns.c1.title);
      
      var line = d3.line()
        .x(function(r) { return x(r.c0); })
        .y(function(r) { return -y(r.c1); })
        .curve(ChartUtils.getCurveByName(data.parameters["Interpolate"]));//"linear"
    
      var color = data.parameters["Color"];// 'steelblue'
      //PAINT CHART
      chart.append('svg:g').attr('class', 'shape').attr('transform', translate(xRule.start('content') + (x.bandwidth() / 2), yRule.end('content')))
        .append('svg:path').attr('class', 'shape')
          .attr('stroke', color)
          .attr('fill', 'none')
          .attr('stroke-width', 3)
          .attr('shape-rendering', 'initial')
          .attr('d', line(data.rows))
          
      //paint graph - hover area trigger
      chart.append('svg:g').attr('class', 'hover-trigger').attr('transform', translate(xRule.start('content') + (x.bandwidth() / 2), yRule.end('content')))
        .enterData(data.rows, 'circle', 'hover-trigger')
          .attr('cx', function (v) { return x(v.c0); })
          .attr('cy', function (v) { return -y(v.c1); })
          .attr('r', 15)
          .attr('fill', '#fff')
          .attr('fill-opacity', 0)
          .attr('stroke', 'none')
          .attr('data-click', function (v) { return getClickKeys(v, data.columns); })
          .append('svg:title')
          .text(function (v) { return v.c0.niceToString() + ': ' + v.c1.niceToString(); });
          
      //paint graph - points
      chart.append('svg:g').attr('class', 'point').attr('transform', translate(xRule.start('content') + (x.bandwidth() / 2), yRule.end('content')))
        .enterData(data.rows, 'circle', 'point')
          .attr('fill', color)
          .attr('r', 5)
          .attr('cx', function (v) { return x(v.c0); })
          .attr('cy', function (v) { return -y(v.c1); })
          .attr('data-click', function (v) { return getClickKeys(v, data.columns); })
          .attr('shape-rendering', 'initial')
          .append('svg:title')
          .text(function (v) { return v.c0.niceToString() + ': ' + v.c1.niceToString(); });
      
       if(data.parameters["NumberOpacity"] > 0){
      
          //paint graph - point-labels
          chart.append('svg:g').attr('class', 'point-label').attr('transform', translate(xRule.start('content') + (x.bandwidth() / 2), yRule.end('content')))
          .enterData(data.rows, 'text', 'point-label')
            .attr('r', 5)
            .attr('x', function (v) { return x(v.c0); })
            .attr('y', function (v) { return -y(v.c1) - 10; })
            .attr('opacity', data.parameters["NumberOpacity"])
            .attr('text-anchor', 'middle')
            .attr('data-click', function (v) { return getClickKeys(v, data.columns); })
            .attr('shape-rendering', 'initial')
            .text(function (v) { return v.c1.niceToString(); });
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
