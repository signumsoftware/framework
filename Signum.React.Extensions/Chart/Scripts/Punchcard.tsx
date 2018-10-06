import * as React from 'react'
import * as D3 from 'd3'
import D3ChartScriptRendererBase from '../ChartRenderer';
import * as ChartClient from '../ChartClient';
import * as ChartUtils from '../Templates/ChartUtils';
import { getClickKeys, translate, scale, rotate, skewX, skewY, matrix, scaleFor, rule, ellipsis } from '../Templates/ChartUtils';


export default class BarsChartScriptRendererBase extends D3ChartScriptRendererBase {

    drawChart(data: ChartClient.ChartTable, chart: D3.Selection<SVGElement, {}, HTMLDivElement, unknown >) {
                
      
      function chooseSort(collection, shortType){
        switch(shortType){
          case "AscendingToStr": return collection.orderBy(function(v){return v.k.toStr || "";});
          case "AscendingKey": return collection.orderBy(function(v){return v.k.key || "";});  
          case "AscendingSumOrder": return collection.orderBy(function(v){return v.sum;});  
          case "DescendingToStr": return collection.orderByDescending(function(v){return v.k.toStr || "";});
          case "DescendingKey": return collection.orderByDescending(function(v){return v.k.key || "";});  
          case "DescendingSumOrder": return collection.orderByDescending(function(v){return v.sum;});  
          case "None":  return collection;
        }
      }
      
      function getSum(g){
         return g.elements.reduce(function(acum, val) { return acum + (val.c6 || 0) }, 0);
      }
      
      var dim0 = data.rows
         .groupBy(function(r){return "k" + r.c0.key; })
        .map(function(g){ return { k: g.elements[0].c0, sum: getSum(g) };});
    
     var dim1 = data.rows
         .groupBy(function(r){return "k" + r.c1.key; })
         .map(function(g){ return { k: g.elements[0].c1, sum: getSum(g) };});
     
      dim0 = chooseSort(dim0, data.parameters["XSort"]).map(function(a) { return a.k; });
      dim1 = chooseSort(dim1, data.parameters["YSort"]).map(function(a) { return a.k; });
    
      var xRule = rule({
        _1 : 5,
        title : 15,
        _2 : 10, 
        labels : parseInt(data.parameters["XMargin"]),
        _3 : 5,
        ticks: 4,
        content: '*',
        _4: 10,
      }, width);
      //xRule.debugX(chart)
      
      var yRule = rule({
        _1 : 5,
        content: '*',
        ticks: 4,
        _2 : 5,
        labels0: 15,
        labels1: 15,
        _3 : 10,
        title: 15,
        _4 : 5,
      }, height);
      //yRule.debugY(chart);
      
      var x = d3.scaleBand()
          .domain(dim0)
          .range([0, xRule.size('content')]);
      
      var y = d3.scaleBand()
          .domain(dim1)
          .range([0, yRule.size('content')]);
      
     debugger;
      
      var color = null;
      if(data.columns.c3.token != null)
      {
        var scaleFunc = scaleFor(data.columns.c3, data.rows.map(function(r){return r.c3;}), 0, 1, data.parameters["ColorScale"]);
        var colorInterpolator = ChartUtils.getColorInterpolation(data.parameters["ColorInterpolate"]);
        color = function(v){return colorInterpolator(scaleFunc(v)); }
      }
      
      var opacity = null;
      if(data.columns.c4.token != null)
      {
       	
        opacity = scaleFor(data.columns.c4, data.rows.map(function(r){return r.c4;}), 0, 1, data.parameters["OpacityScale"]);
      }
      
      var shape = data.parameters["Shape"];
      var innerSize = null
      if(data.columns.c5.token != null)
      {
      	innerSize = scaleFor(data.columns.c5, data.rows.map(function(r){return r.c5;}), 0, 100, data.parameters["OpacityScale"]);
      }
      
      if(data.parameters["VerticalLineColor"]){
      chart.append('svg:g').attr('class', 'x-line').attr('transform',  translate(xRule.start('content') + x.bandwidth() / (shape == "ProgressBar" ? 1 : 2), yRule.start('content')))
        .enterData(dim0, 'line', 'y-line')
         .attr('y2',  yRule.size('content'))
          .attr('x1', function (d) { return x(d); })
          .attr('x2', function (d) { return x(d); })
        .style('stroke', data.parameters["VerticalLineColor"]);
      }
      
      chart.append('svg:g').attr('class', 'x-tick').attr('transform', translate(xRule.start('content') + x.bandwidth() / (shape == "ProgressBar" ? 1 : 2), yRule.start('ticks')))
        .enterData(dim0, 'line', 'x-tick')
          .attr('y2',  yRule.size('ticks'))
          .attr('x1', function (d) { return x(d); })
          .attr('x2', function (d) { return x(d); })
          .style('stroke', 'Black');
      
      if ((x.bandwidth() * 2) > 60)     
      {
        chart.append('svg:g').attr('class', 'x-label').attr('transform', translate(xRule.start('content')+ (x.bandwidth() / 2), yRule.middle('labels0')))
          .enterData(dim0, 'text', 'x-label')
            .attr('x', function (d) { return x(d); })
            .attr('y', function (d, i) { return yRule.middle('labels' + (i % 2)) - yRule.middle('labels0'); })
            .attr('dominant-baseline', 'middle')
            .attr('text-anchor', 'middle')
            .text(function (d) { return d.niceToString(); })
            .each(function (v) { ellipsis(this, x.bandwidth() * 1.7); });
      }
      
      chart.append('svg:g').attr('class', 'x-title').attr('transform', translate(xRule.middle('content'), yRule.middle('title')))
        .append('svg:text').attr('class', 'x-title')
        .attr('text-anchor', 'middle')
        .attr('dominant-baseline', 'middle')
        .text(data.columns.c0.title);
      
    
      if(data.parameters["HorizontalLineColor"]){
      chart.append('svg:g').attr('class', 'y-line').attr('transform', translate(xRule.start('content'), yRule.end('content') - y.bandwidth() / (shape == "ProgressBar" ? 1 : 2)))
        .enterData(dim1, 'line', 'y-line')
        .attr('x2', xRule.size('content'))
        .attr('y1', function (t) { return -y(t); })
        .attr('y2', function (t) { return -y(t); })
        .style('stroke', data.parameters["HorizontalLineColor"]);
      }
      
      chart.append('svg:g').attr('class', 'y-tick').attr('transform', translate(xRule.start('ticks'), yRule.end('content') - y.bandwidth() / 2))
        .enterData(dim1, 'line', 'y-tick')
        .attr('x2', xRule.size('ticks'))
        .attr('y1', function (t) { return -y(t); })
        .attr('y2', function (t) { return -y(t); })
        .style('stroke', 'Black');
      
       if (y.bandwidth() > 16)     
       {
          chart.append('svg:g').attr('class', 'y-label').attr('transform',  translate(xRule.end('labels'), yRule.end('content') - y.bandwidth() / 2))
            .enterData(dim1, 'text', 'y-label')
            .attr('y', function (t) { return -y(t); })
            .attr('dominant-baseline', 'middle')
            .attr('text-anchor', 'end')
            .text(function (d) { return d.niceToString(); })
            .each(function (v) { ellipsis(this, xRule.size('labels')); });  
    	}
     
      chart.append('svg:g').attr('class', 'y-label').attr('transform', translate(xRule.middle('title'), yRule.middle('content')) + rotate(270))
        .append('svg:text').attr('class', 'y-label')
        .attr('text-anchor', 'middle')
        .attr('dominant-baseline', 'middle')
        .text(data.columns.c1.title);
      
     
      var groups = chart.enterData(data.rows, 'g').attr('data-click', function(r) { return getClickKeys(r, data.columns); });
      
      
      function configureShape(column, rowValue, domain, extra)
      {
      
        var shapes = groups.append(shape == "Circle" ? 'circle': 'rect', 'punch').attr('transform', translate(xRule.start('content') + x.bandwidth() / 2, yRule.end('content') - y.bandwidth() / 2))
            .filter(function(r) {return r != undefined;})      
            .attr('fill-opacity', function(r) {return data.parameters["FillOpacity"]* (opacity != null ? opacity(r.c4) : 1);})
            .attr("shape-rendering","initial");
    
    
        if(shape == "Circle")
        {
           var circleSize = Math.min(x.bandwidth(), y.bandwidth()) * 0.45; 
           var area = column.token == null? function(){return circleSize * circleSize;} : scaleFor(column, domain, 0, circleSize * circleSize, data.parameters["SizeScale"]);
           extra.numberOpacity = function(r) { return area(r) / 500; };
    
           shapes.attr('cx', function(r, i) { return x(r.c0); })
            .attr('cy', function(r) { return -y(r.c1); })
            .attr('r', function(r) { return  Math.sqrt(area(rowValue(r))); });
    
        } else if(shape == "Rectangle") {
    
           var area = column.token == null? function(){return x.bandwidth() * y.bandwidth();} : scaleFor(column, domain, 0, x.bandwidth() * y.bandwidth(), data.parameters["SizeScale"]); 
           var ratio = x.bandwidth() / y.bandwidth();
           var recWidth = function(r) { return  Math.sqrt(area(rowValue(r)) * ratio); };
           var recHeight = function(r) { return Math.sqrt(area(rowValue(r)) / ratio); };
           extra.numberOpacity = function(r) { return area(r) / 500; };
    
          shapes.attr('x', function(r, i) { return x(r.c0) - recWidth(r) / 2; })
            .attr('y', function(r) { return -y(r.c1) - recHeight(r) / 2; })
            .attr('width', recWidth)
            .attr('height', recHeight);
    
        } else if(shape == "ProgressBar") {
    
           var recWidth = column.token == null? function(){return x.bandwidth();} : scaleFor(column, domain, 0, x.bandwidth(), data.parameters["SizeScale"]); 
           extra.numberOpacity = function(r) { return 1; };
    
          shapes.attr('x', function(r, i) { return x(r.c0) - x.bandwidth() / 2; })
            .attr('y', function(r) { return -y(r.c1) - y.bandwidth() / 2; })
            .attr('width', function(r) { return recWidth(rowValue(r));})
            .attr('height', y.bandwidth());    
        }
        
        return shapes;
     }
      
     var extra = {};
      configureShape(data.columns.c2, function(r) { return r.c2; }, data.rows.map(function(r) { return r.c2; }), extra)
       .attr('fill', function(r) { return color == null? (data.parameters["FillColor"] || 'black'):  color(r.c3); })
       .attr('stroke', function(r) { return data.parameters["StrokeColor"] || (color == null? 'black': color(r.c3)); })
       .attr('stroke-width',  data.parameters["StrokeWidth"])
       .attr('stroke-opacity', function(r){return(opacity != null ? opacity(r.c4) : 1); });
      
      
     var isRelative = data.parameters["InnerSizeType"] == "Relative"; 
     if(data.columns.c5.token != null)
     {  
       var fun = !isRelative ? function(r) { return r.c5; } :
       		data.columns.c2.token != null ?  function(r) { return r.c5 * r.c2; }:
            function(r)  { return r.c5; } ;
       var domain = !isRelative ? data.rows.map(function(r) { return r.c5; }) :
       		data.columns.c2.token != null ?  data.rows.map(function(r) { return r.c2; }):
       	    [1]
       
        configureShape(data.columns.c5, fun, domain, {})
          .attr('fill', data.parameters["InnerFillColor"] || 'black')
     }
      
     
      function percentage(v) { return Math.floor(v * 10000)/100 + "%";} 
      
      if(data.parameters["NumberOpacity"] > 0)
      { 
          groups.append('text').attr('class', 'punch').attr('transform', translate(xRule.start('content') + x.bandwidth() / 2, yRule.end('content') - y.bandwidth() / 2))
          .attr('x', function(r, i) { return x(r.c0); })
          .attr('y', function(r) { return -y(r.c1) })
          .attr('fill', data.parameters["NumberColor"])
          .attr('dominant-baseline', 'central')
          .attr('opacity', function(r){ return data.parameters["NumberOpacity"] * extra.numberOpacity(r.c2);})
          .attr('text-anchor', 'middle')
          .attr('font-weight', 'bold')
          .text(function (r) { 
            return data.columns.c2.token != null ? r.c2 : 
                   data.columns.c5.token != null ? (isRelative? percentage(r.c5) : r.c5) :
                   data.columns.c3.token != null ? r.c3 :
                   data.columns.c4.token != null ? r.c4 : null; });
     
      }
      
      groups.append('svg:title')
          .text(function(r) { return r.c0.niceToString() + ', ' + r.c1.niceToString() + 
            (data.columns.c2.token == null? "" : ("\n" +  data.columns.c2.title +": " + r.c2.niceToString())) +
            (data.columns.c3.token == null? "" : ("\n" +  data.columns.c3.title +": " + r.c3.niceToString())) +
            (data.columns.c4.token == null? "" : ("\n" +  data.columns.c4.title +": " + r.c4.niceToString())) +
            (data.columns.c5.token == null? "" : ("\n" +  data.columns.c5.title +": " + (isRelative? percentage(r.c5) : r.c5.niceToString())) +
            (data.columns.c6.token == null? "" : ("\n" +  data.columns.c6.title +": " + r.c6.niceToString()))); });
      
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
