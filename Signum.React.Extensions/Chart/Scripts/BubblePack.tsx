import * as React from 'react'
import * as D3 from 'd3'
import D3ChartScriptRendererBase from '../ChartRenderer';
import * as ChartClient from '../ChartClient';
import * as ChartUtils from '../Templates/ChartUtils';
import { getClickKeys, translate, scale, rotate, skewX, skewY, matrix, scaleFor, rule, ellipsis } from '../Templates/ChartUtils';


export default class BarsChartScriptRendererBase extends D3ChartScriptRendererBase {

    drawChart(data: ChartClient.ChartTable, chart: D3.Selection<SVGElement, {}, HTMLDivElement, unknown >) {
            
      if(width == 0 || height == 0)
        return;
      
      var color = null;
      if(data.columns.c3.token != null)
      {
        var scaleFunc = scaleFor(data.columns.c3, data.rows.map(function(r){return r.c3;}), 0, 1, data.parameters["ColorScale"]);
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
      
      var format = d3.format(",d");
      
      var root = ChartUtils.stratifyTokens(data, "c0", "c2");
      
      root.sum(function(d) { return d.value; })
      .sort(function(a, b) { return b.value - a.value; })
      
      var size = scaleFor(data.columns.c1, data.rows.map(function(r){return r.c1;}), 0, 1, data.parameters["Scale"]);
      
      root.sum(function(r){
          return r == null ? 0: size(r.c1);
      });
    
      var bubble = d3.pack()
         .size([width, height])
         .padding(2);
     
      bubble(root);
      
      var nodes = root.descendants().filter(function(d){return d.data.isRoot == null;});
        
      var node = chart.selectAll("g.node")
          .data(nodes)
        .enter().append("g")
          .attr("class", "node")
          .attr("transform", function(d) { return "translate(" + d.x + "," + d.y + ")"; })
    	  .attr('data-click', function(p) { return getClickKeys(p, data.columns); });
      
      node.append("circle")
          .attr('shape-rendering', 'initial')
          .attr("r", function(d) { return d.r; })
          .style("fill", function(d) { return d.data.folder ? folderColor(d.data.folder): color(d.data); })
          .style("fill-opacity", function(d) { return data.parameters["FillOpacity"]; })
          .style("stroke", function(d) { return data.parameters["StrokeColor"] || (d.data.folder ? folderColor(d.data.folder): color(d.data)); })
          .style("stroke-width", data.parameters["StrokeWidth"])
          .style("stroke-opacity", 1)
          .attr('data-click', function(p) { return p.data.folder ? getClickKeys({ c2 : p.data.folder }, data.columns): getClickKeys(p.data, data.columns); });
    
      var showNumber = data.parameters["NumberOpacity"] > 0;
      var numberSizeLimit = data.parameters["NumberSizeLimit"];
      
      node.filter(function(d){return !d.data.folder;}).append("text")
          .attr('dominant-baseline', 'central')
          .attr('text-anchor', 'middle')
      	  .attr("dy",function(d) { return  showNumber &&  d.r > numberSizeLimit ? "-0.5em" : null;})
          .text(function(d) { return d.data.folder ? d.data.folder.niceToString(): d.data.c0 ? (d.data.c0.niceToString()) : undefined; })
          .attr('data-click', function(p) { return p.data.folder ? getClickKeys({ c2 : p.data.folder }, data.columns) : getClickKeys(p.data, data.columns); })
          .each(function(d) { return ellipsis(this, d.r * 2, 1, ""); });
      
      if(showNumber)
      {
        node.filter(function(d) { return d.r > numberSizeLimit; })
        	.append("text")
            .attr('fill',  data.parameters["NumberColor"] )
            .attr('dominant-baseline', 'central')
            .attr('text-anchor', 'middle')
            .attr('font-weight', 'bold')
        	.attr('opacity', function(d) { return data.parameters["NumberOpacity"] * d.r / 30; })
            .attr("dy", ".5em")
            .attr('data-click', function(p) { return p.data.folder ? getClickKeys({ c2 : p.data.folder }, data.columns) : getClickKeys(p.data, data.columns); })
            .text(function(d) { return d.data.c1; });
      }
      
      node.append('svg:title')
      	  .text(function(d) { 
        		var key = (d.data.folder ? d.data.folder.niceToString() : 
        			(d.data.c0.niceToString() + (data.columns.c2.token == null ? '' : (' (' + (d.data.c2 ? d.data.c2.niceToString() : null) + ')'))));
         
        
        		var value = (d.data.folder ? format(size.invert(d.value)) : 
                             (d.data.c1 + (data.columns.c3.token == null ? '' : (' (' + d.data.c3 + ')'))));
        
        		return key + ': '  + value;
            });
    }
}
