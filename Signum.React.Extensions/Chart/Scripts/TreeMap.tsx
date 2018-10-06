import * as React from 'react'
import * as D3 from 'd3'
import D3ChartScriptRendererBase from '../ChartRenderer';
import * as ChartClient from '../ChartClient';
import * as ChartUtils from '../Templates/ChartUtils';
import { getClickKeys, translate, scale, rotate, skewX, skewY, matrix, scaleFor, rule, ellipsis } from '../Templates/ChartUtils';


export default class BarsChartScriptRendererBase extends D3ChartScriptRendererBase {

    drawChart(data: ChartClient.ChartTable, chart: D3.Selection<SVGElement, {}, HTMLDivElement, unknown >) {
              
      var color = null;
      if(data.columns.c3.token != null)
      {
        var scaleFunc = scaleFor(data.columns.c3, data.rows.map(function(r){ return r.c3; }), 0, 1, data.parameters["ColorScale"]);
        var colorInterpolator = ChartUtils.getColorInterpolation(data.parameters["ColorInterpolate"]);
        
        
        color = function(v){return colorInterpolator(scaleFunc(v.c3)); }
      }
      else if(data.columns.c4.token != null) 
      {
        var scheme = ChartUtils.getColorScheme(data.parameters["ColorScheme"], data.parameters["ColorSchemeSteps"]);
        var categoryColor = d3.scaleOrdinal(scheme).domain(data.rows.map(function(v) { return v.c4; }));
        color = function(v) { return v.c4.color || categoryColor(v.c4); };
      }
      else
      { 
        var scheme = ChartUtils.getColorScheme(data.parameters["ColorScheme"], data.parameters["ColorSchemeSteps"]);
        var categoryColor =  d3.scaleOrdinal(scheme).domain(data.rows.map(function(v) { return v.c0; }));
        color = function(v) { return v.c0.color || categoryColor(v.c0); };
      }
      
      
      var folderColor = null;
      if(!data.columns.c2.token != null){
        var scheme = ChartUtils.getColorScheme(data.parameters["ColorScheme"], data.parameters["ColorSchemeSteps"]);
        var categoryColor =  d3.scaleOrdinal(scheme).domain(data.rows.map(function(v) { return v.c2; }));
        folderColor = function(c2) { return  c2.color || categoryColor(c2); };
      }
      
      var root = ChartUtils.stratifyTokens(data, "c0", "c2");
      
      var size = scaleFor(data.columns.c1, data.rows.map(function(r){return r.c1;}), 0, 1, data.parameters["Scale"]);
      
      root.sum(function(r){
          return r == null ? 0: size(r.c1);
      });
      
      var opacity = data.columns.c2.token ?   parseFloat(data.parameters["Opacity"]) : 1;
      var padding = data.columns.c2.token ?   parseInt(data.parameters["Padding"]) : 1;
      var p2 = padding / 2;
      
      var bubble = d3.treemap()
      	.size([width, height])
        .round(true)
        .padding(padding);
      
      bubble(root);
      
      var nodes = root.descendants().filter(function(d){return !!d.data;});
      
      nodes.forEach(n => {
      	n.width = n.x1 - n.x0;
        n.height = n.y1 - n.y0;
      });
      
      var node = chart.selectAll("g.node")
        .data(nodes)
        .enter().append("g")
          .attr("class", "node")
          .attr("transform", function(d) { return translate(d.x0-p2,d.y0-p2); });
    
      node.filter(function(d) {return !!d.data.folder;}).append("rect")
          .attr('shape-rendering', 'initial')
          .attr("width", function(d) { return d.width; })
          .attr("height", function(d) { return d.height;})
          .style("fill", function(d) { return  d.data.folder.color || folderColor(d.data.folder); })    
          .attr('data-click', function(p) { return getClickKeys({c2: p.data.folder}, data.columns); })
          .append('svg:title')
          .text(function(d) { return  d.data.folder.niceToString();});
      
      node.filter(function(d) { return !!d.data.c1; }).append("rect")
          .attr('shape-rendering', 'initial')
          .attr("opacity",opacity)
          .attr("width", function(d) { return d.width; })
          .attr("height", function(d) { return d.height; })
          .style("fill", function(d) { return color(d.data); })
          .attr('data-click', function(p) { return getClickKeys(p.data, data.columns); })
          .append('svg:title')
          .text(function(d) { return  d.data.c0.niceToString() + ': ' + d.data.c1.niceToString();});
    
      var showNumber = data.parameters["NumberOpacity"] > 0;
      
      var nodeFilter = node.filter(function(d) {return !!d.data.c1 && d.width > 10 && d.height > 25;});
      
      nodeFilter.append("text")
          .attr("text-anchor", "middle")
          .attr('dominant-baseline', 'middle')
          .attr("dx",function(d) { return d.width/2; })
      	  .attr("dy",function(d) { return d.height/2 + (showNumber ? -6: 0); })
          .attr('data-click', function(p) { return getClickKeys(p.data, data.columns); })
          .text(function(d) { return d.data.c0.niceToString(); })
          .each(function (d) { ellipsis(this, d.width, 4, ""); })
          .append('svg:title')
          .text(function(d) { return  d.data.c0.niceToString() + ': ' + d.data.c1.niceToString();});
      
      if(showNumber)
      {
        nodeFilter.append("text")
            .attr('fill', data.parameters["NumberColor"])
        	.attr('opacity', ".5")
            .attr('dominant-baseline', 'central')
            .attr('opacity', data.parameters["NumberOpacity"])
            .attr('text-anchor', 'middle')
            .attr('font-weight', 'bold')
            .attr("dx",function(d) { return d.width / 2; })
            .attr("dy",function(d) { return d.height / 2 + 6; })
            .attr('data-click', function(p) { return getClickKeys(p.data, data.columns); })
            .text(function(d) { return d.data.c1; })
            .each(function(d) { return ellipsis(this, d.r * 2, 1, ""); });
      }
      
    }
}
