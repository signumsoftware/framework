import * as React from 'react'
import * as d3 from 'd3'
import D3ChartBase from './D3ChartBase';
import * as ChartClient from '../ChartClient';
import * as ChartUtils from '../Templates/ChartUtils';
import { translate, scale, rotate, skewX, skewY, matrix, scaleFor, rule, ellipsis, PivotRow, groupedPivotTable } from '../Templates/ChartUtils';
import { ChartTable, ChartColumn, ChartRow } from '../ChartClient';


export default class StackedLinesChart extends D3ChartBase {

    drawChart(data: ChartTable, chart: d3.Selection<SVGElement, {}, null, undefined>, width: number, height: number) {

        var c = data.columns;
        var keyColumn = c.c0 as ChartColumn<unknown>;
        var valueColumn0 = c.c2 as ChartColumn<number>;

        var pivot = c.c1 == null ?
            ChartUtils.toPivotTable(data, c.c0!, [c.c2, c.c3, c.c4, c.c5, c.c6].filter(cn => cn != undefined) as ChartColumn<number>[]) :
            ChartUtils.groupedPivotTable(data, c.c0!, c.c1, c.c2 as ChartColumn<number>);

        var xRule = rule({
            _1: 5,
            title: 15,
            _2: 10,
            labels: parseInt(data.parameters["Horizontal Margin"]),
            _3: 5,
            ticks: 4,
            content: '*',
            _4: 10,
        }, width);
        //xRule.debugX(chart)


        var yRule = rule({
            _1: 5,
            legend: 15,
            _2: 5,
            content: '*',
            ticks: 4,
            _3: 5,
            labels0: 15,
            labels1: 15,
            _4: 10,
            title: 15,
            _5: 5,
        }, height);
        //yRule.debugY(chart);

        var x = d3.scaleBand()
            .domain(pivot.rows.map(d => keyColumn.getKey(d.rowValue)))
            .range([0, xRule.size('content')]);

        var pStack = data.parameters["Stack"];

        var stack = d3.stack<PivotRow>()
            .offset(ChartUtils.getStackOffset(pStack)!)
            .order(ChartUtils.getStackOrder(data.parameters["Order"])!)
            .keys(pivot.columns.map(d => d.key))
            .value(function (r, k) {
                var v = r.values[k];
                return v && v.value || 0;
            });

        var stackedSeries = stack(pivot.rows);

        var max = d3.max(stackedSeries, s => d3.max(s, v => v[1]))!;
        var min = d3.min(stackedSeries, s => d3.min(s, v => v[0]))!;

        var y = d3.scaleLinear()
            .domain([min, max])
            .range([0, yRule.size('content')]);

        chart.append('svg:g').attr('class', 'x-tick').attr('transform', translate(xRule.start('content') + (x.bandwidth() / 2), yRule.start('ticks')))
            .enterData(pivot.rows, 'line', 'x-tick')
            .attr('y2', (d, i) => yRule.start('labels' + (i % 2)) - yRule.start('ticks'))
            .attr('x1', d => x(keyColumn.getKey(d.rowValue))!)
            .attr('x2', d => x(keyColumn.getKey(d.rowValue))!)
            .style('stroke', 'Black');

        if ((x.bandwidth() * 2) > 60) {
            chart.append('svg:g').attr('class', 'x-label').attr('transform', translate(xRule.start('content') + (x.bandwidth() / 2), yRule.middle('labels0')))
                .enterData(pivot.rows, 'text', 'x-label')
                .attr('x', d => x(keyColumn.getKey(d.rowValue))!)
                .attr('y', (d, i)  => yRule.middle('labels' + (i % 2)) - yRule.middle('labels0'))
                .attr('dominant-baseline', 'middle')
                .attr('text-anchor', 'middle')
                .text(r => keyColumn.getNiceName(r.rowValue))
                .each(function (v) { ellipsis(this as SVGTextElement, x.bandwidth() * 2); });
        }

        chart.append('svg:g').attr('class', 'x-title').attr('transform', translate(xRule.middle('content'), yRule.middle('title')))
            .append('svg:text').attr('class', 'x-title')
            .attr('text-anchor', 'middle')
            .attr('dominant-baseline', 'middle')
            .text(keyColumn.title);

        var yTicks = y.ticks(10);
        chart.append('svg:g').attr('class', 'y-line').attr('transform', translate(xRule.start('content'), yRule.end('content')))
            .enterData(yTicks, 'line', 'y-line')
            .attr('x2', xRule.size('content'))
            .attr('y1', t => -y(t))
            .attr('y2', t => -y(t))
            .style('stroke', 'LightGray');

        chart.append('svg:g').attr('class', 'y-tick').attr('transform', translate(xRule.start('ticks'), yRule.end('content')))
            .enterData(yTicks, 'line', 'y-tick')
            .attr('x2', xRule.size('ticks'))
            .attr('y1', t => -y(t))
            .attr('y2', t => -y(t))
            .style('stroke', 'Black');

        var formatter = pStack == "expand" ? d3.format(".0%") :
            pStack == "zero" ? d3.format("") :
                (n: number) => d3.format("")(n) + "?";

        chart.append('svg:g').attr('class', 'y-label').attr('transform', translate(xRule.end('labels'), yRule.end('content')))
            .enterData(yTicks, 'text', 'y-label')
            .attr('y', t => -y(t))
            .attr('dominant-baseline', 'middle')
            .attr('text-anchor', 'end')
            .text(formatter);


        chart.append('svg:g').attr('class', 'y-label').attr('transform', translate(xRule.middle('title'), yRule.middle('content')) + rotate(270))
            .append('svg:text').attr('class', 'y-label')
            .attr('text-anchor', 'middle')
            .attr('dominant-baseline', 'middle')
            .text(pivot.title);

        var color = d3.scaleOrdinal(ChartUtils.getColorScheme(data.parameters["ColorScheme"], parseInt(data.parameters["ColorSchemeSteps"]))).domain(pivot.columns.map(s => s.key));

        var pInterpolate = data.parameters["Interpolate"];

        var area = d3.area<d3.SeriesPoint<PivotRow>>()
            .x(v => x(keyColumn.getKey(v.data.rowValue))!)
            .y0(v => -y(v[0]))
            .y1(v => -y(v[1]))
            .curve(ChartUtils.getCurveByName(pInterpolate) as d3.CurveFactory);

        var columnsByKey = pivot.columns.toObject(a => a.key);

        //paint graph
        chart.enterData(stackedSeries, 'g', 'shape-serie').attr('transform', translate(xRule.start('content') + x.bandwidth() / 2, yRule.end('content')))
            .append('svg:path').attr('class', 'shape')
            .attr('stroke', s => color(s.key))
            .attr('fill', s => color(s.key))
            .attr('shape-rendering', 'initial')
            .attr('d', s => area(s))
            .append('svg:title')
            .text(s => columnsByKey[s.key].niceName!)

        var rectRadious = 2;
        const me = this;
        //paint graph - hover area trigger
        chart.enterData(stackedSeries, 'g', 'hover-trigger-serie').attr('transform', translate(xRule.start('content') + x.bandwidth() / 2, yRule.end('content')))
            .each(function (s) {

                d3.select(this).enterData(s, 'rect', 'point')
                    .filter(v => v.data.values[s.key] != undefined)
                    .attr('x', v => x(keyColumn.getKey(v.data.rowValue))! - rectRadious)
                    .attr('y', v => -y(v[1]))
                    .attr('width', 2 * rectRadious)
                    .attr('height', v => y(v[1]) - y(v[0]))
                    .attr('fill', '#fff')
                    .attr('fill-opacity', .1)
                    .attr('stroke', 'none')
                    .on('click', v => me.props.onDrillDown(v.data.values[s.key].rowClick))
                    .style("cursor", "pointer")
                    .append('svg:title')
                    .text(v => v.data.values[s.key].valueTitle);

                if (x.bandwidth() > 15 && parseFloat(data.parameters["NumberOpacity"]) > 0) {
                    d3.select(this).enterData(s, 'text', 'number-label')
                        .filter(v => v.data.values[s.key] != undefined && (y(v[1]) - y(v[0])) > 10)
                        .attr('x', v => x(keyColumn.getKey(v.data.rowValue))!)
                        .attr('y', v => -y(v[1]) * 0.5 - y(v[0]) * 0.5)
                        .attr('fill', data.parameters["NumberColor"])
                        .attr('dominant-baseline', 'central')
                        .attr('opacity', data.parameters["NumberOpacity"])
                        .attr('text-anchor', 'middle')
                        .attr('font-weight', 'bold')
                        .text(v => v.data.values[s.key].value)
                        .on('click', v => me.props.onDrillDown(v.data.values[s.key].rowClick))
                        .style("cursor", "pointer")
                        .append('svg:title')
                        .text(v => v.data.values[s.key].valueTitle)
                }


                var legendScale = d3.scaleBand()
                    .domain(pivot.columns.map((s, i) => i.toString()))
                    .range([0, xRule.size('content')]);

                if (legendScale.bandwidth() > 50) {

                    var legendMargin = yRule.size('legend') + 4;

                    chart.append('svg:g').attr('class', 'color-legend').attr('transform', translate(xRule.start('content'), yRule.start('legend')))
                        .enterData(pivot.columns, 'rect', 'color-rect')
                        .attr('x', (e, i) => legendScale(i.toString())!)
                        .attr('width', yRule.size('legend'))
                        .attr('height', yRule.size('legend'))
                        .attr('fill', s => s.color || color(s.key));

                    chart.append('svg:g').attr('class', 'color-legend').attr('transform', translate(xRule.start('content') + legendMargin, yRule.middle('legend') + 1))
                        .enterData(pivot.columns, 'text', 'color-text')
                        .attr('x', (e, i) => legendScale(i.toString())!)
                        .attr('dominant-baseline', 'middle')
                        .text(s => s.niceName!)
                        .each(function (s) { ellipsis(this as SVGTextElement, legendScale.bandwidth() - legendMargin); });
                }

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

            });
    }
}
