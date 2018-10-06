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
        labels : parseInt(data.parameters["UnitMargin"]) ,
        _3 : 5,
        ticks: 4,
        content: '*',
        _4: 10,
      }, width);
      //xRule.debugX(chart)
      
      var yRule = rule({
        _1 : 5,
        legend : 15,
        _2 : 5,
        content: '*',
        ticks: 4,
        _3: data.parameters["Labels"] == "Margin" ? 5 : 0,
        labels: data.parameters["Labels"] == "Margin" ? parseInt(data.parameters["LabelsMargin"]) : 0,
        _4 : 10,
        title: 15,
        _5 : 5,
      }, height);
      //yRule.debugY(chart);
      
      
      var x = d3.scaleBand()
          .domain(data.rows.map(function (e) { return e.c0; }))
          .range([0, xRule.size('content')]);
      
      var y = scaleFor(data.columns.c1, data.rows.map(function(r){return r.c1;}), 0, yRule.size('content'), data.parameters["Scale"]);
      
      chart.append('svg:g').attr('class', 'x-title').attr('transform', translate(xRule.middle('content'), yRule.middle('title')))
        .append('svg:text').attr('class', 'x-title')
        .attr('text-anchor', 'middle')
        .attr('dominant-baseline', 'middle')
        .text(data.columns.c0.title);
      
      var yTicks = y.ticks(10);
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
      
      var color = d3.scaleOrdinal(ChartUtils.getColorScheme(data.parameters["ColorScheme"], data.parameters["ColorSchemeSteps"])).domain( data.rows.map(function (v) { return v.c0; }));
    
      
      //PAINT CHART
      chart.append('svg:g').attr('class', 'shape').attr('transform', translate(xRule.start('content'), yRule.end('content')))
        .enterData(data.rows, 'rect', 'shape')
        .attr('y', function (v) { return -y(v.c1); })
        .attr('height', function (v) { return y(v.c1); })
        .attr('width', x.bandwidth)
        .attr('x', function (v) { return x(v.c0); })
        .attr('fill', function (v) { return v.c0.color || color(v.c0); })
        .attr('stroke', x.bandwidth() > 4 ? '#fff' : null)
        .attr('data-click', function (v) { return getClickKeys(v, data.columns); })
        .append('svg:title')
        .text(function (d) { return d.c0.niceToString() + ': ' + d.c1.niceToString(); });
      
      if (x.bandwidth() > 15)
      { 
         if(data.parameters["Labels"] == "Margin")
         {
            chart.append('svg:g').attr('class', 'x-label').attr('transform', translate(xRule.start('content'), yRule.start('labels')))
              .enterData(data.rows, 'text', 'x-label')
              .attr('transform', function (v){ return translate( x(v.c0) + x.bandwidth()/2, 0 ) +  rotate(-90);})
              .attr('dominant-baseline', 'middle')
              .attr('font-weight', 'bold')
              .attr('fill', function (v) { return (v.c0.color || color(v.c0)); })
              .attr('text-anchor', "end")
              .attr('data-click', function (v) { return getClickKeys(v, data.columns); })
              .text(function (v) { return v.c0.niceToString(); })
              .each(function (v) { ellipsis(this,  yRule.size('labels'), labelMargin); });
         }
         else if(data.parameters["Labels"] == "Inside")
         {
            var size = yRule.size('content');    
            var labelMargin = 10;    
            chart.append('svg:g').attr('class', 'x-label').attr('transform', translate(xRule.start('content'), yRule.end('content')))
              .enterData(data.rows, 'text', 'x-label')
              .attr('transform', function (v){ return translate( x(v.c0) + x.bandwidth()/2, -y(v.c1) ) +   rotate(-90);})
              .attr('dominant-baseline', 'middle')
              .attr('font-weight', 'bold')
              .attr('fill', function (v) { return y(v.c1) >= size / 2 ? '#fff' : (v.c0.color || color(v.c0)); })
              .attr('dx', function (v) { return y(v.c1) >= size / 2 ? -labelMargin : labelMargin; })
              .attr('text-anchor', function (v) { return y(v.c1) >= size / 2 ? 'end' : 'start'; })
              .attr('data-click', function (v) { return getClickKeys(v, data.columns); })
              .text(function (v) { return v.c0.niceToString(); })
              .each(function (v) { var posy = y(v.c1); ellipsis(this, posy >= size / 2 ? posy : size - posy, labelMargin); });
         }
        
         if(data.parameters["NumberOpacity"] > 0)
         {
           chart.append('svg:g').attr('class', 'numbers-label').attr('transform', translate(xRule.start('content'), yRule.end('content')))
           .enterData(data.rows, 'text', 'number-label')
           .filter(function(v){return y(v.c1) > 10; })
           .attr('transform', function (v){ return translate( x(v.c0) + x.bandwidth()/2, -y(v.c1) / 2 ) +  rotate(-90);})
           .attr('fill', data.parameters["NumberColor"])
           .attr('dominant-baseline', 'central')
           .attr('opacity', data.parameters["NumberOpacity"])
           .attr('text-anchor', 'middle')
           .attr('font-weight', 'bold')
           .attr('data-click', function (v) { return getClickKeys(v, data.columns); })
           .text(function (v) { return v.c1.niceToString(); });
    
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
