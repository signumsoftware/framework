import * as React from 'react'
import * as d3 from 'd3'
import * as ChartClient from '../ChartClient';
import * as ChartUtils from './Components/ChartUtils';
import { translate, scale, rotate, skewX, skewY, matrix, scaleFor } from './Components/ChartUtils';
import { ChartRow } from '../ChartClient';
import { Rule } from './Components/Rule';
import InitialMessage from './Components/InitialMessage';


export default function renderCalendarStream({ data, width, height, parameters, loading, onDrillDown, initialLoad }: ChartClient.ChartScriptProps): React.ReactElement<any> {

  if (data == null || data.rows.length == 0)
    return (
      <svg direction="ltr" width={width} height={height}>
        <InitialMessage data={data} x={width / 2} y={height / 2} loading={loading} />
      </svg>
    );

  var dateColumn = data.columns.c0! as ChartClient.ChartColumn<string>;
  var valueColumn = data.columns.c1 as ChartClient.ChartColumn<number>;

  var format = d3.timeFormat("%Y-%m-%d");

  var monday = parameters["StartDate"] == "Monday"

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

  var scaleFunc = scaleFor(valueColumn, data.rows.map(r => valueColumn.getValue(r)), 0, 1, parameters["ColorScale"]);

  var colorInterpolate = parameters["ColorInterpolate"];
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

  var cleanDate = (d: Date) => d.toJSON().beforeLast(".");

  var yRule = new Rule({
    _1: '*',
    title: 14,
    _2: 4,
    content: (horizontal ? numDaysY : numDaysX) * cellSize,
    _4: '*',
  }, height);
  //yRule.debugY(chart);

  var xRule = new Rule({
    _1: '*',
    title: 14,
    _2: 4,
    content: (horizontal ? numDaysX : numDaysY) * cellSize,
    _4: '*',
  }, width);
  //xRule.debugX(chart);

  var yearRange = d3.range(minDate.getFullYear(), maxDate.getFullYear() + 1);

  var rowYByDate = data.rows.toObject(r => dateColumn.getValueKey(r));


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

  return (
    <svg direction="ltr" width={width} height={height}>
      <g transform={translate(xRule.start("content"), yRule.start("content"))}>
        {yearRange.map(yr => <g key={yr} className="year-group sf-transition"
          transform={horizontal ?
            translate(0, (yr - minDate.getFullYear()) * (cellSize * (7 + 1))) :
            translate((yr - minDate.getFullYear()) * (cellSize * (7 + 1)), 0)}>

          <text transform={horizontal ? translate(-6, cellSize * 3.5) + rotate(-90) :
            translate(cellSize * 3.5, -6)} textAnchor="middle">
            {yr}
          </text>

          {d3.utcDays(new Date(Date.UTC(yr, 0, 1)), new Date(Date.UTC(yr + 1, 0, 1))).map(d => {
            const r = rowYByDate[cleanDate(d)];
            return <rect key={d.toISOString()}
              className="sf-transition"
              stroke="#ccc"
              fill={r == undefined || initialLoad ? "#fff" : color(r)}
              width={cellSize}
              height={cellSize}
              x={(horizontal ? week(d) : day(d)) * cellSize}
              y={(horizontal ? (6 - day(d)) : week(d)) * cellSize}
              cursor="pointer"
              onClick={e => r == undefined ? null : onDrillDown(r)}>
              <title>
                {format(d) + (r == undefined ? "" : ("(" + valueColumn.getValueNiceName(r) + ")"))}
              </title>
            </rect>
          })}

          {d3.timeMonths(new Date(yr, 0, 1), new Date(yr + 1, 0, 1))
            .map(m => <path key={m.toString()}
              className="month"
              stroke="#666"
              strokeWidth={1}
              fill="none"
              d={horizontal ? monthPathH(m) : monthPathV(m)} />
            )
          }
        </g>)}
      </g>
    </svg>
  );

}
