import * as React from 'react'
import * as d3 from 'd3'
import D3ChartBase from './D3ChartBase';
import * as ChartClient from '../ChartClient';
import * as ChartUtils from '../Templates/ChartUtils';
import { translate, scale, rotate, skewX, skewY, matrix, scaleFor, rule, ellipsis, Folder, Root, isFolder } from '../Templates/ChartUtils';
import { ChartRow } from '../ChartClient';


export default class TreeMapChart extends D3ChartBase {

  drawChart(data: ChartClient.ChartTable, chart: d3.Selection<SVGElement, {}, null, undefined>, width: number, height: number) {

    var keyColumn = data.columns.c0!;
    var valueColumn = data.columns.c1! as ChartClient.ChartColumn<number>;
    var parentColumn = data.columns.c2;
    var colorScaleColumn = data.columns.c3 as ChartClient.ChartColumn<number> | undefined;
    var colorSchemeColumn = data.columns.c4;

    if (width == 0 || height == 0)
      return;

    var color: (v: ChartRow) => string | undefined;
    if (colorScaleColumn) {
      var scaleFunc = scaleFor(colorScaleColumn, data.rows.map(r => colorScaleColumn!.getValue(r)), 0, 1, data.parameters["ColorScale"]);
      var colorInterpolator = ChartUtils.getColorInterpolation(data.parameters["ColorInterpolate"]);
      color = r => colorInterpolator && colorInterpolator(scaleFunc(colorScaleColumn!.getValue(r)));
    }
    else if (colorSchemeColumn) {
      var scheme = ChartUtils.getColorScheme(data.parameters["ColorScheme"], parseInt(data.parameters["ColorSchemeSteps"]));
      var categoryColor = d3.scaleOrdinal(scheme).domain(data.rows.map(r => colorSchemeColumn!.getValueKey(r)));
      color = r => colorSchemeColumn!.getColor(r) || categoryColor(colorSchemeColumn!.getValueKey(r));
    }
    else {
      var scheme = ChartUtils.getColorScheme(data.parameters["ColorScheme"], parseInt(data.parameters["ColorSchemeSteps"]));
      var categoryColor = d3.scaleOrdinal(scheme).domain(data.rows.map(r => keyColumn.getValueKey(r)));
      color = r => keyColumn.getValueColor(r) || categoryColor(keyColumn.getValueKey(r));
    }

    var folderColor: null | ((folder: unknown) => string) = null;
    if (parentColumn) {
      var scheme = ChartUtils.getColorScheme(data.parameters["ColorScheme"], parseInt(data.parameters["ColorSchemeSteps"]));
      var categoryColor = d3.scaleOrdinal(scheme).domain(data.rows.map(r => parentColumn!.getValueKey(r)));
      folderColor = folder => parentColumn!.getColor(folder) || categoryColor(parentColumn!.getKey(folder));
    }

    var root = ChartUtils.stratifyTokens(data, keyColumn, parentColumn);

    var size = scaleFor(valueColumn, data.rows.map(r => valueColumn.getValue(r)), 0, 1, data.parameters["Scale"]);

    root.sum(r => r == null ? 0 : size(valueColumn.getValue(r as ChartRow)));

    var opacity = parentColumn ? parseFloat(data.parameters["Opacity"]) : 1;
    var padding = parentColumn ? parseInt(data.parameters["Padding"]) : 1;
    var p2 = padding / 2;

    var bubble = d3.treemap<ChartRow | Folder | Root>()
      .size([width, height])
      .round(true)
      .padding(padding);

    const treeMapRoot = bubble(root);

    var nodes = treeMapRoot.descendants().filter(d => !!d.data);

    const nodeHeight = (n: d3.HierarchyRectangularNode<any>) => n.y1 - n.y0;
    const nodeWidth = (n: d3.HierarchyRectangularNode<any>) => n.x1 - n.x0;

    var node = chart.selectAll("g.node")
      .data(nodes)
      .enter().append("g")
      .attr("class", "node")
      .attr("transform", d => translate(d.x0 - p2, d.y0 - p2));

    node.filter(d => isFolder(d.data)).append("rect")
      .attr('shape-rendering', 'initial')
      .attr("width", d => nodeWidth(d))
      .attr("height", d => nodeHeight(d))
      .style("fill", d => parentColumn!.getColor((d.data as Folder).folder) || folderColor!((d.data as Folder).folder))
      .on('click', p => this.props.onDrillDown({ c2: (p.data as Folder).folder }))
      .style("cursor", "pointer")
      .append('svg:title')
      .text(d => folderColor!(((d.data as Folder).folder)));

    node.filter(d => !isFolder(d)).append("rect")
      .attr('shape-rendering', 'initial')
      .attr("opacity", opacity)
      .attr("width", d => nodeWidth(d))
      .attr("height", d => nodeHeight(d))
      .style("fill", d => color(d.data as ChartRow)!)
      .on('click', p => this.props.onDrillDown(p.data as ChartRow))
      .style("cursor", "pointer")
      .append('svg:title')
      .text(d => keyColumn.getValueNiceName(d.data as ChartRow) + ': ' + valueColumn.getValueNiceName(d.data as ChartRow));

    var showNumber = parseFloat(data.parameters["NumberOpacity"]) > 0;

    var nodeFilter = node.filter(d => !isFolder(d.data) && nodeWidth(d) > 10 && nodeHeight(d) > 25);

    nodeFilter.append("text")
      .attr("text-anchor", "middle")
      .attr('dominant-baseline', 'middle')
      .attr("dx", d => nodeWidth(d) / 2)
      .attr("dy", d => nodeHeight(d) / 2 + (showNumber ? -6 : 0))
      .on('click', p => this.props.onDrillDown(p.data as ChartRow))
      .style("cursor", "pointer")
      .text(d => keyColumn.getValueNiceName(d.data as ChartRow))
      .each(function (d) { ellipsis(this as SVGTextElement, nodeWidth(d), 4, ""); })
      .append('svg:title')
      .text(d => keyColumn.getValueNiceName(d.data as ChartRow) + ': ' + valueColumn.getValueNiceName(d.data as ChartRow));

    if (showNumber) {
      nodeFilter.append("text")
        .attr('fill', data.parameters["NumberColor"] || "#fff")
        .attr('opacity', ".5")
        .attr('dominant-baseline', 'central')
        .attr('opacity', parseFloat(data.parameters["NumberOpacity"]))
        .attr('text-anchor', 'middle')
        .attr('font-weight', 'bold')
        .attr("dx", d => nodeWidth(d) / 2)
        .attr("dy", d => nodeHeight(d) / 2 + 6)
        .on('click', p => this.props.onDrillDown(p.data as ChartRow))
        .style("cursor", "pointer")
        .text(d => valueColumn.getValueNiceName(d.data as ChartRow))
        .each(function (d) { ellipsis(this as SVGTextElement, nodeWidth(d), 1, "") });
    }

  }
}
