import * as React from 'react'
import * as d3 from 'd3'
import D3ChartBase from './D3ChartBase';
import * as ChartClient from '../ChartClient';
import * as ChartUtils from '../Templates/ChartUtils';
import { translate, scale, rotate, skewX, skewY, matrix, scaleFor, rule, ellipsis } from '../Templates/ChartUtils';
import { ChartColumn, ChartRow } from '../ChartClient';


export default class PunchcardChart extends D3ChartBase {

  drawChart(data: ChartClient.ChartTable, chart: d3.Selection<SVGElement, {}, null, undefined>, width: number, height: number) {

    var horizontalColumn = data.columns.c0!;
    var verticalColumn = data.columns.c1!;
    var sizeColumn = data.columns.c2! as ChartColumn<number> | undefined;
    var colorColumn = data.columns.c3! as ChartColumn<number> | undefined;
    var opacityColumn = data.columns.c4! as ChartColumn<number> | undefined;
    var innerSizeColumn = data.columns.c5! as ChartColumn<number> | undefined;
    var orderingColumn = data.columns.c6! as ChartColumn<number> | undefined;

    function groupAndSort(rows: ChartRow[], shortType: string, column: ChartColumn<unknown>): unknown[] {
      var array = rows.groupBy(r => "k" + column.getValueKey(r));

      switch (shortType) {
        case "AscendingToStr": array = array.orderBy(g => column.getValueNiceName(g.elements[0]) || "");
        case "AscendingKey": array = array.orderBy(g => column.getValueKey(g.elements[0]) || "");
        case "AscendingSumOrder": array = array.orderBy(g => getSum(g));
        case "DescendingToStr": array = array.orderByDescending(g => column.getValueNiceName(g.elements[0]) || "");
        case "DescendingKey": array = array.orderByDescending(g => column.getValueKey(g.elements[0]) || "");
        case "DescendingSumOrder": array = array.orderByDescending(g => getSum(g));
        default: array = array;
      }

      return array.map(g => column.getValue(g.elements[0]));
    }

    function getSum(group: { key: string; elements: ChartRow[], sum?: number }) {

      if (orderingColumn == null)
        return 0;

      if (group.sum !== undefined)
        return group.sum;

      return group.sum = group.elements.reduce<number>((acum, r) => acum + orderingColumn!.getValue(r) || 0, 0);
    }

    var dim0 = groupAndSort(data.rows, data.parameters["XSort"]!, horizontalColumn);
    var dim1 = groupAndSort(data.rows, data.parameters["YSort"]!, verticalColumn);

    var xRule = rule({
      _1: 5,
      title: 15,
      _2: 10,
      labels: parseInt(data.parameters["XMargin"]),
      _3: 5,
      ticks: 4,
      content: '*',
      _4: 10,
    }, width);
    //xRule.debugX(chart)

    var yRule = rule({
      _1: 5,
      content: '*',
      ticks: 4,
      _2: 5,
      labels0: 15,
      labels1: 15,
      _3: 10,
      title: 15,
      _4: 5,
    }, height);
    //yRule.debugY(chart);

    var x = d3.scaleBand()
      .domain(dim0.map(horizontalColumn.getKey))
      .range([0, xRule.size('content')]);

    var y = d3.scaleBand()
      .domain(dim1.map(verticalColumn.getKey))
      .range([0, yRule.size('content')]);

    var color: null | ((row: number) => string) = null;
    if (colorColumn != null) {
      var scaleFunc = scaleFor(colorColumn, data.rows.map(colorColumn.getValue), 0, 1, data.parameters["ColorScale"]);
      var colorInterpolator = ChartUtils.getColorInterpolation(data.parameters["ColorInterpolate"]);
      color = v => colorInterpolator!(scaleFunc(v))
    }

    var opacity: null | ((row: number) => number) = null;
    if (opacityColumn != null) {
      opacity = scaleFor(opacityColumn, data.rows.map(opacityColumn.getValue), 0, 1, data.parameters["OpacityScale"]);
    }

    var shape = data.parameters["Shape"];
    var innerSize = null
    if (innerSizeColumn != null) {
      innerSize = scaleFor(innerSizeColumn, data.rows.map(innerSizeColumn.getValue), 0, 100, data.parameters["OpacityScale"])
    }
    if (data.parameters["VerticalLineColor"]) {
      chart.append('svg:g').attr('class', 'x-line').attr('transform', translate(xRule.start('content') + x.bandwidth() / (shape == "ProgressBar" ? 1 : 2), yRule.start('content')))
        .enterData(dim0, 'line', 'y-line')
        .attr('y2', yRule.size('content'))
        .attr('x1', d => x(horizontalColumn.getKey(d))!)
        .attr('x2', d => x(horizontalColumn.getKey(d))!)
        .style('stroke', data.parameters["VerticalLineColor"]!);
    }

    chart.append('svg:g').attr('class', 'x-tick').attr('transform', translate(xRule.start('content') + x.bandwidth() / (shape == "ProgressBar" ? 1 : 2), yRule.start('ticks')))
      .enterData(dim0, 'line', 'x-tick')
      .attr('y2', yRule.size('ticks'))
      .attr('x1', d => x(horizontalColumn.getKey(d))!)
      .attr('x2', d => x(horizontalColumn.getKey(d))!)
      .style('stroke', 'Black');

    if ((x.bandwidth() * 2) > 60) {
      chart.append('svg:g').attr('class', 'x-label').attr('transform', translate(xRule.start('content') + (x.bandwidth() / 2), yRule.middle('labels0')))
        .enterData(dim0, 'text', 'x-label')
        .attr('x', d => x(horizontalColumn.getKey(d))!)
        .attr('y', (d, i) => yRule.middle('labels' + (i % 2)) - yRule.middle('labels0'))
        .attr('dominant-baseline', 'middle')
        .attr('text-anchor', 'middle')
        .text(d => horizontalColumn.getNiceName(d))
        .each(function (v) { ellipsis(this as SVGTextElement, x.bandwidth() * 1.7); });
    }

    chart.append('svg:g').attr('class', 'x-title').attr('transform', translate(xRule.middle('content'), yRule.middle('title')))
      .append('svg:text').attr('class', 'x-title')
      .attr('text-anchor', 'middle')
      .attr('dominant-baseline', 'middle')
      .text(horizontalColumn.title);


    if (data.parameters["HorizontalLineColor"]) {
      chart.append('svg:g').attr('class', 'y-line').attr('transform', translate(xRule.start('content'), yRule.end('content') - y.bandwidth() / (shape == "ProgressBar" ? 1 : 2)))
        .enterData(dim1, 'line', 'y-line')
        .attr('x2', xRule.size('content'))
        .attr('y1', t => -y(verticalColumn.getKey(t))!)
        .attr('y2', t => -y(verticalColumn.getKey(t))!)
        .style('stroke', data.parameters["HorizontalLineColor"]!);
    }

    chart.append('svg:g').attr('class', 'y-tick').attr('transform', translate(xRule.start('ticks'), yRule.end('content') - y.bandwidth() / 2))
      .enterData(dim1, 'line', 'y-tick')
      .attr('x2', xRule.size('ticks'))
      .attr('y1', t => -y(verticalColumn.getKey(t))!)
      .attr('y2', t => -y(verticalColumn.getKey(t))!)
      .style('stroke', 'Black');

    if (y.bandwidth() > 16) {
      chart.append('svg:g').attr('class', 'y-label').attr('transform', translate(xRule.end('labels'), yRule.end('content') - y.bandwidth() / 2))
        .enterData(dim1, 'text', 'y-label')
        .attr('y', t => -y(verticalColumn.getKey(t))!)
        .attr('dominant-baseline', 'middle')
        .attr('text-anchor', 'end')
        .text(d => verticalColumn.getNiceName(d))
        .each(function (v) { ellipsis(this as SVGTextElement, xRule.size('labels')); });
    }

    chart.append('svg:g').attr('class', 'y-label').attr('transform', translate(xRule.middle('title'), yRule.middle('content')) + rotate(270))
      .append('svg:text').attr('class', 'y-label')
      .attr('text-anchor', 'middle')
      .attr('dominant-baseline', 'middle')
      .text(verticalColumn.title);


    var groups = chart.enterData(data.rows, 'g', "chart-groups")
      .style("cursor", "pointer")
      .on('click', r => this.props.onDrillDown(r));


    function configureShape(column: ChartColumn<number> | undefined, rowValue: (r: ChartRow) => number, extra: { numberOpacity?: (val: number) => number }) {

      var shapes = groups.append(shape == "Circle" ? 'circle' : 'rect').attr('transform', translate(xRule.start('content') + x.bandwidth() / 2, yRule.end('content') - y.bandwidth() / 2))
        .filter(r => r != undefined)
        .attr('fill-opacity', r => parseFloat(data.parameters["FillOpacity"]) * (opacity != null ? opacity(opacityColumn!.getValue(r)) : 1))
        .attr("shape-rendering", "initial");


      if (shape == "Circle") {

        var circleSize = Math.min(x.bandwidth(), y.bandwidth()) * 0.45;
        var area: (n: number) => number = column == null ?
          (() => circleSize * circleSize) :
          scaleFor(column, data.rows.map(column.getValue), 0, circleSize * circleSize, data.parameters["SizeScale"]);
        extra.numberOpacity = r => area(r) / 500;

        shapes.attr('cx', r => x(horizontalColumn.getValueKey(r))!)
          .attr('cy', r => -y(verticalColumn.getValueKey(r))!)
          .attr('r', r => Math.sqrt(area(rowValue(r))));

      } else if (shape == "Rectangle") {

        var area: (n: number) => number = column == null ?
          (() => x.bandwidth() * y.bandwidth()) :
          scaleFor(column, data.rows.map(column.getValue), 0, x.bandwidth() * y.bandwidth(), data.parameters["SizeScale"]);
        var ratio = x.bandwidth() / y.bandwidth();
        var recWidth = (r: ChartRow) => Math.sqrt(area(rowValue(r)) * ratio);
        var recHeight = (r: ChartRow) => Math.sqrt(area(rowValue(r)) / ratio);
        extra.numberOpacity = r => area(r) / 500;

        shapes
          .attr('x', r => x(horizontalColumn.getValueKey(r))! - recWidth(r) / 2)
          .attr('y', r => -y(verticalColumn.getValueKey(r))! - recHeight(r) / 2)
          .attr('width', recWidth)
          .attr('height', recHeight);

      } else if (shape == "ProgressBar") {

        var progressWidth: (n: number) => number = column == null ?
          () => x.bandwidth() :
          scaleFor(column, data.rows.map(column.getValue), 0, x.bandwidth(), data.parameters["SizeScale"]);

        extra.numberOpacity = r => 1;

        shapes.attr('x', r => x(horizontalColumn.getValueKey(r))! - x.bandwidth() / 2)
          .attr('y', r => -y(verticalColumn.getValueKey(r))! - y.bandwidth() / 2)
          .attr('width', r => progressWidth(rowValue(r)))
          .attr('height', y.bandwidth());
      }

      return shapes;
    }

    var extra: { numberOpacity?: (val: number) => number } = {};

    configureShape(sizeColumn, r => sizeColumn ? sizeColumn.getValue(r) : 0, extra)
      .attr('fill', r => color == null ? (data.parameters["FillColor"] || 'black') : color(colorColumn!.getValue(r)))
      .attr('stroke', r => data.parameters["StrokeColor"] || (color == null ? 'black' : color(colorColumn!.getValue(r))))
      .attr('stroke-width', data.parameters["StrokeWidth"])
      .attr('stroke-opacity', r => (opacity != null ? opacity(opacityColumn!.getValue(r)) : 1));

    var isRelative = data.parameters["InnerSizeType"] == "Relative";
    if (innerSizeColumn != null) {
      var fun = !isRelative ? innerSizeColumn.getValue :
        sizeColumn != null ? (r: ChartRow) => innerSizeColumn!.getValue(r) * sizeColumn!.getValue(r) :
          innerSizeColumn.getValue;

      var domain = !isRelative ? data.rows.map(innerSizeColumn.getValue) :
        sizeColumn != null ? data.rows.map(sizeColumn.getValue) :
          [1];

      configureShape(innerSizeColumn, fun, {})
        .attr('fill', data.parameters["InnerFillColor"] || 'black')
    }


    function percentage(v: number) { return Math.floor(v * 10000) / 100 + "%"; }

    if (parseFloat(data.parameters["NumberOpacity"]) > 0) {
      groups.append('text').attr('class', 'punch').attr('transform', translate(xRule.start('content') + x.bandwidth() / 2, yRule.end('content') - y.bandwidth() / 2))
        .attr('x', r => x(horizontalColumn.getValueKey(r))!)
        .attr('y', r => -y(verticalColumn.getValueKey(r))!)
        .attr('fill', data.parameters["NumberColor"])
        .attr('dominant-baseline', 'central')
        .attr('opacity', r => parseFloat(data.parameters["NumberOpacity"]) * extra.numberOpacity!(sizeColumn!.getValue(r)))
        .attr('text-anchor', 'middle')
        .attr('font-weight', 'bold')
        .text(r => sizeColumn ? sizeColumn.getValueNiceName(r) :
          innerSizeColumn != null ? (isRelative ? percentage(innerSizeColumn.getValue(r)) : innerSizeColumn.getValue(r)) :
            colorColumn != null ? colorColumn.getValue(r) :
              opacityColumn != null ? opacityColumn.getValue(r) : null
        );

    }

    colorColumn
    opacityColumn
    innerSizeColumn
    orderingColumn

    groups.append('svg:title')
      .text(r => horizontalColumn.getValueNiceName(r) + ', ' + verticalColumn.getValueNiceName(r) +
        (sizeColumn == null ? "" : ("\n" + sizeColumn.title + ": " + sizeColumn.getValueNiceName(r))) +
        (colorColumn == null ? "" : ("\n" + colorColumn.title + ": " + colorColumn.getValueNiceName(r))) +
        (opacityColumn == null ? "" : ("\n" + opacityColumn.title + ": " + opacityColumn.getValueNiceName(r))) +
        (innerSizeColumn == null ? "" : ("\n" + innerSizeColumn.title + ": " + (isRelative ? percentage(innerSizeColumn.getValue(r)) : innerSizeColumn.getValueNiceName(r)))) +
        (orderingColumn == null ? "" : ("\n" + orderingColumn.title + ": " + orderingColumn.getValueNiceName(r)))
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
