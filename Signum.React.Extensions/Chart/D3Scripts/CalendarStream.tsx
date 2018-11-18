import * as React from 'react'
import * as d3 from 'd3'
import D3ChartBase from './D3ChartBase';
import * as ChartClient from '../ChartClient';
import * as ChartUtils from '../Templates/ChartUtils';
import { translate, scale, rotate, skewX, skewY, matrix, scaleFor, rule, ellipsis } from '../Templates/ChartUtils';
import { ChartRow } from '../ChartClient';


export default class CalendarStreamChart extends D3ChartBase {

  drawChart(data: ChartClient.ChartTable, chart: d3.Selection<SVGElement, {}, null, undefined>, width: number, height: number) {

    var dateColumn = data.columns.c0! as ChartClient.ChartColumn<string>;
    var valueColumn = data.columns.c1 as ChartClient.ChartColumn<number>;

    var format = d3.timeFormat("%Y-%m-%d");

    var monday = data.parameters["StartDate"] == "Monday"

    var dayString = d3.timeFormat("%w");
    var day = !monday ?
      (d: Date) => parseInt(dayString(d)) :
      (d: Date) => {
        var old = parseInt(dayString(d));
        return old == 0 ? 6 : old - 1;
      };

    var weekString = d3.timeFormat("%U");
    var week = !monday ?
      (d: Date) => parseInt(weekString(d)) :
      (d: Date) => parseInt(dayString(d)) == 0 ? parseInt(weekString(d)) - 1 : parseInt(weekString(d));

    var scaleFunc = scaleFor(valueColumn, data.rows.map(r => valueColumn.getValue(r)), 0, 1, data.parameters["ColorScale"]);

    var colorInterpolate = data.parameters["ColorInterpolate"];
    var colorInterpolation = ChartUtils.getColorInterpolation(colorInterpolate)!;
    var color = (r: ChartRow) => colorInterpolation(scaleFunc(valueColumn.getValue(r)))

    var minDate = d3.min(data.rows, r => new Date(dateColumn.getValue(r)))!;
    var maxDate = d3.max(data.rows, r => new Date(dateColumn.getValue(r)))!;

    var numDaysX = 53;
    var numDaysY = ((maxDate.getFullYear() - minDate.getFullYear() + 1) * (7 + 1));

    var horizontal = (numDaysX > numDaysY) == (width > height);

    var cellSizeX = (width - 20) / (horizontal ? numDaysX : numDaysY);
    var cellSizeY = (height - 20) / (horizontal ? numDaysY : numDaysX);
    var cellSize = Math.min(cellSizeX, cellSizeY);

    var cleanDate = (d: Date) => d.toJSON().afterLast(".");

    var yRule = rule({
      _1: '*',
      title: 14,
      _2: 4,
      content: (horizontal ? numDaysY : numDaysX) * cellSize,
      _4: '*',
    }, height);
    //yRule.debugY(chart);

    var xRule = rule({
      _1: '*',
      title: 14,
      _2: 4,
      content: (horizontal ? numDaysX : numDaysY) * cellSize,
      _4: '*',
    }, width);
    //xRule.debugX(chart);

    var yearRange = d3.range(minDate.getFullYear(), maxDate.getFullYear() + 1);

    var svg = chart
      .append('svg:g')
      .attr("transform", translate(xRule.start("content"), yRule.start("content")))
      .enterData(yearRange, "g", "year-group")
      .attr("transform", yr => horizontal ?
        translate(0, (yr - minDate.getFullYear()) * (cellSize * (7 + 1))) :
        translate((yr - minDate.getFullYear()) * (cellSize * (7 + 1)), 0)
      );

    svg.append("text")
      .attr("transform", horizontal ? translate(-6, cellSize * 3.5) + rotate(-90) :
        translate(cellSize * 3.5, -6))
      .attr("text-anchor", "middle")
      .text(String);

    var groups = data.rows.toObject(r => dateColumn.getValueKey(r));

    var rect = svg.selectAll("rect.day")
      .data(d => d3.utcDays(new Date(Date.UTC(d, 0, 1)), new Date(Date.UTC(d + 1, 0, 1))))
      .enter().append("rect")
      .attr("stroke", "#ccc")
      .attr("fill", d => {
        var r = groups[cleanDate(d)];
        return r == undefined ? "#fff" : color(r);
      })
      .attr("width", cellSize)
      .attr("height", cellSize)
      .attr("x", d => (horizontal ? week(d) : day(d)) * cellSize)
      .attr("y", d => (horizontal ? (6 - day(d)) : week(d)) * cellSize)
      .style("cursor", "pointer")
      .on('click', d => {
        var r = groups[cleanDate(d)];
        return r == undefined ? null : this.props.onDrillDown(r);
      })
      .append("title")
      .text(d => {
        var r = groups[cleanDate(d)];
        return format(d) + (r == undefined ? "" : ("(" + valueColumn.getValueNiceName(r) + ")"));
      });

    svg.selectAll("path.month")
      .data(d => d3.timeMonths(new Date(d, 0, 1), new Date(d + 1, 0, 1)))
      .enter().append("path")
      .attr("class", "month")
      .attr("stroke", "#666")
      .attr("stroke-width", 1)
      .attr("fill", "none")
      .attr("d", horizontal ? monthPathH : monthPathV);


    function monthPathH(t0: Date): string {
      var t1 = new Date(t0.getFullYear(), t0.getMonth() + 1, 0),
        d0 = +day(t0), w0 = +week(t0),
        d1 = +day(t1), w1 = +week(t1);
      return "M" + (w0) * cellSize + "," + (7 - d0) * cellSize
        + "H" + (w0 + 1) * cellSize + "V" + 7 * cellSize
        + "H" + (w1 + 1) * cellSize + "V" + (7 - d1 - 1) * cellSize
        + "H" + (w1) * cellSize + "V" + 0
        + "H" + (w0) * cellSize + "Z";

    }

    function monthPathV(t0: Date): string {
      var t1 = new Date(t0.getFullYear(), t0.getMonth() + 1, 0),
        d0 = +day(t0), w0 = +week(t0),
        d1 = +day(t1), w1 = +week(t1);
      return "M" + d0 * cellSize + "," + (w0) * cellSize
        + "V" + (w0 + 1) * cellSize + "H" + 0
        + "V" + (w1 + 1) * cellSize + "H" + (d1 + 1) * cellSize
        + "V" + (w1) * cellSize + "H" + 7 * cellSize
        + "V" + (w0) * cellSize + "Z";

    }
  }
}
