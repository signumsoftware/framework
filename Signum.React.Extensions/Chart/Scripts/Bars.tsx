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
        labels: data.parameters["Labels"] == "Margin" ? parseInt(data.parameters["LabelsMargin"]) : 0,
        _3: data.parameters["Labels"] == "Margin" ? 5 : 0,
        ticks: 4,
        content: '*',
        _4: 5,
      }, width);
      //xRule.debugX(chart)
      
      var yRule = rule({
        _1 : 5,
        content: '*',
        ticks: 4,
        _2 : 5,
        labels: 10,
        _3 : 10,
        title: 15,
        _4 : 5,
      }, height);
      //yRule.debugY(chart);
      
    
      var x = scaleFor(data.columns.c1, data.rows.map(function (e) { return e.c1; }), 0, xRule.size('content'), data.parameters['Scale']);
      
      var y = d3.scaleBand()
          .domain(data.rows.map(function (e) { return e.c0; }))
          .range([0, yRule.size('content')]);
    
      var xTicks = x.ticks(width / 50);
      var xTickFormat = x.tickFormat(width / 50);
      
      chart.append('svg:g').attr('class', 'x-lines').attr('transform', translate(xRule.start('content'), yRule.start('content')))
        .enterData(xTicks, 'line', 'y-lines')
        .attr('x1', function(t) { return x(t); })
        .attr('x2', function(t) { return x(t); })
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
      	.text(data.columns.c1.title);
    
      chart.append('svg:g').attr('class', 'y-tick').attr('transform', translate(xRule.start('ticks'), yRule.end('content')))
    	.enterData(data.rows, 'line', 'y-tick')
    	.attr('x2', xRule.size('ticks'))
    	.attr('y1', function (v) { return -y(v.c0); })
    	.attr('y2', function (v) { return -y(v.c0); })
      	.style('stroke', 'Black');
    
      chart.append('svg:g').attr('class', 'y-title').attr('transform', translate(xRule.middle('title'), yRule.middle('content')) + rotate(270))
        .append('svg:text').attr('class', 'y-title')
        .attr('text-anchor', 'middle')
        .attr('dominant-baseline', 'central')
        .text(data.columns.c0.title);
    
      var color = d3.scaleOrdinal(ChartUtils.getColorScheme(data.parameters["ColorScheme"], data.parameters["ColorSchemeSteps"])).domain(data.rows.map(function (v) { return v.c0; }));
      //PAINT GRAPH
      chart.append('svg:g').attr('class', 'shape').attr('transform', translate(xRule.start('content'), yRule.start('content')))
    	.enterData(data.rows, 'rect', 'shape')
    	.attr('width', function (v) { return x(v.c1); })
    	.attr('height', y.bandwidth)
    	.attr('y', function (v) { return y(v.c0); })
    	.attr('fill', function (v) { return v.c0.color ||  color(v.c0); })
    	.attr('stroke', y.bandwidth() > 4 ? '#fff' : null)
    	.attr('data-click', function (v) { return getClickKeys(v, data.columns); })
    	.append('svg:title')
    	.text(function (d) { return d.c0.niceToString() + ': ' + d.c1.niceToString(); });
    
      if (y.bandwidth() > 15) 
      {
        if(data.parameters["Labels"] == "Margin")
        {
           chart.append('svg:g').attr('class', 'y-label').attr('transform', translate(xRule.end('labels'), yRule.start('content') + y.rangeBand() / 2))
            .enterData(data.rows, 'text', 'y-label')
            .attr('y', function (v) { return y(v.c0); })
            .attr('fill', function (v) { return  (v.c0.color || color(v.c0)); })
            .attr('dominant-baseline', 'central')
            .attr('text-anchor', 'end')
            .attr('font-weight', 'bold')
            .attr('data-click', function (v) { return getClickKeys(v, data.columns); })
            .text(function (v) { return v.c0.niceToString(); })
            .each(function (v) { var posx = x(v.c1); ellipsis(this, xRule.size('labels'), labelMargin); });
        }
        else if(data.parameters["Labels"] == "Inside")
        {
          var size = xRule.size('content');
          var labelMargin = 10;
          chart.append('svg:g').attr('class', 'y-label').attr('transform', translate(xRule.start('content') + labelMargin, yRule.start('content') + y.bandwidth() / 2))
            .enterData(data.rows, 'text', 'y-label')
            .attr('x', function (v) { var posx = x(v.c1); return posx >= size / 2 ? 0 : posx; })
            .attr('y', function (v) { return y(v.c0); })
            .attr('fill', function (v) { return x(v.c1) >= size / 2 ? '#fff' : (v.c0.color || color(v.c0)); })
            .attr('dominant-baseline', 'central')
            .attr('font-weight', 'bold')
            .attr('data-click', function (v) { return getClickKeys(v, data.columns); })
            .text(function (v) { return v.c0.niceToString(); })
            .each(function (v) { var posx = x(v.c1); ellipsis(this, posx >= size / 2 ? posx : size - posx, labelMargin); });
        }
        
        
        if(data.parameters["NumberOpacity"] > 0)
        {
          chart.append('svg:g').attr('class', 'numbers-label').attr('transform', translate(xRule.start('content'), yRule.start('content')))
            .enterData(data.rows, 'text', 'number-label')
            .filter(function(v) { return x(v.c1) > 20; })
            .attr('y', function (v) { return y(v.c0) + y.bandwidth() / 2; })
            .attr('x', function (v) { return x(v.c1) / 2;})
            .attr('fill',data.parameters["NumberColor"])
            .attr('dominant-baseline', 'central')
            .attr('opacity', data.parameters["NumberOpacity"])
            .attr('text-anchor', 'middle')
            .attr('font-weight', 'bold')
            .attr('data-click', function (v) { return getClickKeys(v, data.columns); })
            .text(function (v) { return v.c1.niceToString(); })
            .each(function (v) { var posx = x(v.c1); ellipsis(this, posx >= size / 2 ? posx : size - posx, labelMargin); });
          
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
