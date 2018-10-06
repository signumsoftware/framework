import * as React from 'react'
import * as D3 from 'd3'
import D3ChartScriptRendererBase from '../ChartRenderer';
import * as ChartClient from '../ChartClient';
import * as ChartUtils from '../Templates/ChartUtils';
import { getClickKeys, translate, scale, rotate, skewX, skewY, matrix, scaleFor, rule, ellipsis } from '../Templates/ChartUtils';


export default class BarsChartScriptRendererBase extends D3ChartScriptRendererBase {

    drawChart(data: ChartClient.ChartTable, chart: D3.Selection<SVGElement, {}, HTMLDivElement, unknown >) {
            
      var pInnerRadius = data.parameters.InnerRadious;
      var pSort = data.parameters.Sort;
    
      var size = d3.scaleLinear()
          .domain([0, d3.max(data.rows,function(r){return r.c1;})])
          .range([0, 1]);
    
      var r = d3.min([width / 2, height]) / 3;
      var rInner = r  * parseFloat(pInnerRadius);
      var color = d3.scaleOrdinal(ChartUtils.getColorScheme(data.parameters["ColorScheme"], data.parameters["ColorSchemeSteps"])).domain( data.rows.map(function (v) { return v.c0; }));
      
      var pie = d3.pie()
          .sort(pSort == "Ascending"? function(a, b) {return d3.descending(size(a.c1), size(b.c1)) }  :
                pSort == "Descending"? function(a, b) {return d3.ascending(size(a.c1), size(b.c1)) }: null )
          .value(function (v) { return size(v.c1); });
        
       var arc = d3.arc()     
           .outerRadius(r).innerRadius(rInner);
      
      chart.append('svg:g').attr('class', 'shape').attr('transform', translate(width / 2, height / 2))
        .enterData(pie(data.rows), 'g', 'slice')
          .append('svg:path').attr('class', 'shape')
          .attr('d', arc)
          .attr('fill', function (slice) { return slice.data.c0.color || color(slice.data.c0); })
          .attr('shape-rendering', 'initial')
          .attr('data-click', function (slice) { return getClickKeys(slice.data, data.columns); })
          .append('svg:title')
          .text(function (slice) { return slice.data.c0.niceToString() + ': ' + slice.data.c1.niceToString(); });
      
      //paint color legend
      var cx = (width / 2),
          cy = (height / 2),
          legendRadius = 1.2;
      chart.append('svg:g').data([data.rows]).attr('class', 'color-legend').attr('transform', translate(cx, cy))
        .enterData(pie, 'g', 'color-legend')
        .append('svg:text').attr('class', 'color-legend sf-chart-strong')
        .attr('x', function (slice) { var m = (slice.endAngle + slice.startAngle) / 2; return Math.sin(m) * r * legendRadius; })
        .attr('y', function (slice) { var m = (slice.endAngle + slice.startAngle) / 2; return -Math.cos(m) * r * legendRadius; })
        .attr('text-anchor', function (slice) { 
          var m = (slice.endAngle + slice.startAngle) / 2; 
          var cuadr = Math.floor(12 * m / (2 * Math.PI)); 
          return (1 <= cuadr && cuadr <= 4) ? 'start' : (7 <= cuadr && cuadr <= 10) ? 'end' : 'middle'; })
        .attr('fill', function (slice) { return  slice.data.c0.color || color(slice.data.c0); })
        .attr('data-click', function (slice) { return getClickKeys(slice.data, data.columns); })
      .text(function (slice) { return ((slice.endAngle - slice.startAngle) >= (Math.PI / 16)) ? slice.data.c0.niceToString() : ''; });
      
    }
    }
}
