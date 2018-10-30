import * as React from 'react'
import * as d3 from 'd3'
import D3ChartBase from '../D3ChartBase';
import * as ChartUtils from '../Templates/ChartUtils';
import { getClickKeys, translate, scale, rotate, skewX, skewY, matrix, scaleFor, rule, ellipsis } from '../Templates/ChartUtils';
import { ChartTable } from '../ChartClient';


export default class ColumnsChart extends D3ChartBase {

    drawChart(chartTable: ChartTable, chart: d3.Selection<SVGElement, {}, null, undefined>, width: number, height: number) {

        var data = chartTable as ChartTable<string, number>;

        var keyColumn = data.columns.c0!;
        var valueColumn = data.columns.c1!;

        var xRule = rule({
            _1: 5,
            title: 15,
            _2: 10,
            labels: parseInt(data.parameters["UnitMargin"] || "0"),
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
            _3: data.parameters["Labels"] == "Margin" ? 5 : 0,
            labels: data.parameters["Labels"] == "Margin" ? parseInt(data.parameters["LabelsMargin"] || "0") : 0,
            _4: 10,
            title: 15,
            _5: 5,
        }, height);
        //yRule.debugY(chart);


        var x = d3.scaleBand()
            .domain(data.rows.map(r => keyColumn.getValueKey(r)))
            .range([0, xRule.size('content')]);

        var y = scaleFor(valueColumn, data.rows.map(r => valueColumn.getValue(r)), 0, yRule.size('content'), data.parameters["Scale"]);

        chart.append('svg:g').attr('class', 'x-title').attr('transform', translate(xRule.middle('content'), yRule.middle('title')))
            .append('svg:text').attr('class', 'x-title')
            .attr('text-anchor', 'middle')
            .attr('dominant-baseline', 'middle')
            .text(keyColumn.title);

        var yTicks = y.ticks(10);
        var yTickFormat = y.tickFormat(height / 50);
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

        chart.append('svg:g').attr('class', 'y-label').attr('transform', translate(xRule.end('labels'), yRule.end('content')))
            .enterData(yTicks, 'text', 'y-label')
            .attr('y', t => -y(t))
            .attr('dominant-baseline', 'middle')
            .attr('text-anchor', 'end')
            .text(yTickFormat);

        chart.append('svg:g').attr('class', 'y-label').attr('transform', translate(xRule.middle('title'), yRule.middle('content')) + rotate(270))
            .append('svg:text').attr('class', 'y-label')
            .attr('text-anchor', 'middle')
            .attr('dominant-baseline', 'middle')
            .text(valueColumn.title);

        var color = d3.scaleOrdinal(ChartUtils.getColorScheme(data.parameters["ColorScheme"], parseInt(data.parameters["ColorSchemeSteps"]!)))
            .domain(data.rows.map(r => keyColumn.getValueKey(r)!));


        //PAINT CHART
        chart.append('svg:g').attr('class', 'shape').attr('transform', translate(xRule.start('content'), yRule.end('content')))
            .enterData(data.rows, 'rect', 'shape')
            .attr('y', r => -y(valueColumn.getValue(r)))
            .attr('height', r => y(valueColumn.getValue(r)))
            .attr('width', x.bandwidth)
            .attr('x', r => x(keyColumn.getValueKey(r))!)
            .attr('fill', r => keyColumn.getValueColor(r) || color(keyColumn.getValueKey(r)))
            .attr('stroke', x.bandwidth() > 4 ? '#fff' : null)
            .attr('data-click', r => getClickKeys(r, data.columns))
            .append('svg:title')
            .text(r => keyColumn.getValueNiceName(r) + ': ' + valueColumn.getValueNiceName(r));

        if (x.bandwidth() > 15) {
            if (data.parameters["Labels"] == "Margin") {
                chart.append('svg:g').attr('class', 'x-label').attr('transform', translate(xRule.start('content'), yRule.start('labels')))
                    .enterData(data.rows, 'text', 'x-label')
                    .attr('transform', r => translate(x(keyColumn.getValueKey(r))! + x.bandwidth() / 2, 0) + rotate(-90))
                    .attr('dominant-baseline', 'middle')
                    .attr('font-weight', 'bold')
                    .attr('fill', r => (keyColumn.getValueColor(r) || color(keyColumn.getValueKey(r))))
                    .attr('text-anchor', "end")
                    .attr('data-click', r => getClickKeys(r, data.columns))
                    .text(r => keyColumn.getValueNiceName(r))
                    .each(function (r) { ellipsis(this as SVGTextElement, yRule.size('labels'), labelMargin); });
            }
            else if (data.parameters["Labels"] == "Inside") {
                var size = yRule.size('content');
                var labelMargin = 10;
                chart.append('svg:g').attr('class', 'x-label').attr('transform', translate(xRule.start('content'), yRule.end('content')))
                    .enterData(data.rows, 'text', 'x-label')
                    .attr('transform', r => translate(x(keyColumn.getValueKey(r))! + x.bandwidth() / 2, -y(valueColumn.getValue(r))) + rotate(-90))
                    .attr('dominant-baseline', 'middle')
                    .attr('font-weight', 'bold')
                    .attr('fill', r => y(valueColumn.getValue(r)) >= size / 2 ? '#fff' : (keyColumn.getValueColor(r) || color(keyColumn.getValueKey(r))))
                    .attr('dx', r => y(valueColumn.getValue(r)) >= size / 2 ? -labelMargin : labelMargin)
                    .attr('text-anchor', r => y(valueColumn.getValue(r)) >= size / 2 ? 'end' : 'start')
                    .attr('data-click', r => getClickKeys(r, data.columns))
                    .text(r => keyColumn.getValueNiceName(r))
                    .each(function (r) { var posy = y(valueColumn.getValue(r)); ellipsis(this as SVGTextElement, posy >= size / 2 ? posy : size - posy, labelMargin); });
            }

            if (parseFloat(data.parameters["NumberOpacity"] || "0") > 0) {
                chart.append('svg:g').attr('class', 'numbers-label').attr('transform', translate(xRule.start('content'), yRule.end('content')))
                    .enterData(data.rows, 'text', 'number-label')
                    .filter(r => y(valueColumn.getValue(r)) > 10)
                    .attr('transform', r => translate(x(keyColumn.getValueKey(r))! + x.bandwidth() / 2, -y(valueColumn.getValue(r)) / 2) + rotate(-90))
                    .attr('fill', data.parameters["NumberColor"]||"#000")
                    .attr('dominant-baseline', 'central')
                    .attr('opacity', data.parameters["NumberOpacity"] || "0")
                    .attr('text-anchor', 'middle')
                    .attr('font-weight', 'bold')
                    .attr('data-click', r => getClickKeys(r, data.columns))
                    .text(r => valueColumn.getValueNiceName(r));
            }
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
    }
}
