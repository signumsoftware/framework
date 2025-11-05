import * as React from 'react'
import * as d3 from 'd3'
import * as ChartUtils from './Components/ChartUtils';
import { translate, scale, rotate, skewX, skewY, matrix, scaleFor } from './Components/ChartUtils';
import { ChartClient, ChartTable, ChartColumn, ChartRow, ChartScriptProps } from '../ChartClient';
import { Dic } from '@framework/Globals';
import { XKeyTicks } from './Components/Ticks';
import { Rule } from './Components/Rule';
import InitialMessage from './Components/InitialMessage';
import { ChartMessage, ChartParameter, D3ChartScript } from '../Signum.Chart';
import { symbolNiceName, getQueryNiceName } from '@framework/Reflection';

interface ColumnWithScales {
  column: ChartColumn<number>;
  scale: d3.ScaleContinuousNumeric<number, number>;
  colorScale: (r: ChartRow) => string;
}

export default function renderParallelCoordinates(p: ChartScriptProps): React.ReactElement<any> {
  return <ParallelCoordinatesImp {...p} />
}


function ParallelCoordinatesImp({ data, width, height, parameters, loading, onDrillDown, initialLoad, chartRequest, dashboardFilter }: ChartScriptProps) {

  var [selectedColumnName, setSelectedColumnName] = React.useState<string | undefined>(undefined);

  var yRule = Rule.create({
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

  var xRule = Rule.create({
    _1: 20,
    content: '*',
    _2: 20,
  }, width);
  //xRule.debugX(chart);

  if (data == null || data.rows.length == 0)
    return (
      <svg direction="ltr" width={width} height={height}>
        <InitialMessage data={data} x={xRule.middle("content")} y={yRule.middle("content")} loading={loading} />
      </svg>
    );

  var keyColumn = data.columns.c0!;

  var colorInterpolate = parameters["ColorInterpolate"];
  var colorInterpolation = ChartUtils.getColorInterpolation(colorInterpolate)!;

  var cords = Dic.getValues(data.columns)
    .filter(c => c && c.name != "c0" && c.name != "entity")
    .map(p => {
      const c = p! as ChartColumn<number>;
      var values = data.rows.map(r => c.getValue(r));
      var scaleType = parameters[("Scale" + c.name.after("c")) as ChartParameter];
      var scale = scaleFor(c, values, 0, yRule.size('content'), scaleType);
      var scaleFunc = scaleFor(c, values, 0, 1, scaleType);
      var colorScale = (r: ChartRow) => colorInterpolation(scaleFunc(c.getValue(r))!);

      return {
        column: c,
        scale,
        colorScale
      } as ColumnWithScales;
    });

  var x = d3.scaleBand()
    .domain(cords.map(d => d.column.name))
    .rangeRound([0, xRule.size('content')]);

  var line = d3.line<{ col: ColumnWithScales, row: ChartRow }>()
    .defined(t => t.col.column.getValue(t.row) != undefined)
    .x(t => x(t.col.column.name)!)
    .y(t => - t.col.scale(t.col.column.getValue(t.row))!)
    .curve(ChartUtils.getCurveByName(parameters["Interpolate"])!);//"linear"

  var boxWidth = 10;

  var selectedColumn = cords.firstOrNull(a => a.column.name == selectedColumnName) || cords.first();

  var detector = ChartClient.getActiveDetector(dashboardFilter, chartRequest);

  var keyColumns: ChartColumn<any>[] = data.columns.entity ? [data.columns.entity] :
    [data.columns.c1, data.columns.c2, data.columns.c3, data.columns.c4, data.columns.c5, data.columns.c6, data.columns.c7, data.columns.c8].filter(cn => cn != undefined).filter(a => a.token && a.token.queryTokenType != "Aggregate")

  var aggregateColumns: ChartColumn<any>[] = data.columns.entity ? [data.columns.entity] :
    [data.columns.c1, data.columns.c2, data.columns.c3, data.columns.c4, data.columns.c5, data.columns.c6, data.columns.c7, data.columns.c8].filter(cn => cn != undefined).filter(a => a.token && a.token.queryTokenType == "Aggregate")

  var titleMessage = (aggregateColumns.length != 0) ?
    ChartMessage._0Of1_2Per3.niceToString(symbolNiceName(D3ChartScript.ParallelCoordinates), getQueryNiceName(chartRequest.queryKey), keyColumns.map(cn => cn.title).join(", "), aggregateColumns.map(cn => cn.title).join(", ")) :
    ChartMessage._0Of1_2.niceToString(symbolNiceName(D3ChartScript.ParallelCoordinates), getQueryNiceName(chartRequest.queryKey), keyColumns.map(cn => cn.title).join(", "));

  return (
    <svg direction="ltr" width={width} height={height} role="img">
      <title id="parallelCoodinatesChartTitle">{titleMessage}</title>
      <g className="x-tick" transform={translate(xRule.start('content') + x.bandwidth() / 2, yRule.start('content'))}>
        {cords.map(d => <line key={d.column.name} className="x-tick sf-transition"
          transform={translate(x(d.column.name)!, 0)}
          y2={yRule.size('content')}
          stroke="var(--bs-body-color)" />)}
      </g>

      <g className="x-label" transform={translate(xRule.start('content') + x.bandwidth() / 2, yRule.middle('title'))}>
        {cords.map(d => <text key={d.column.name} className="x-label sf-transition"
          transform={translate(x(d.column.name)!, 0)}
          dominantBaseline="middle"
          textAnchor="middle"
          fontWeight="bold">
          {d.column.title}
        </text>)}
      </g>

      <g className="x-label-max" transform={translate(xRule.start('content') + x.bandwidth() / 2, yRule.middle('max'))}>
        {cords.map(d => <text key={d.column.name} className="x-label-max sf-transition"
          transform={translate(x(d.column.name)!, 0)}
          dominantBaseline="middle"
          textAnchor="middle">
          {d.column.type != "Date" && d.column.type != "DateTime" ?
            d.scale.domain()[1] :
            d.column.getNiceName(d3.max(data.rows, r => d.column.getValue(r))!)}
        </text>)}
      </g>

      <g className="x-label-min" transform={translate(xRule.start('content') + x.bandwidth() / 2, yRule.middle('min'))}>
        {cords.map(d => <text key={d.column.name} className="x-label-min sf-transition"
          transform={translate(x(d.column.name)!, 0)}
          dominantBaseline="middle"
          textAnchor="middle">
          {d.column.type != "Date" && d.column.type != "DateTime" ?
            d.column.getNiceName(d.scale.domain()[0]) :
            d.column.getNiceName(d3.min(data.rows, r => d.column.getValue(r))!)}
        </text>)}
      </g>


      {data.rows.orderBy(r => keyColumn.getValueKey(r)).map((r, i) => {

        var active = detector?.(r);

        return (
          <g key={i} className="shape-serie"
            opacity={active == false ? .5 : undefined}
            transform={translate(xRule.start('content') + x.bandwidth() / 2, yRule.end('content'))}>
            <path
              opacity={initialLoad ? 0 : 1}
              className="shape sf-transition"
              fill="none"
              strokeWidth={active == true ? 3 : 2}
              stroke={active == true ? "var(--bs-body-color)" : selectedColumn.colorScale(r)}
              shapeRendering="initial"
              onClick={e => onDrillDown(r, e)}
              role="button"
              tabIndex={0}
              cursor="pointer"
              onKeyDown={e => {
                if (e.key === "Enter" || e.key === " ") {
                  e.preventDefault();
                  (onclick as any)?.(e);
                }
              }}
              d={line(cords.map(c => ({ col: c, row: r })))!}>
              <title>
                {keyColumn.getValueNiceName(r) + "\n" +
                  cords.map(c => c.column.title + ": " + c.column.getValueNiceName(r)).join("\n")}
              </title>
            </path>
          </g>
        );
      })}

      <g className="x-tick-box" transform={translate(xRule.start('content') + x.bandwidth() / 2, yRule.start('content'))}>
        {cords.map(d => <rect key={d.column.name}
          transform={translate(x(d.column.name)! - boxWidth / 2, 0)}
          className="x-tick-box sf-transition"
          height={yRule.size('content')}
          width={boxWidth}
          stroke="#ccc"
          fill={selectedColumn.column.name != d.column.name ? '#ccc' : '#000'}
          fillOpacity=".2"
          role="button"
          tabIndex={0}
          cursor="pointer"
          onKeyDown={e => {
            if (e.key === "Enter" || e.key === " ") {
              e.preventDefault();
              (onclick as any)?.(e);
            }
          }}
          onClick={e => setSelectedColumnName(d.column.name)} />)}
      </g>

      <InitialMessage data={data} x={xRule.middle("content")} y={yRule.middle("content")} loading={loading} />
    </svg>
  );
}


