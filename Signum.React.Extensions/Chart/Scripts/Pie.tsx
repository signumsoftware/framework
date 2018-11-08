import * as React from 'react'
import * as d3 from 'd3'
import D3ChartBase from '../D3ChartBase';
import * as ChartClient from '../ChartClient';
import * as ChartUtils from '../Templates/ChartUtils';
import { getClickKeys, translate, scale, rotate, skewX, skewY, matrix, scaleFor, rule, ellipsis } from '../Templates/ChartUtils';
import { ChartRow, ChartTable } from '../ChartClient';


export default class PieChart extends D3ChartBase {

    drawChart(data: ChartTable, chart: d3.Selection<SVGElement, {}, null, undefined>, width: number, height: number) {
        
        var keyColumn = data.columns.c0!;
        var valueColumn = data.columns.c1! as ChartClient.ChartColumn<number>;

        var pInnerRadius = data.parameters.InnerRadious || "0";
        var pSort = data.parameters.Sort;

        var size = d3.scaleLinear()
            .domain([0, d3.max(data.rows, r => valueColumn.getValue(r))!])
            .range([0, 1]);

        var outerRadious = d3.min([width / 2, height])! / 3;
        var rInner = outerRadious * parseFloat(pInnerRadius);
        var color = d3.scaleOrdinal(ChartUtils.getColorScheme(data.parameters["ColorScheme"], parseInt(data.parameters["ColorSchemeSteps"] || "0")))
            .domain(data.rows.map(r => keyColumn.getValueKey(r)));


        var pie = d3.pie<ChartRow>()
            .sort(pSort == "Ascending" ? ((a, b) => d3.descending(size(valueColumn.getValue(a)), size(valueColumn.getValue(b)))) :
                pSort == "Descending" ? ((a, b) => d3.ascending(size(valueColumn.getValue(a)), size(valueColumn.getValue(b)))) : null)
            .value(r => size(valueColumn.getValue(r)));

        var arc = d3.arc<ChartRow>()
            .outerRadius(outerRadious)
            .innerRadius(rInner);

        chart.append('svg:g').attr('class', 'shape').attr('transform', translate(width / 2, height / 2))
            .enterData(pie(data.rows), 'g', 'slice')
            .append('svg:path').attr('class', 'shape')
            .attr('d', slice => arc(slice.data)!)
            .attr('fill', slice => keyColumn.getValueColor(slice.data) || color(keyColumn.getValueKey(slice.data)))
            .attr('shape-rendering', 'initial')
            .attr('data-click', slice => getClickKeys(slice.data, data.columns))
            .append('svg:title')
            .text(slice => keyColumn.getValueNiceName(slice.data) + ': ' + valueColumn.getValueNiceName(slice.data));

        //paint color legend
        var cx = (width / 2),
            cy = (height / 2),
            legendRadius = 1.2;

        chart.append('svg:g').data([data.rows]).attr('class', 'color-legend').attr('transform', translate(cx, cy))
            .enterData(pie, 'g', 'color-legend')
            .append('svg:text').attr('class', 'color-legend sf-chart-strong')
            .attr('x', slice => { var m = (slice.endAngle + slice.startAngle) / 2; return Math.sin(m) * outerRadious * legendRadius; })
            .attr('y', slice => { var m = (slice.endAngle + slice.startAngle) / 2; return -Math.cos(m) * outerRadious * legendRadius; })
            .attr('text-anchor', slice => {
                var m = (slice.endAngle + slice.startAngle) / 2;
                var cuadr = Math.floor(12 * m / (2 * Math.PI));
                return (1 <= cuadr && cuadr <= 4) ? 'start' : (7 <= cuadr && cuadr <= 10) ? 'end' : 'middle';
            })
            .attr('fill', slice => { return keyColumn.getValueColor(slice.data) || color(keyColumn.getValueKey(slice.data)); })
            .attr('data-click', slice => { return getClickKeys(slice.data, data.columns); })
            .text(slice => { return ((slice.endAngle - slice.startAngle) >= (Math.PI / 16)) ? keyColumn.getValueNiceName(slice.data) : ''; });
    }
}
