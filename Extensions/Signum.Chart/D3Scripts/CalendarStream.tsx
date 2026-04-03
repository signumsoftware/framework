import * as React from 'react'
import { DateTime, Settings } from 'luxon'
import * as d3 from 'd3'
import { ChartClient, ChartColumn, ChartRow, ChartScriptProps, ChartTable } from '../ChartClient';
import * as ChartUtils from './Components/ChartUtils';
import { translate, scale, rotate, skewX, skewY, matrix, scaleFor } from './Components/ChartUtils';
import { Rule } from './Components/Rule';
import InitialMessage from './Components/InitialMessage';
import { MemoRepository } from './Components/ReactChart';
import { ChartMessage, ChartRequestModel, D3ChartScript } from '../Signum.Chart';
import { DashboardFilter } from '../../Signum.Dashboard/View/DashboardFilterController';
import { symbolNiceName, getQueryNiceName } from '@framework/Reflection';


export default function renderCalendarStream({ data, width, height, parameters, loading, onDrillDown, initialLoad, dashboardFilter, chartRequest }: ChartScriptProps): React.ReactElement<any> {

  if (data == null || data.rows.length == 0)
    return (
      <svg direction="ltr" width={width} height={height}>
        <InitialMessage data={data} x={width / 2} y={height / 2} loading={loading} />
      </svg>
    );

  var dateColumn = data.columns.c0! as ChartColumn<string>;
  var valueColumn = data.columns.c1 as ChartColumn<number>;

  var monday = parameters["StartDate"] == "Monday"

  var scaleFunc = scaleFor(valueColumn, data.rows.map(r => valueColumn.getValue(r)), 0, 1, parameters["ColorScale"]);

  var colorInterpolate = parameters["ColorInterpolate"];
  var colorInterpolation = ChartUtils.getColorInterpolation(colorInterpolate)!;
  var color = (r: ChartRow) => colorInterpolation(scaleFunc(valueColumn.getValue(r))!);

  var rowsWithValue = data.rows.filter(r => dateColumn.getValue(r) != null);

  var minDate = d3.min(rowsWithValue, r => new Date(dateColumn.getValue(r)))!;
  var maxDate = d3.max(rowsWithValue, r => new Date(dateColumn.getValue(r)))!;
  var years = d3.range(minDate.getFullYear(), maxDate.getFullYear() + 1);

  function getRules(weeksSize: number, daysSize: number): Rules {

    var weeksRule = Rule.create({
      _1: 4,
      yearTitle: 14,
      _2: 4,
      weekDayTitle: 14,
      _3: 4,
      weeksContent: '*',
      _4: 4,
    }, weeksSize);

    var daysRule = Rule.create({
      _1: 4,
      monthTitle: 14,
      _3: 4,
      daysContent: '*',
      _4: 4,
    }, daysSize);

    var cellSizeWeeks = weeksRule.size("weeksContent") / 53;
    var cellSizeDays = daysRule.size("daysContent") / 7;

    var cellSize: number;
    if (cellSizeWeeks < cellSizeDays) {
      cellSize = cellSizeWeeks;
      daysRule = Rule.create({
        _1: 4,
        monthTitle: 14,
        _3: 4,
        daysContent: cellSizeWeeks * 7,
        _4: 4,
      });
    }
    else {
      cellSize = cellSizeDays;
      weeksRule = Rule.create({
        _1: 4,
        yearTitle: 14,
        _2: 4,
        weekDayTitle: 14,
        _3: 4,
        weeksContent: cellSizeDays * 53,
        _4: 4,
      });
    }

    return {
      cellSize,
      weeksRule,
      daysRule
    };
  }

  var vertical = getRules(height, width / years.length);
  var horizontal = getRules(width, height / years.length);

  var rules = vertical.cellSize > horizontal.cellSize ? vertical : horizontal;

  var rowByDate = data.rows.toObject(r => {
    var date = dateColumn.getValueKey(r);
    return date.tryBefore("T") ?? date;
  });

  var xRule = Rule.create({
    _1: '*',
    content: rules == vertical ? rules.daysRule.totalSize * years.length : rules.weeksRule.totalSize,
    _2: '*',
  }, width);

  var yRule = Rule.create({
    _1: '*',
    content: rules == vertical ? rules.weeksRule.totalSize : rules.daysRule.totalSize * years.length,
    _2: '*',
  }, height);


  return (
    <svg direction="ltr" width={width} height={height} role="img">
      <title id="calendarStreamChartTitle">{ChartMessage._0Of1_2.niceToString(symbolNiceName(D3ChartScript.CalendarStream), getQueryNiceName(chartRequest.queryKey), [valueColumn.title, dateColumn.title].join(", "))}</title>
      <g transform={translate(xRule.start("content"), yRule.start("content"))}>
        {years.map((yr, i) => <CalendarYear key={yr}
          transform={rules == vertical ?
            translate(i * rules.daysRule.totalSize, 0) :
            translate(0, i * rules.daysRule.totalSize) + rotate(-90) + translate(-rules.daysRule.totalSize, 0)
          }
          year={yr}
          rules={rules}
          rowByDate={rowByDate}
          width={width}
          height={height}
          onDrillDown={onDrillDown}
          initialLoad={initialLoad}
          monday={monday}
          valueColumn={valueColumn}
          color={color}
          isHorizontal={rules == horizontal}
          dashboardFilter={dashboardFilter}
          chartRequest={chartRequest} />)}
      </g>
      {/*{xRule.debugX()}*/}
      {/*{yRule.debugY()}*/}
    </svg>
  );
}

