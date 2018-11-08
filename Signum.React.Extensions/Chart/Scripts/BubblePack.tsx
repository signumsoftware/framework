import * as React from 'react'
import * as d3 from 'd3'
import D3ChartBase from '../D3ChartBase';
import * as ChartClient from '../ChartClient';
import * as ChartUtils from '../Templates/ChartUtils';
import { getClickKeys, translate, scale, rotate, skewX, skewY, matrix, scaleFor, rule, ellipsis, Folder, isFolder, Root, isRoot } from '../Templates/ChartUtils';
import { ChartRow, ChartTable } from '../ChartClient';


export default class BubblePackChart extends D3ChartBase {

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
            var scheme = ChartUtils.getColorScheme(data.parameters["ColorScheme"], parseInt(data.parameters["ColorSchemeSteps"] || "0"));
            var categoryColor = d3.scaleOrdinal(scheme).domain(data.rows.map(r => colorSchemeColumn!.getValueKey(r)));
            color = r => colorSchemeColumn!.getColor(r) || categoryColor(colorSchemeColumn!.getValueKey(r));
        }
        else {
            var scheme = ChartUtils.getColorScheme(data.parameters["ColorScheme"], parseInt(data.parameters["ColorSchemeSteps"] || "0"));
            var categoryColor = d3.scaleOrdinal(scheme).domain(data.rows.map(r => keyColumn.getValueKey(r)));
            color = r => keyColumn.getValueColor(r) || categoryColor(keyColumn.getValueKey(r));
        }

        var folderColor: null | ((folder: unknown) => string) = null;
        if (parentColumn) {
            var scheme = ChartUtils.getColorScheme(data.parameters["ColorScheme"], parseInt(data.parameters["ColorSchemeSteps"] || "0"));
            var categoryColor = d3.scaleOrdinal(scheme).domain(data.rows.map(r => parentColumn!.getValueKey(r)));
            folderColor = folder => parentColumn!.getColor(folder) || categoryColor(parentColumn!.getKey(folder));
        }

        var format = d3.format(",d");

        var root = ChartUtils.stratifyTokens(data, keyColumn, parentColumn);

        //root.sum(d => valueColumn.getValue(d as ChartRow))
            //.sort(function (a, b) { return b.value - a.value; })

        var size = scaleFor(valueColumn, data.rows.map(r => valueColumn.getValue(r)), 0, 1, data.parameters["Scale"]);

        root.sum(r => r == null ? 0 : size(valueColumn.getValue(r as ChartRow)));

        var bubble = d3.pack<ChartRow | Folder | Root>()
            .size([width, height])
            .padding(2);

        const circularRoot = bubble(root);

        var nodes = circularRoot.descendants().filter(d => !isRoot(d.data)) as d3.HierarchyCircularNode<ChartRow | Folder>[];

        var node = chart.selectAll("g.node")
            .data(nodes)
            .enter().append("g")
            .attr("class", "node")
            .attr("transform", d => "translate(" + d.x + "," + d.y + ")")
            .attr('data-click', p => getClickKeys(p, data.columns));

        node.append("circle")
            .attr('shape-rendering', 'initial')
            .attr("r", d => d.r)
            .style("fill", d => isFolder(d.data) ? folderColor!(d.data.folder) : color(d.data)!)
            .style("fill-opacity", d => data.parameters["FillOpacity"] || null)
            .style("stroke", d => data.parameters["StrokeColor"] || (isFolder(d.data) ? folderColor!(d.data.folder) : (color(d.data) || null)))
            .style("stroke-width", data.parameters["StrokeWidth"] || "0")
            .style("stroke-opacity", 1)
            .attr('data-click', p => isFolder(p.data) ? getClickKeys({ c2: p.data.folder }, data.columns) : getClickKeys(p.data, data.columns));

        var showNumber = parseFloat(data.parameters["NumberOpacity"] || "0") > 0;
        var numberSizeLimit = parseInt(data.parameters["NumberSizeLimit"] || "0");

        node.filter(d => !isFolder(d.data)).append("text")
            .attr('dominant-baseline', 'central')
            .attr('text-anchor', 'middle')
            .attr("dy", d => showNumber && d.r > numberSizeLimit ? "-0.5em" : null)
            .text(d => keyColumn.getValueNiceName(d.data as ChartRow))
            .attr('data-click', p => getClickKeys(p.data, data.columns))
            .each(function (d) { return ellipsis(this as SVGTextElement, d.r * 2, 1, ""); });

        if (showNumber) {
            node.filter(d => d.r > numberSizeLimit && !isFolder(d.data))
                .append("text")
                .attr('fill', data.parameters["NumberColor"] || "#000")
                .attr('dominant-baseline', 'central')
                .attr('text-anchor', 'middle')
                .attr('font-weight', 'bold')
                .attr('opacity', d => parseFloat(data.parameters["NumberOpacity"] || "0") * d.r / 30)
                .attr("dy", ".5em")
                .attr('data-click', p => getClickKeys(p.data, data.columns))
                .text(d => valueColumn.getValueNiceName(d.data as ChartRow));
        }

        node.append('svg:title')
            .text(d => {
                var key = isFolder(d.data) ?
                    parentColumn!.getNiceName(d.data.folder) :
                    (keyColumn.getValueNiceName(d.data as ChartRow)
                        + (parentColumn == null ? '' : (' (' + parentColumn.getValueNiceName(d.data as ChartRow) + ')')));


                var value = isFolder(d.data) ?
                    format(size.invert(d.value!)) :
                    (valueColumn.getValueNiceName(d.data)
                        + (colorScaleColumn == null ? '' : (' (' + colorScaleColumn.getValueNiceName(d.data) + ')'))
                        + (colorSchemeColumn == null ? '' : (' (' + colorSchemeColumn.getValueNiceName(d.data) + ')'))
                    );

                return key + ': ' + value;
            });
    }
}