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
        _2 : 5, 
        labels: parseInt(data.parameters["UnitMargin"]),
        _3 : 5,
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
      
      
      var x = scaleFor(data.columns.c1, data.rows.map(function(r){return r.c1;}), 0, xRule.size('content'), data.parameters["HorizontalScale"]);
      
      var y = scaleFor(data.columns.c2, data.rows.map(function(r){return r.c2;}), 0, yRule.size('content'), data.parameters["VerticalScale"]);
      
      var xTickSize = data.columns.c1.type == "Date" ||data.columns.c1.type == "DateTime" ? 100 : 60; 
      
      var xTicks = x.ticks(width / xTickSize);
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
        .attr('dominant-baseline', 'middle')
      	.text(data.columns.c1.title);
      
      
      var yTicks = y.ticks(height / 50);
      var yTickFormat = y.tickFormat(height / 50);
      chart.append('svg:g').attr('class', 'y-lines').attr('transform', translate(xRule.start('content'), yRule.end('content')))
        .enterData(yTicks, 'line', 'y-lines')
        .attr('x2', xRule.size('content'))
        .attr('y1', function(t) { return -y(t); })
        .attr('y2', function(t) { return -y(t); })
        .style('stroke', 'LightGray');
      
      chart.append('svg:g').attr('class', 'y-tick').attr('transform', translate(xRule.start('ticks'), yRule.end('content')))
        .enterData(yTicks, 'line', 'y-tick')
        .attr('x2', xRule.size('ticks'))
        .attr('y1', function(t) { return -y(t); })
        .attr('y2', function(t) { return -y(t); })
        .style('stroke', 'Black');
      
      chart.append('svg:g').attr('class', 'y-label').attr('transform', translate(xRule.end('labels'), yRule.end('content')))
        .enterData(yTicks, 'text', 'y-label')
        .attr('y', function(t) { return -y(t); })
        .attr('dominant-baseline', 'middle')
        .attr('text-anchor', 'end')
        .text(yTickFormat);
      
      chart.append('svg:g').attr('class', 'y-title').attr('transform', translate(xRule.middle('title'), yRule.middle('content')) + rotate(270))
        .append('svg:text').attr('class', 'y-title')
        .attr('text-anchor', 'middle')
        .attr('dominant-baseline', 'middle')
        .text(data.columns.c2.title);
      
      var color;
      if(data.parameters["ColorScale"] == "Ordinal"){
        color = d3.scaleOrdinal(ChartUtils.getColorScheme(data.parameters["ColorScheme"], data.parameters["ColorSchemeSteps"])).domain( data.rows.map(function(v) { return v.c0; }));
      }else{
        var scaleFunc = scaleFor(data.columns.c0, data.rows.map(function(r){return r.c0;}), 0, 1, data.parameters["ColorScale"]);
        var colorInterpolate = data.parameters["ColorInterpolate"];
        var colorInterpolation = ChartUtils.getColorInterpolation(colorInterpolate); 
        color = function(v){return colorInterpolation(scaleFunc(v)); }
      }  
      var sizeList = data.rows.map(function(r){return r.c3;});
          
      var sizeTemp = scaleFor(data.columns.c3, sizeList, 0, 1, data.parameters["SizeScale"]);
      
      var totalSizeTemp = d3.sum(data.rows, function(p){ return sizeTemp(p.c3); });
      
      var sizeScale = scaleFor(data.columns.c3, sizeList, 0, (xRule.size('content') * yRule.size('content')) / (totalSizeTemp * 3), data.parameters["SizeScale"]);
    
      
      //PAINT GRAPH
      var gr = chart.enterData(data.rows.sort(function (p){ return -p.c3;})  , 'g', 'shape-serie').attr('transform', translate(xRule.start('content'), yRule.end('content')));
      
      gr
        .append('svg:circle').attr('class', 'shape')
        .attr('stroke', function(p) { return p.c0.color || color(p.c0); })
        .attr('stroke-width', 3)
        .attr('fill', function(p) { return p.c0.color || color(p.c0); })
        .attr('fill-opacity', data.parameters["FillOpacity"])
        .attr('shape-rendering', 'initial')
        .attr('r', function(p) { return Math.sqrt(sizeScale(p.c3)/Math.PI); })
        .attr('cx', function(p) { return x(p.c1); })
        .attr('cy', function(p) { return -y(p.c2); })
        ;
      
      if(data.parameters["ShowLabel"] == 'Yes')
      {
        gr.append('svg:text', 'number-label')
          .attr('y', function (v) { return -y(v.c2); })
          .attr('x', function (v) { return x(v.c1);})
          .attr('fill', function(p) { return  data.parameters["LabelColor"] || p.c0.color || color(p.c0); })
          .attr('dominant-baseline', 'central')
          .attr('text-anchor', 'middle')
          .attr('font-weight', 'bold')
          .text(function (v) { return v.c0.niceToString(); })
          .each(function (v) { ellipsis(this, Math.sqrt(sizeScale(v.c3)/Math.PI) *2, 0, ""); });
      }
      
      gr.attr('data-click', function(p) { return getClickKeys(p, data.columns); })
        .append('svg:title')
        .text(function(p) { return p.c0.niceToString() + 
          ("\n" + data.columns.c1.title +": " + p.c1.niceToString()) + 
          ("\n" + data.columns.c2.title +": " + p.c2.niceToString()) + 
          ("\n" + data.columns.c3.title +": " + p.c3.niceToString()) });
          
      
     
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
