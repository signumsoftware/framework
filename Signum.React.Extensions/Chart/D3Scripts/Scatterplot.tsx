import * as React from 'react'
import * as d3 from 'd3'
import D3ChartBase from './D3ChartBase';
import * as ChartClient from '../ChartClient';
import * as ChartUtils from '../Templates/ChartUtils';
import { translate, scale, rotate, skewX, skewY, matrix, scaleFor, rule, ellipsis } from '../Templates/ChartUtils';
import { ChartRow } from '../ChartClient';


export default class ScatterplotChart extends D3ChartBase {

  drawChart(data: ChartClient.ChartTable, chart: d3.Selection<SVGElement, {}, null, undefined>, width: number, height: number) {

    var colorKeyColumn = data.columns.c0!;
    var horizontalColumn = data.columns.c1! as ChartClient.ChartColumn<number>;
    var verticalColumn = data.columns.c2! as ChartClient.ChartColumn<number>;

    var xRule = rule({
      _1: 5,
      title: 15,
      _2: 5,
      labels: parseInt(data.parameters["UnitMargin"]),
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

    var x = scaleFor(horizontalColumn, data.rows.map(horizontalColumn.getValue), 0, xRule.size('content'), data.parameters["HorizontalScale"]);

    var y = scaleFor(verticalColumn, data.rows.map(verticalColumn.getValue), 0, yRule.size('content'), data.parameters["VerticalScale"]);


    var pointSize = parseInt(data.parameters["PointSize"]);

    var numXTicks = horizontalColumn.type == 'Date' || horizontalColumn.type == 'DateTime' ? 100 : 60;

    var xTicks = x.ticks(width / numXTicks);
    var xTickFormat = x.tickFormat(width / numXTicks);

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
      .text(horizontalColumn.title);

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

    debugger;
    var color: (val: ChartRow) => string;
    if (data.parameters["ColorScale"] == "Ordinal") {
      var scheme = ChartUtils.getColorScheme(data.parameters["ColorScheme"], parseInt(data.parameters["ColorSchemeSteps"]));
      var categoryColor = d3.scaleOrdinal(scheme).domain(data.rows.map(colorKeyColumn.getValueKey));
      color = r => colorKeyColumn.getValueColor(r) || categoryColor(colorKeyColumn.getValueKey(r));

    } else {
      var scaleFunc = scaleFor(colorKeyColumn, data.rows.map(colorKeyColumn.getValue) as number[], 0, 1, data.parameters["ColorScale"]);
      var colorInterpolate = data.parameters["ColorInterpolate"];
      var colorInterpolation = ChartUtils.getColorInterpolation(colorInterpolate);
      color = r => colorInterpolation!(scaleFunc(colorKeyColumn.getValue(r) as number));
    }


    var svg = chart.node()!;
    var container = svg.parentNode!;

    if (data.parameters["DrawingMode"] == "Svg") {

      //PAINT GRAPH
      chart.enterData(data.rows, 'g', 'shape-serie').attr('transform', translate(xRule.start('content'), yRule.end('content')))
        .append('svg:circle').attr('class', 'shape')
        .attr('stroke', r => colorKeyColumn.getValueColor(r) || color(r))
        .attr('fill', r => colorKeyColumn.getValueColor(r) || color(r))
        .attr('shape-rendering', 'initial')
        .attr('r', pointSize)
        .attr('cx', r => x(horizontalColumn.getValue(r)))
        .attr('cy', r => -y(verticalColumn.getValue(r)))
        .on('click', r => this.props.onDrillDown(r))
        .style("cursor", "pointer")
        .append('svg:title')
        .text(r => colorKeyColumn.getValueNiceName(r) +
          ("\n" + horizontalColumn.title + ": " + horizontalColumn.getValueNiceName(r)) +
          ("\n" + verticalColumn.title + ": " + verticalColumn.getValueNiceName(r))
        );
    } else {
      var w = xRule.size('content');
      var h = yRule.size('content');

      var c = document.createElement('canvas');
      var vc = document.createElement('canvas');
      container.appendChild(c);

      var dummy = chart.append('svg:circle')
        .attr('class', 'dummy')
        .node();

      const canvas = d3.select(c)
        .attr('width', w)
        .attr('height', h)
        .style('position', 'absolute')
        .style('left', xRule.start('content') + 'px')
        .style('top', yRule.start('content') + 'px');

      const virtualCanvas = d3.select(vc)
        .attr('width', w)
        .attr('height', h)
        .style('position', 'absolute')
        .style('left', xRule.start('content') + 'px')
        .style('top', yRule.start('content') + 'px');

      const ctx = c.getContext("2d")!;
      const vctx = vc.getContext("2d")!;
      var colorToData: { [key: string]: ChartRow } = {};
      ctx.clearRect(0, 0, w, h);
      vctx.clearRect(0, 0, w, h);
      data.rows.forEach(function (r, i) {

        var c = colorKeyColumn.getValueColor(r) || color(r);

        ctx.fillStyle = c;
        ctx.strokeStyle = c;
        var vColor = getVirtualColor(i);
        vctx.fillStyle = vColor;
        vctx.strokeStyle = vColor;
        colorToData[vColor] = r;

        var xVal = x(horizontalColumn.getValue(r));
        var yVal = h - y(verticalColumn.getValue(r));

        ctx.beginPath();
        ctx.arc(xVal, yVal, pointSize, 0, 2 * Math.PI);
        ctx.fill();
        ctx.stroke();

        vctx.beginPath();
        vctx.arc(xVal, yVal, pointSize, 0, 2 * Math.PI);
        vctx.fill();
        vctx.stroke();

      });

      console.log(colorToData)

      var getVirtualColor = (index: number): string => d3.rgb(
        Math.floor(index / 256 / 256) % 256,
        Math.floor(index / 256) % 256,
        index % 256)
        .toString();

      c.addEventListener('mousemove', function (e) {
        const imageData = vctx.getImageData(e.offsetX, e.offsetY, 1, 1);
        const color = d3.rgb.apply(null, imageData.data).toString();
        const r = colorToData[color];
        if (r) {
          c.style.cursor = "pointer";
          c.setAttribute("title", colorKeyColumn.getNiceName(r) +
            ("\n" + horizontalColumn.title + ": " + horizontalColumn.getValueNiceName(r)) +
            ("\n" + verticalColumn.title + ": " + verticalColumn.getValueNiceName(r)));
        } else {
          c.style.cursor = "initial";
          c.setAttribute("title", "...");
        }
      });

      c.addEventListener('mouseup', e => {
        const imageData = vctx.getImageData(e.offsetX, e.offsetY, 1, 1);

        const color = d3.rgb.apply(null, imageData.data).toString();
        const p = colorToData[color];
        if (p) {
          this.props.onDrillDown(p);
        }
      });
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
