import * as React from 'react'
import * as d3 from 'd3'
import D3ChartBase from '../D3ChartBase';
import * as ChartClient from '../ChartClient';
import * as ChartUtils from '../Templates/ChartUtils';
import { getClickKeys, translate, scale, rotate, skewX, skewY, matrix, scaleFor, rule, ellipsis } from '../Templates/ChartUtils';
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

        var folderColor: null | ((folder: object) => string | undefined) = null;
        if (parentColumn) {
            var scheme = ChartUtils.getColorScheme(data.parameters["ColorScheme"], parseInt(data.parameters["ColorSchemeSteps"] || "0"));
            var categoryColor = d3.scaleOrdinal(scheme).domain(data.rows.map(r => parentColumn!.getValueKey(r)));
            folderColor = folder => parentColumn!.getColor(folder) || categoryColor(parentColumn!.getKey(folder));
        }

        var format = d3.format(",d");

        var root = ChartUtils.stratifyTokens(data, keyColumn, parentColumn);

        root.sum(d => d.value)
            .sort(function (a, b) { return b.value - a.value; })

        var size = scaleFor(data.columns.c1, data.rows.map(r => r.c1), 0, 1, data.parameters["Scale"]);

        root.sum(r => r == null ? 0 : size(r.c1));

        var bubble = d3.pack()
            .size([width, height])
            .padding(2);

        bubble(root);

        var nodes = root.descendants().filter(d => d.data.isRoot == null);

        var node = chart.selectAll("g.node")
            .data(nodes)
            .enter().append("g")
            .attr("class", "node")
            .attr("transform", d => "translate(" + d.x + "," + d.y + ")")
            .attr('data-click', p => getClickKeys(p, data.columns));

        node.append("circle")
            .attr('shape-rendering', 'initial')
            .attr("r", d => d.r)
            .style("fill", d => d.data.folder ? folderColor(d.data.folder) : color(d.data))
            .style("fill-opacity", d => data.parameters["FillOpacity"])
            .style("stroke", d => data.parameters["StrokeColor"] || (d.data.folder ? folderColor(d.data.folder) : color(d.data)))
            .style("stroke-width", data.parameters["StrokeWidth"])
            .style("stroke-opacity", 1)
            .attr('data-click', p => p.data.folder ? getClickKeys({ c2: p.data.folder }, data.columns) : getClickKeys(p.data, data.columns));

        var showNumber = data.parameters["NumberOpacity"] > 0;
        var numberSizeLimit = data.parameters["NumberSizeLimit"];

        node.filter(d => !d.data.folder).append("text")
            .attr('dominant-baseline', 'central')
            .attr('text-anchor', 'middle')
            .attr("dy", d => showNumber && d.r > numberSizeLimit ? "-0.5em" : null)
            .text(d => d.data.folder ? d.data.folder.niceToString() : d.data.c0 ? (d.data.c0.niceToString()) : undefined)
            .attr('data-click', p => p.data.folder ? getClickKeys({ c2: p.data.folder }, data.columns) : getClickKeys(p.data, data.columns))
            .each(d => ellipsis(this, d.r * 2, 1, ""));

        if (showNumber) {
            node.filter(d => d.r > numberSizeLimit)
                .append("text")
                .attr('fill', data.parameters["NumberColor"])
                .attr('dominant-baseline', 'central')
                .attr('text-anchor', 'middle')
                .attr('font-weight', 'bold')
                .attr('opacity', d => data.parameters["NumberOpacity"] * d.r / 30)
                .attr("dy", ".5em")
                .attr('data-click', p => p.data.folder ? getClickKeys({ c2: p.data.folder }, data.columns) : getClickKeys(p.data, data.columns))
                .text(d => d.data.c1;
        })

        node.append('svg:title')
            .text(function (d) {
                var key = (d.data.folder ? d.data.folder.niceToString() :
                    (d.data.c0.niceToString() + (data.columns.c2.token == null ? '' : (' (' + (d.data.c2 ? d.data.c2.niceToString() : null) + ')'))));


                var value = (d.data.folder ? format(size.invert(d.value)) :
                    (d.data.c1 + (data.columns.c3.token == null ? '' : (' (' + d.data.c3 + ')'))));

                return key + ': ' + value;
            });
    }
}
