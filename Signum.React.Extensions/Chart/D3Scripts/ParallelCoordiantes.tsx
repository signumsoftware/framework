import * as React from 'react'
import * as d3 from 'd3'
import D3ChartBase from './D3ChartBase';
import * as ChartClient from '../ChartClient';
import * as ChartUtils from '../Templates/ChartUtils';
import { translate, scale, rotate, skewX, skewY, matrix, scaleFor, rule, ellipsis } from '../Templates/ChartUtils';
import { ChartTable, ChartColumn, ChartRow } from '../ChartClient';
import { Dic } from '@framework/Globals';

interface ColumnWithScales {
  column: ChartColumn<number>;
  scale: d3.ScaleContinuousNumeric<number, number>;
  colorScale: (r: ChartRow) => string;
}

export default class ParallelCoordinatesChart extends D3ChartBase {

  drawChart(data: ChartTable, chart: d3.Selection<SVGElement, {}, null, undefined>, width: number, height: number) {

    var keyColumn = data.columns.c0!;

    var yRule = rule({
      _1: 5,
      title: 15,
      _2: 5,
      max: 12,
      _3: 4,
      content: '*',
      _4: 4,
      min: 12,
      _5: 5,
    }, height);

    var xRule = rule({
      _1: 20,
      content: '*',
      _2: 20,
    }, width);
    //xRule.debugX(chart);

    var colorInterpolate = data.parameters["ColorInterpolate"];
    var colorInterpolation = ChartUtils.getColorInterpolation(colorInterpolate)!;

    var cords = Dic.getValues(data.columns)
      .filter(c => c && c.name != "c0" && c.name != "entity")
      .map(p => {
        const c = p! as ChartColumn<number>;
        var values = data.rows.map(r => c.getValue(r));
        var scaleType = data.parameters["Scale" + c.name.after("c")];
        var scale = scaleFor(c, values, 0, yRule.size('content'), scaleType);
        var scaleFunc = scaleFor(c, values, 0, 1, scaleType);
        var colorScale = (r: ChartRow) => colorInterpolation(scaleFunc(c.getValue(r)));

        return {
          column: c,
          scale,
          colorScale
        } as ColumnWithScales;
      });

    var x = d3.scaleBand()
      .domain(cords.map(d => d.column.name))
      .rangeRound([0, xRule.size('content')]);

    chart.append('svg:g').attr('class', 'x-tick').attr('transform', translate(xRule.start('content') + x.bandwidth() / 2, yRule.start('content')))
      .enterData(cords, 'line', 'x-tick')
      .attr('y2', d => yRule.size('content'))
      .attr('x1', d => x(d.column.name)!)
      .attr('x2', d => x(d.column.name)!)
      .style('stroke', 'black');


    chart.append('svg:g').attr('class', 'x-label').attr('transform', translate(xRule.start('content') + x.bandwidth() / 2, yRule.middle('title')))
      .enterData(cords, 'text', 'x-label')
      .attr('x', d => x(d.column.name)!)
      .attr('dominant-baseline', 'middle')
      .attr('text-anchor', 'middle')
      .attr("font-weight", "bold")
      .text(d => d.column.title);

    chart.append('svg:g').attr('class', 'x-label-max').attr('transform', translate(xRule.start('content') + x.bandwidth() / 2, yRule.middle('max')))
      .enterData(cords, 'text', 'x-label-max')
      .attr('x', d => x(d.column.name)!)
      .attr('dominant-baseline', 'middle')
      .attr('text-anchor', 'middle')
      .text(d => d.column.type != "Date" && d.column.type != "DateTime" ?
        d.scale.domain()[1] :
        d.column.getNiceName(d3.max(data.rows, r => d.column.getValue(r))!)
      );


    chart.append('svg:g').attr('class', 'x-label-min').attr('transform', translate(xRule.start('content') + x.bandwidth() / 2, yRule.middle('min')))
      .enterData(cords, 'text', 'x-label-min')
      .attr('x', d => x(d.column.name)!)
      .attr('dominant-baseline', 'middle')
      .attr('text-anchor', 'middle')
      .text(d => d.column.type != "Date" && d.column.type != "DateTime" ?
        d.column.getNiceName(d.scale.domain()[0]) :
        d.column.getNiceName(d3.min(data.rows, r => d.column.getValue(r))!));

    var line = d3.line<{ col: ColumnWithScales, row: ChartRow }>()
      .defined(t => t.col.column.getValue(t.row) != undefined)
      .x(t => x(t.col.column.name)!)
      .y(t => - t.col.scale(t.col.column.getValue(t.row)))
      .curve(ChartUtils.getCurveByName(data.parameters["Interpolate"])!);//"linear"

    //paint graph - line
    var lines = chart.enterData(data.rows, 'g', 'shape-serie').attr('transform', translate(xRule.start('content') + x.bandwidth() / 2, yRule.end('content')))
      .append('svg:path').attr('class', 'shape')
      .attr('fill', 'none')
      .attr('stroke-width', 1)
      .attr('stroke', 'black')
      .attr('shape-rendering', 'initial')
      .on('click', r => this.props.onDrillDown(r))
      .style("cursor", "pointer")
      .attr('d', r => line(cords.map(c => ({ col: c, row: r })))!);

    lines
      .append("title")
      .text(r => keyColumn.getValueNiceName(r) + "\n" +
        cords.map(c => c.column.title + ": " + c.column.getValueNiceName(r)).join("\n")
      );

    var boxWidth = 10;
    var box = chart.append('svg:g').attr('class', 'x-tick-box').attr('transform', translate(xRule.start('content') + x.bandwidth() / 2, yRule.start('content')))
      .enterData(cords, 'rect', 'x-tick-box')
      .attr('height', d => yRule.size('content'))
      .attr('width', boxWidth)
      .attr('x', d => x(d.column.name)! - boxWidth / 2)
      .style('stroke', '#ccc')
      .style('fill', '#ccc')
      .style('fill-opacity', '.2')
      .on("click", d => drawGradient(d));


    var drawGradient = function (col: ColumnWithScales) {
      box.style('fill', d => col.column.name != d.column.name ? '#ccc' : '#000');
      lines.attr("stroke", r => col.colorScale(r));
    };
    
    drawGradient(cords.first());
  }
}
