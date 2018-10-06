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
        labels : parseInt(data.parameters["LabelMargin"]),
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
        labels: 10,
        _4 : 10,
        title: 15,
        _5 : 5,
      }, height);
      //yRule.debugY(chart);
    
      var allValues = pivot.rows.flatMap(function(r){ 
        return pivot.columns.map(function(c){ return r.values[c.key] && r.values[c.key].value; }); 
      });
      
      
      var x = scaleFor(data.columns.c2, allValues, 0, xRule.size('content'), data.parameters["Scale"]);
      
      var y = d3.scaleBand()
          .domain(pivot.rows.map(function (v) { return v.rowValue; }))
          .range([0,yRule.size('content')]);
      
      var xTicks = x.ticks(width/50);
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
      	.text(pivot.title);
     
      chart.append('svg:g').attr('class', 'y-tick').attr('transform', translate(xRule.start('ticks'), yRule.start('content')  + (y.bandwidth() / 2)))
        .enterData(pivot.rows, 'line', 'y-tick')
        .attr('x2', xRule.size('ticks'))
        .attr('y1', function (v) { return y(v.rowValue); })
        .attr('y2', function (v) { return y(v.rowValue); })
        .style('stroke', 'Black');
      
       if (y.bandwidth() > 15 && pivot.columns.length > 0) {
      
          chart.append('svg:g').attr('class', 'y-label').attr('transform', translate(xRule.end('labels'), yRule.start('content')  + (y.bandwidth() / 2)))
            .enterData(pivot.rows, 'text', 'y-label')
            .attr('y', function (v) { return y(v.rowValue); })
            .attr('dominant-baseline', 'middle')
            .attr('text-anchor', 'end')
            .text(function (v) { return v.rowValue.niceToString(); })
            .each(function (v) { ellipsis(this, xRule.size('labels')); });
       }
      
      chart.append('svg:g').attr('class', 'y-label').attr('transform', translate(xRule.middle('title'), yRule.middle('content')) + rotate(270))
        .append('svg:text').attr('class', 'y-label')
        .attr('text-anchor', 'middle')
        .attr('dominant-baseline', 'middle')
        .text(data.columns.c0.title);
      
      var interMagin = 2;
      
      var color = d3.scaleOrdinal(ChartUtils.getColorScheme(data.parameters["ColorScheme"], data.parameters["ColorSchemeSteps"])).domain(pivot.columns.map(function (s) { return s.key; }));
      
      var ySubscale = d3.scaleBand()
        .domain(pivot.columns.map(function (s) { return s.key; }))
        .range([interMagin, y.bandwidth()-interMagin]);
      
      //PAINT GRAPH
      chart.enterData(pivot.columns, 'g', 'shape-serie').attr('transform', translate(xRule.start('content'), yRule.start('content')))
        .each(function(s){
          
          d3.select(this).enterData(pivot.rows, 'rect', 'shape')
          .filter(function (r){return r.values[s.key] != undefined;})
          .attr('stroke', ySubscale.bandwidth() > 4 ? '#fff' : null)
          .attr('fill', function (r) { return s.color || color(s.key); })
          .attr('y', function (r) { return ySubscale(s.key); })
          .attr('transform', function (r) { return 'translate(0, ' + y(r.rowValue) + ')'; })
          .attr('height', ySubscale.bandwidth())
          .attr('width', function (r) { return x(r.values[s.key] && r.values[s.key].value); })
          .attr('data-click', function (v) { return getClickKeys(v.values[s.key].rowClick, data.columns); })
          .append('svg:title')
          .text(function(v) { return v.values[s.key].valueTitle; });
          
        
          if (ySubscale.bandwidth() > 15 && data.parameters["NumberOpacity"] > 0)
          {
            d3.select(this).enterData(pivot.rows, 'text', 'number-label')
            .filter(function(r) {return r.values[s.key] != undefined && x(r.values[s.key] && r.values[s.key].value) > 16;})
            .attr('y', function (r) { return ySubscale(s.key) + ySubscale.bandwidth()/2; })
            .attr('x', function (r) { return x(r.values[s.key] && r.values[s.key].value) / 2; })
            .attr('transform', function (r) { return 'translate(0, ' + y(r.rowValue) + ')'; })
            .attr('opacity', data.parameters["NumberOpacity"])
            .attr('fill', data.parameters["NumberColor"])
            .attr('dominant-baseline', 'central')
            .attr('text-anchor', 'middle')
            .attr('font-weight', 'bold')
            .text(function(v) { return v.values[s.key].value; })
            .attr('data-click', function (v) { return getClickKeys(v.values[s.key].rowClick, data.columns); })
            .append('svg:title')
          	.text(function(v) { return v.values[s.key].valueTitle; });
          }
    	});
          
        //paint color legend
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
