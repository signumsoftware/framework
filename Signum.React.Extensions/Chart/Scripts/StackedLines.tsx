import * as React from 'react'
import * as D3 from 'd3'
import D3ChartScriptRendererBase from '../ChartRenderer';
import * as ChartClient from '../ChartClient';
import * as ChartUtils from '../Templates/ChartUtils';
import { getClickKeys, translate, scale, rotate, skewX, skewY, matrix, scaleFor, rule, ellipsis } from '../Templates/ChartUtils';


export default class BarsChartScriptRendererBase extends D3ChartScriptRendererBase {

    drawChart(data: ChartClient.ChartTable, chart: D3.Selection<SVGElement, {}, HTMLDivElement, unknown >) {
            
       var pivot = data.columns.c1.token == null ?  
           ChartUtils.toPivotTable(data, "c0", ["c2", "c3", "c4", "c5", "c6"]): 
           ChartUtils.groupedPivotTable(data, "c0", "c1", "c2");
       
      var xRule = rule({
        _1 : 5,
       title : 15,
       _2 : 10, 
       labels : parseInt(data.parameters["Horizontal Margin"]),
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
        _3 : 5,
        labels0: 15,
        labels1: 15,
        _4 : 10,
        title: 15,
        _5 : 5,
      }, height);
      //yRule.debugY(chart);
      
      var x = d3.scaleBand()
          .domain(pivot.rows.map(function (d) { return d.rowValue.key; }))
          .range([0, xRule.size('content')]);
      
      var pStack = data.parameters["Stack"];
      
      var stack = d3.stack()
        .offset(ChartUtils.getStackOffset(pStack))
        .order(ChartUtils.getStackOrder(data.parameters["Order"]))
        .keys(pivot.columns.map(function(d) { return d.key; }))
        .value(function(r, k){ 
          var v = r.values[k]; 
          return v && v.value && v.value.key || 0; 
        });
     
      var stackedSeries = stack(pivot.rows);
      
      var max = d3.max(stackedSeries, function(s){ return d3.max(s, function(v){return v[1];}); });
      var min = d3.min(stackedSeries, function(s){ return d3.min(s, function(v){return v[0];}); });
      
      var y = d3.scaleLinear()
          .domain([min, max])
          .range([0, yRule.size('content')]);
      
      chart.append('svg:g').attr('class', 'x-tick').attr('transform', translate(xRule.start('content')+ (x.bandwidth() / 2), yRule.start('ticks')))
        .enterData(pivot.rows, 'line', 'x-tick')
          .attr('y2',  function (d, i) { return yRule.start('labels' + (i % 2)) - yRule.start('ticks'); })
          .attr('x1', function (d) { return x(d.rowValue); })
          .attr('x2', function (d) { return x(d.rowValue); })
          .style('stroke', 'Black');
      
      if ((x.bandwidth() * 2) > 60)     
      {
        chart.append('svg:g').attr('class', 'x-label').attr('transform', translate(xRule.start('content')+ (x.bandwidth() / 2), yRule.middle('labels0')))
          .enterData(pivot.rows, 'text', 'x-label')
            .attr('x', function (d) { return x(d.rowValue); })
            .attr('y', function (d, i) { return yRule.middle('labels' + (i % 2)) - yRule.middle('labels0'); })
            .attr('dominant-baseline', 'middle')
            .attr('text-anchor', 'middle')
            .text(function (r) { return r.rowValue.niceToString(); })
            .each(function (v) { ellipsis(this, x.bandwidth() * 2); });
      }
      
      chart.append('svg:g').attr('class', 'x-title').attr('transform', translate(xRule.middle('content'), yRule.middle('title')))
        .append('svg:text').attr('class', 'x-title')
        .attr('text-anchor', 'middle')
        .attr('dominant-baseline', 'middle')
        .text(data.columns.c0.title);
    
      var yTicks = y.ticks(10);  
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
    
      var formatter = pStack == "expand" ? d3.format(".0%") : 
        		      pStack == "zero" ? d3.format("") : 
                      function(n) { return d3.format("")(n) + "?"};
                        
      chart.append('svg:g').attr('class', 'y-label').attr('transform',  translate(xRule.end('labels'), yRule.end('content')))
        .enterData(yTicks, 'text', 'y-label')
        .attr('y', function (t) { return -y(t); })
        .attr('dominant-baseline', 'middle')
        .attr('text-anchor', 'end')
        .text(formatter);
       
      
      chart.append('svg:g').attr('class', 'y-label').attr('transform', translate(xRule.middle('title'), yRule.middle('content')) + rotate(270))
        .append('svg:text').attr('class', 'y-label')
          .attr('text-anchor', 'middle')
          .attr('dominant-baseline', 'middle')
          .text(pivot.title);
      
      var color = d3.scaleOrdinal(ChartUtils.getColorScheme(data.parameters["ColorScheme"], data.parameters["ColorSchemeSteps"])).domain(pivot.columns.map(function (s) { return s.key; }));
      
      var pInterpolate = data.parameters["Interpolate"];
      
      var area = d3.area()
        .x(function(v) { return x(v.data.rowValue); })
        .y0(function(v) { return -y(v[0]); })
        .y1(function(v) { return -y(v[1]); })
        .curve(ChartUtils.getCurveByName(pInterpolate)); 
      
      //paint graph
      chart.enterData(stackedSeries, 'g', 'shape-serie').attr('transform', translate(xRule.start('content') + x.bandwidth() / 2, yRule.end('content')))
        .append('svg:path').attr('class', 'shape')
          .attr('stroke', function (s) { return s.color || color(s.key); })
          .attr('fill', function (s) { return s.color || color(s.key); })
          .attr('shape-rendering', 'initial')
          .attr('d', function (s) { return area(s);})
          .append('svg:title')
          .text(function (s) { return s.niceName; })
          
      var rectRadious = 2; 
      
      //paint graph - hover area trigger
      chart.enterData(stackedSeries, 'g', 'hover-trigger-serie').attr('transform', translate(xRule.start('content') + x.bandwidth() / 2, yRule.end('content')))
       .each(function(s){    
        
         d3.select(this).enterData(s, 'rect', 'point')
          .filter(function(v) {return v.data.values[s.key] != undefined;})
          .attr('x', function(v) { return x(v.data.rowValue) - rectRadious; })
          .attr('y', function(v) { return -y(v[1]); })
          .attr('width', 2 * rectRadious)
          .attr('height', function(v) { return y(v[1])- y(v[0]); } )
          .attr('fill', '#fff')
          .attr('fill-opacity', .1)
          .attr('stroke', 'none')
          .attr('data-click', function(v) { return getClickKeys(v.data.values[s.key].rowClick, data.columns); })
          .append('svg:title')
          .text(function(v) { return v.data.values[s.key].valueTitle; });
        
         if (x.bandwidth() > 15 && data.parameters["NumberOpacity"] > 0 )
          {
            d3.select(this).enterData(s, 'text', 'number-label')
            .filter(function(v) {return v.data.values[s.key] != undefined && (y(v[1]) - y(v[0])) > 10;})
            .attr('x', function (v) { return x(v.data.rowValue) })
            .attr('y', function (v) { return -y(v[1])*0.5 -y(v[0])*0.5; })
            .attr('fill', data.parameters["NumberColor"])
            .attr('dominant-baseline', 'central')
            .attr('opacity', data.parameters["NumberOpacity"])
            .attr('text-anchor', 'middle')
            .attr('font-weight', 'bold')
            .text(function (v) { return v.data.values[s.key].value; })     
            .attr('data-click', function(v) { return getClickKeys(v.data.values[s.key].rowClick, data.columns); })
          	.append('svg:title')
          	.text(function(v) { return v.data.values[s.key].valueTitle; });
            
          }
      });
      
      
      var legendScale = d3.scaleBand()
          .domain(pivot.columns.map(function (s, i) { return i; }))
          .range([0, xRule.size('content')]);
      
      if (legendScale.bandwidth() > 50) {
        
            var legendMargin = yRule.size('legend') + 4;
            
            chart.append('svg:g').attr('class', 'color-legend').attr('transform', translate(xRule.start('content'), yRule.start('legend')))
              .enterData(pivot.columns, 'rect', 'color-rect')
              .attr('x', function (e, i) { return legendScale(i); })
              .attr('width', yRule.size('legend'))
              .attr('height', yRule.size('legend'))
              .attr('fill', function (s) { return s.color || color(s.key); });
            
            chart.append('svg:g').attr('class', 'color-legend').attr('transform',  translate(xRule.start('content') + legendMargin, yRule.middle('legend') + 1))
              .enterData(pivot.columns, 'text', 'color-text')
                .attr('x', function (e, i) { return legendScale(i); })
                .attr('dominant-baseline', 'middle')
                .text(function (s) { return s.niceName; })
                .each(function (s) { ellipsis(this, legendScale.bandwidth() - legendMargin); });
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
