import * as React from 'react'
import * as d3 from 'd3'
import D3ChartBase from './D3ChartBase';
import * as ChartClient from '../ChartClient';
import * as ChartUtils from '../Templates/ChartUtils';
import { translate, scale, rotate, skewX, skewY, matrix, scaleFor, rule, ellipsis } from '../Templates/ChartUtils';

interface ForceGraphNode extends d3.SimulationNodeDatum {
  col: ChartClient.ChartColumn<unknown>;
  val: unknown;
}

export default class ForceGraphChart extends D3ChartBase {

  drawChart(data: ChartClient.ChartTable, chart: d3.Selection<SVGElement, {}, null, undefined>, width: number, height: number) {

    var fromColumn = data.columns.c0! as ChartClient.ChartColumn<unknown>;
    var toColumn = data.columns.c1! as ChartClient.ChartColumn<unknown>;
    var linkWidthColumn = data.columns.c2 as ChartClient.ChartColumn<number> | undefined;

    var charge = parseInt(data.parameters["Charge"] || "150");
    var linkDistance = parseInt(data.parameters["LinkDistance"] || "10");

    var size = linkWidthColumn == null ? null : scaleFor(linkWidthColumn, data.rows.map(linkWidthColumn.getValue), 1, parseFloat(data.parameters["MaxWidth"]), "ZeroMax");

    chart.append("defs")
      .append("marker")
      .attr("id", "arrow")
      .attr("viewBox", "0 -5 5 10")
      .attr("refX", "10")
      .attr("refY", "0")
      .attr("markerWidth", "10")
      .attr("markerHeight", "6")
      .attr("orient", "auto")
      .style("fill", "#ccc")
      .append("path")
      .attr("d", "M0,-2L10,0L0,2");


    var nodes = data.rows.map(r => ({ col: fromColumn, val: fromColumn.getValue(r) }) as ForceGraphNode)
      .concat(data.rows.map(r => ({ col: toColumn, val: toColumn.getValue(r) }) as ForceGraphNode))
      .filter(p => p.val != undefined)
      .distinctBy(p => p.col.getKey(p.val));

    var nodeKeys = nodes.toObjectDistinct(p => p.col.getKey(p.val));

    var links = data.rows
      .filter(r =>
        fromColumn.getValue(r) != null &&
        toColumn.getValue(r) != null
      )
      .map(r => ({
        source: nodeKeys[fromColumn.getValueKey(r)],
        target: nodeKeys[toColumn.getValueKey(r)],
        value: linkWidthColumn && linkWidthColumn.getValue(r),
        fromValue: fromColumn.getValue(r),
        toValue: toColumn.getValue(r),
      }));

    var color = d3.scaleOrdinal(ChartUtils.getColorScheme(data.parameters["ColorCategory"], parseInt(data.parameters["ColorCategorySteps"])))

    var simulation = d3.forceSimulation()
      .force("link", d3.forceLink(links))
      .force("charge", d3.forceManyBody())
      .force("center", d3.forceCenter(width / 2, height / 2));

    simulation
      .nodes(nodes)
      .on("tick", ticked);

    var link = chart.selectAll("line.link")
      .data(links)
      .enter().append("line")
      .attr('shape-rendering', 'initial')
      .attr("class", "link")
      .style("stroke", "#ccc")
      .attr("marker-end", "url(#arrow)")
      .style("stroke-width", size == null ? lk => 2 : lk => size!(lk.value!));

    link.append("title")
      .text(lk =>
        fromColumn.getNiceName(lk.fromValue) +
        (linkWidthColumn == null ? " -> " : (" -(" + linkWidthColumn.getNiceName(lk.value!) + ")-> ")) +
        toColumn.getNiceName(lk.toValue)
      );


    const drag = d3.drag<SVGCircleElement, ForceGraphNode>()
      .on("start", d => {
        if (!d3.event.active)
          simulation.alphaTarget(0.3).restart();

        d.fx = d.x;
        d.fy = d.y;
      })
      .on("drag", d => {
        d.fx = d3.event.x;
        d.fy = d3.event.y;
      })
      .on("end", d => {
        simulation.alphaTarget(0);
      });

    var node = chart.selectAll("circle.node")
      .data(nodes)
      .enter().append<SVGCircleElement>("circle")
      .attr('shape-rendering', 'initial')
      .attr("class", "node")
      .attr("r", 5)
      .style("fill", d => d.col.getColor(d.val) || color(d.col.getKey(d.val)))
      .on("dblclick", d => {
        d.fx = null;
        d.fy = null;
        simulation.alpha(0.3).restart();
      })
      .call(drag);


    node.append("title")
      .text(d => d.col.getNiceName(d.val));

    function ticked() {
      link.attr("x1", d => d.source.x!)
        .attr("y1", d => d.source.y!)
        .attr("x2", d => d.target.x!)
        .attr("y2", d => d.target.y!);

      node.attr("cx", d => d.x!)
        .attr("cy", d => d.y!);
    }
  }
}
