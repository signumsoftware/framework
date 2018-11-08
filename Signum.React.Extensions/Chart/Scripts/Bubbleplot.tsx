import * as React from 'react'
import * as d3 from 'd3'
import D3ChartBase from '../D3ChartBase';
import * as ChartClient from '../ChartClient';
import * as ChartUtils from '../Templates/ChartUtils';
import { getClickKeys, translate, scale, rotate, skewX, skewY, matrix, scaleFor, rule, ellipsis } from '../Templates/ChartUtils';
import { ChartRow } from '../ChartClient';


export default class BubblePlotChart extends D3ChartBase {

    drawChart(data: ChartClient.ChartTable, chart: d3.Selection<SVGElement, {}, null, undefined>, width: number, height: number) {
        
        var colorKeyColumn = data.columns.c0!;
        var horizontalColumn = data.columns.c1! as ChartClient.ChartColumn<number>;
        var verticalColumn = data.columns.c2 as ChartClient.ChartColumn<number>;
        var sizeColumn = data.columns.c3 as ChartClient.ChartColumn<number>;

        var xRule = rule({
            _1: 5,
            title: 15,
            _2: 5,
            labels: parseInt(data.parameters["UnitMargin"] || "0"),
            _3: 5,
            ticks: 4,
            content: '*',
            _4: 5,
        }, width);
        //xRule.debugX(chart)

        var yRule = rule({
            _1: 5,
            content: '*',
            ticks: 4,
            _2: 5,
            labels: 10,
            _3: 10,
            title: 15,
            _4: 5,
        }, height);
        //yRule.debugY(chart);


        var x = scaleFor(horizontalColumn, data.rows.map(r => horizontalColumn.getValue(r)), 0, xRule.size('content'), data.parameters["HorizontalScale"]);

        var y = scaleFor(verticalColumn, data.rows.map(r => verticalColumn.getValue(r)), 0, yRule.size('content'), data.parameters["VerticalScale"]);

        var xTickSize = verticalColumn.type == "Date" || verticalColumn.type == "DateTime" ? 100 : 60;

        var xTicks = x.ticks(width / xTickSize);
        var xTickFormat = x.tickFormat(width / 50);

        chart.append('svg:g').attr('class', 'x-lines').attr('transform', translate(xRule.start('content'), yRule.start('content')))
            .enterData(xTicks, 'line', 'y-lines')
            .attr('x1', t => x(t))
            .attr('x2', t => x(t))
            .attr('y1', yRule.size('content'))
            .style('stroke', 'LightGray');

        chart.append('svg:g').attr('class', 'x-tick').attr('transform', translate(xRule.start('content'), yRule.start('ticks')))
            .enterData(xTicks, 'line', 'x-tick')
            .attr('x1', x)
            .attr('x2', x)
            .attr('y2', yRule.size('ticks'))
            .style('stroke', 'Black');

        chart.append('svg:g').attr('class', 'x-label').attr('transform', translate(xRule.start('content'), yRule.end('labels')))
            .enterData(xTicks, 'text', 'x-label')
            .attr('x', x)
            .attr('text-anchor', 'middle')
            .text(xTickFormat);

        chart.append('svg:g').attr('class', 'x-title').attr('transform', translate(xRule.middle('content'), yRule.middle('title')))
            .append('svg:text').attr('class', 'x-title')
            .attr('text-anchor', 'middle')
            .attr('dominant-baseline', 'middle')
            .text(verticalColumn.title);


        var yTicks = y.ticks(height / 50);
        var yTickFormat = y.tickFormat(height / 50);
        chart.append('svg:g').attr('class', 'y-lines').attr('transform', translate(xRule.start('content'), yRule.end('content')))
            .enterData(yTicks, 'line', 'y-lines')
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

        chart.append('svg:g').attr('class', 'y-title').attr('transform', translate(xRule.middle('title'), yRule.middle('content')) + rotate(270))
            .append('svg:text').attr('class', 'y-title')
            .attr('text-anchor', 'middle')
            .attr('dominant-baseline', 'middle')
            .text(verticalColumn.title);

        var color: (r: ChartRow) => string;
        if (data.parameters["ColorScale"] == "Ordinal") {
            var scheme = ChartUtils.getColorScheme(data.parameters["ColorScheme"], parseInt(data.parameters["ColorSchemeSteps"] || "0"));
            var categoryColor = d3.scaleOrdinal(scheme).domain(data.rows.map(r => colorKeyColumn.getValueKey(r)));
            color = r => colorKeyColumn.getValueColor(r) || categoryColor(colorKeyColumn.getValueKey(r));
        } else {
            var scaleFunc = scaleFor(colorKeyColumn, data.rows.map(r => colorKeyColumn.getValue(r) as number), 0, 1, data.parameters["ColorScale"]);
            var colorInterpolate = data.parameters["ColorInterpolate"];
            var colorInterpolation = ChartUtils.getColorInterpolation(colorInterpolate)!;
            color = r => colorInterpolation(scaleFunc(colorKeyColumn.getValue(r) as number))
        }
        var sizeList = data.rows.map(r => sizeColumn.getValue(r));

        var sizeTemp = scaleFor(sizeColumn, sizeList, 0, 1, data.parameters["SizeScale"]);

        var totalSizeTemp = d3.sum(data.rows, r => sizeTemp(sizeColumn.getValue(r)));

        var sizeScale = scaleFor(sizeColumn, sizeList, 0, (xRule.size('content') * yRule.size('content')) / (totalSizeTemp * 3), data.parameters["SizeScale"]);


        //PAINT GRAPH
        var gr = chart.enterData(data.rows.sort(p => -sizeColumn.getValue(p)), 'g', 'shape-serie').attr('transform', translate(xRule.start('content'), yRule.end('content')));

        gr.append('svg:circle').attr('class', 'shape')
            .attr('stroke', r => colorKeyColumn.getValueColor(r) || color(r))
            .attr('stroke-width', 3)
            .attr('fill', r => colorKeyColumn.getValueColor(r) || color(r))
            .attr('fill-opacity', parseFloat(data.parameters["FillOpacity"] || "0"))
            .attr('shape-rendering', 'initial')
            .attr('r', r => Math.sqrt(sizeScale(sizeColumn.getValue(r)) / Math.PI))
            .attr('cx', r => x(horizontalColumn.getValue(r)))
            .attr('cy', r => -y(verticalColumn.getValue(r)));

        if (data.parameters["ShowLabel"] == 'Yes') {
            gr.append('svg:text')
                .attr('class', 'number-label')
                .attr('x', r => x(horizontalColumn.getValue(r)))
                .attr('y', r => -y(verticalColumn.getValue(r)))
                .attr('fill', r => data.parameters["LabelColor"] || colorKeyColumn.getValueColor(r) || color(r))
                .attr('dominant-baseline', 'central')
                .attr('text-anchor', 'middle')
                .attr('font-weight', 'bold')
                .text(r => sizeColumn.getValueNiceName(r))
                .each(function (r) { ellipsis(this as SVGTextElement, Math.sqrt(sizeScale(sizeColumn.getValue(r)) / Math.PI) * 2, 0, ""); });
        }

        gr.attr('data-click', p => getClickKeys(p, data.columns))
            .append('svg:title')
            .text(r => colorKeyColumn.getValueNiceName(r) +
                ("\n" + horizontalColumn.title + ": " + horizontalColumn.getValueNiceName(r)) +
                ("\n" + verticalColumn.title + ": " + verticalColumn.getValueNiceName(r)) +
                ("\n" + sizeColumn.title + ": " + sizeColumn.getValueNiceName(r))
            );
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