interface Rules {
  cellSize: number;
  weeksRule: Rule<"yearTitle" | "weekDayTitle" | "weeksContent">;
  daysRule: Rule<"monthTitle" | "daysContent">;
}

export function CalendarYear({ year, rules, rowByDate, width, height, onDrillDown, initialLoad, transform, monday, valueColumn, color, isHorizontal, dashboardFilter, chartRequest }: {
  year: number;
  rowByDate: { [date: string] : ChartRow }
  rules: Rules;
  data?: ChartTable;
  onDrillDown: (row: ChartRow, e: React.MouseEvent<any> | MouseEvent) => void;
  width: number;
  height: number;
  initialLoad: boolean;
  isHorizontal: boolean;
  transform: string; 
  monday: boolean;
  valueColumn: ChartColumn<number>
  color: (cr: ChartRow) => string;
  dashboardFilter?: DashboardFilter,
  chartRequest: ChartRequestModel
}): React.JSX.Element {

  var cellSize = rules.cellSize;

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

  var cleanDate = (d: Date) => d.toJSON().before("T");

  function monthPath(t0: Date): string {
    var t1 = new Date(t0.getFullYear(), t0.getMonth() + 1, 0),
      d0 = +day(t0), w0 = +week(t0),
      d1 = +day(t1), w1 = +week(t1);
    return "M" + d0 * cellSize + "," + (w0) * cellSize
      + "V" + (w0 + 1) * cellSize + "H" + 0
      + "V" + (w1 + 1) * cellSize + "H" + (d1 + 1) * cellSize
      + "V" + (w1) * cellSize + "H" + 7 * cellSize
      + "V" + (w0) * cellSize + "Z";
  }

  var dateFormat = d3.timeFormat("%Y-%m-%d");

  var monthNames = getMonthsNames();
  var weekdayNames = getWeekDayNames(monday);

  function monthPosition(i: number) {
    var firstDay = week(DateTime.local(year, i, 1).toJSDate());
    var lastDay = week(DateTime.local(year, i, 1).plus({ month: 1, day: -1 }).toJSDate());

    return (firstDay + lastDay) / 2.0;
  }

  var detector = ChartClient.getActiveDetector(dashboardFilter, chartRequest);

  return (
    <g key={year} className="year-group sf-transition"
      transform={transform}>

      <text transform={translate(rules.daysRule.middle("daysContent"), rules.weeksRule.end("yearTitle"))} textAnchor="middle" opacity={detector ? .5 : undefined}>
        {year}
      </text>

      <g transform={translate(rules.daysRule.start("daysContent"), rules.weeksRule.end("weekDayTitle"))} textAnchor="middle" opacity={detector ? .5 : undefined}>
        {weekdayNames.map((wdn, i) => <text key={i} transform={translate((i + 0.5) * cellSize, 0)} textAnchor="middle" opacity={detector ? .5 : undefined} fontSize="10">
          {wdn}
        </text>)
        }
      </g>

      <g transform={translate(rules.daysRule.middle("monthTitle"), rules.weeksRule.start("weeksContent"))} textAnchor="middle" opacity={detector ? .5 : undefined}>
        {monthNames.map((month, i) => <text key={i} transform={translate(0, (monthPosition(i + 1) + 0.5) * cellSize) + (isHorizontal ? rotate(90) :"") } textAnchor="middle" dominantBaseline="middle" opacity={detector ? .5 : undefined} fontSize="10">
          {month}
        </text>)
        }
      </g>

      <g transform={translate(rules.daysRule.start("daysContent"), rules.weeksRule.start("weeksContent"))}>
        {d3.utcDays(new Date(Date.UTC(year, 0, 1)), new Date(Date.UTC(year + 1, 0, 1))).map(d => {
          const r: ChartRow | undefined = rowByDate[cleanDate(d)];
          const active = r && detector?.(r);
          return (r == undefined) ?
            (
              <g className="hover-group" key={d.toISOString()}>
                <rect
                  className="sf-transition hover-target"
                  stroke={active == true ? "var(--bs-body-color)" : "#ccc"}
                  fill={"transparent"}
                  width={cellSize}
                  height={cellSize}
                  x={day(d) * cellSize}
                  y={week(d) * cellSize}>
                  <title>
                    {dateFormat(d)}
                  </title>
                </rect>
              </g>
            )
            :
            (
              <g className="hover-group" key={d.toISOString()}>
                <rect
                  className="sf-transition hover-target"
                  opacity={active == false ? .5 : undefined}
                  stroke={active == true ? "var(--bs-body-color)" : "#ccc"}
                  strokeWidth={active == true ? 2 : undefined}
                  fill={initialLoad ? "transparent" : color(r)}
                  width={cellSize}
                  height={cellSize}
                  x={day(d) * cellSize}
                  y={week(d) * cellSize}
                  role="button"
                  tabIndex={0}
                  cursor="pointer"
                  onKeyDown={e => {
                    if (e.key === "Enter" || e.key === " ") {
                      e.preventDefault();
                      (onclick as any)?.(e);
                    }
                  }}
                  onClick={e => onDrillDown(r, e)}>
                  <title>
                    {dateFormat(d) + ("(" + valueColumn.getValueNiceName(r) + ")")}
                  </title>
                </rect>
              </g>
            );
        })}
      </g>

      <g transform={translate(rules.daysRule.start("daysContent"), rules.weeksRule.start("weeksContent"))} opacity={detector ? .5 : undefined} >
        {d3.timeMonths(new Date(year, 0, 1), new Date(year + 1, 0, 1))
          .map(m => <path key={m.toString()}
            className="month"
            stroke="#666"
            strokeWidth={1}
            fill="none"
            d={monthPath(m)} />
          )
        }
      </g>
      {/*{rules.daysRule.debugX(rules.weeksRule.totalSize)}*/}
      {/*{rules.weeksRule.debugY(rules.daysRule.totalSize)}*/}
    </g>
  );
}

function getMonthsNames() {
  var format = new Intl.DateTimeFormat(Settings.defaultLocale, { month: "short" });
  var months = []
  for (var month = 0; month < 12; month++) {
    var testDate = new Date(Date.UTC(2000, month, 1, 0, 0, 0));
    months.push(format.format(testDate))
  }
  return months;
}

function getWeekDayNames(monday: boolean) {
  var format = new Intl.DateTimeFormat(Settings.defaultLocale, { weekday: "narrow" });
  var months = []
  for (var day = 0; day < 7; day++) {
    var testDate = new Date(Date.UTC(2000, 0, 3 + day + (monday ? 0 : -1), 0, 0, 0));
    months.push(format.format(testDate))
  }
  return months;
}
