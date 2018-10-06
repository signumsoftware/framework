import * as React from 'react'
import * as D3 from 'd3'
import D3ChartScriptRendererBase from '../ChartRenderer';
import * as ChartClient from '../ChartClient';
import * as ChartUtils from '../Templates/ChartUtils';
import { getClickKeys, translate, scale, rotate, skewX, skewY, matrix, scaleFor, rule, ellipsis } from '../Templates/ChartUtils';


export default class BarsChartScriptRendererBase extends D3ChartScriptRendererBase {

    drawChart(data: ChartClient.ChartTable, chart: D3.Selection<SVGElement, {}, HTMLDivElement, unknown >) {
              
      var yRule = rule({
        _1 : 5,
        title : 15,
        _2 : 5,
        max : 12,
        _3: 4,
        content: '*',
        _4: 4,
        min: 12,
        _5 : 5,
      }, height);
      //yRule.debugY(chart);
    
    
     var colorInterpolate = data.parameters["ColorInterpolate"];
     var colorInterpolation = ChartUtils.getColorInterpolation(colorInterpolate); 
      
     var cords = d3.entries(data.columns)
        .filter(function(p, i){return p.key != "c0" && p.key != "entity" && p.value.token != undefined;})
        .map(function(p, i){
          var values = data.rows.map(function(r){return r[p.key];});
          
          var scaleType = data.parameters["Scale" + p.key[1]];
          
          p.value.scale = scaleFor(p.value, values, 0, yRule.size('content'),  scaleType);
          
          var scaleFunc = scaleFor(p.value, values, 0, 1, scaleType);
          
          p.value.colorScale = function(r){return colorInterpolation(scaleFunc(r[p.key])) };
          
          return {
          key: p.key,
          column : p.value, 
          };
      });
     
    
      
       var x = d3.scaleBand()
           .domain(cords.map(function(c){return c.key;}))
          .rangeRound([0, width]);
       
     
      chart.append('svg:g').attr('class', 'x-tick').attr('transform', translate(0, yRule.start('content')))
        .enterData(cords, 'line', 'x-tick')
          .attr('y2',  function (d) { return yRule.size('content'); })
          .attr('x1', function (d) { return x(d.key); })
          .attr('x2', function (d) { return x(d.key); })
          .style('stroke', 'Black');
      
    
      chart.append('svg:g').attr('class', 'x-label').attr('transform', translate(0, yRule.middle('title')))
        .enterData(cords, 'text', 'x-label')
          .attr('x', function (d) { return x(d.key); })
          .attr('dominant-baseline', 'middle')
          .attr('text-anchor', 'middle')
          .attr("font-weight","bold")
          .text(function (d) { return d.column.title; });
      
       chart.append('svg:g').attr('class', 'x-label-max').attr('transform', translate(0, yRule.middle('max')))
        .enterData(cords, 'text', 'x-label-max')
          .attr('x', function (d) { return x(d.key); })
          .attr('dominant-baseline', 'middle')
          .attr('text-anchor', 'middle')
          .text(function (d) { return d.column.type != "Date" && d.column.type != "DateTime" ?
            d.column.scale.domain()[1]:        
            d3.max(data.rows, function(r){return r[d.key];}).niceToString(); });
      
      
      chart.append('svg:g').attr('class', 'x-label-min').attr('transform', translate(0, yRule.middle('min')))
        .enterData(cords, 'text', 'x-label-min')
          .attr('x', function (d) { return x(d.key); })
          .attr('dominant-baseline', 'middle')
          .attr('text-anchor', 'middle')
          .text(function (d) { return d.column.type != "Date" && d.column.type != "DateTime" ?
            d.column.scale.domain()[0]:  
            d3.min(data.rows, function(r){return r[d.key];}).niceToString(); });
      
      
      var drawGradient = function(key)
      {    
         chart.selectAll("g.x-tick-box .x-tick-box")
           .style('fill', function(d){ return d.key != key ? '#ccc': '#000'; } )
    
          chart.selectAll("g.shape-serie .shape")
            .attr("stroke", data.columns[key].colorScale);
      };
      
       var line = d3.line()
        .x(function(r) { return x(r.key); })
        .y(function(r) { return  -data.columns[r.key].scale(r.value); })
        .defined(function(r){return r.value != undefined;})
        .curve(ChartUtils.getCurveByName(data.parameters["Interpolate"]));//"linear"
      
      //paint graph - line
      chart.enterData(data.rows, 'g', 'shape-serie').attr('transform', translate(0, yRule.end('content')))
        .append('svg:path').attr('class', 'shape')
          .attr('fill', 'none')
          .attr('stroke-width', 1)
          .attr('stroke', 'black')
          .attr('shape-rendering', 'initial')
          .attr('data-click', function(r) {  return getClickKeys( r, data.columns);})
        .attr('d', function(r){ return line(cords.map(function(c){return {key : c.key, value : r[c.key]};}));})
        .append("title")
        	.text(function(r) {         
              return r.c0.niceToString() + "\n" +
                cords.map(function(c){return c.column.title + ": " + r[c.key].niceToString();}).join("\n");
            });
    
      var boxWidth = 10;
      chart.append('svg:g').attr('class', 'x-tick-box').attr('transform', translate(0, yRule.start('content')))
        .enterData(cords, 'rect', 'x-tick-box')
          .attr('height',  function (d) { return yRule.size('content'); })
          .attr('width', boxWidth)
          .attr('x', function (d) { return x(d.key) - boxWidth/2; })
          .style('stroke', '#ccc')
          .style('fill', '#ccc')
          .style('fill-opacity', '.2')
        .on("click", function(d){ drawGradient(d.key);});
    
      drawGradient("c1");  
    }
}
