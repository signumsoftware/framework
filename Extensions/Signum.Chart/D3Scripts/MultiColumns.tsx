import * as React from 'react'
import * as d3 from 'd3'
import { ChartClient, ChartScriptProps, ChartTable, ChartColumn } from '../ChartClient';
import * as ChartUtils from './Components/ChartUtils';
import { translate, scale, rotate, skewX, skewY, matrix, scaleFor } from './Components/ChartUtils';
import { PivotRow, toPivotTable, groupedPivotTable } from './Components/PivotTable';
import { XKeyTicks, YScaleTicks } from './Components/Ticks';
import Legend from './Components/Legend';
import { XAxis, YAxis } from './Components/Axis';
import { Rule } from './Components/Rule';
import InitialMessage from './Components/InitialMessage';
import TextIfFits from './Components/TextIfFits';
import { ChartMessage, D3ChartScript } from '../Signum.Chart';
import { symbolNiceName, getQueryNiceName } from '@framework/Reflection';


export default function renderMultiColumns({ data, width, height, parameters, loading, onDrillDown, initialLoad, chartRequest, memo, dashboardFilter }: ChartScriptProps): React.ReactElement<any> {

  var xRule = Rule.create({
    _1: 5,
    title: 15,
    _2: 10,
    labels: parseInt(parameters["UnitMargin"]),
    _3: 5,
    ticks: 4,
    content: '*',
    _4: 10,
  }, width);
  //xRule.debugX(chart)

  var yRule = Rule.create({
    _1: 10,
    legend: 15,
    _2: 5,
    content: '*',
    ticks: 4,
    _3: 5,
    labels: 30,
    _4: 10,
    title: 15,
    _5: 5,
  }, height);
  //yRule.debugY(chart);

  if (data == null || data.rows.length == 0)
    return (
      <svg direction="ltr" width={width} height={height}>
        <InitialMessage data={data} x={xRule.middle("content")} y={yRule.middle("content")} loading={loading} />
        <XAxis xRule={xRule} yRule={yRule} />
        <YAxis xRule={xRule} yRule={yRule} />
      </svg>
    );

  var c = data.columns;
  var keyColumn = c.c0 as ChartColumn<unknown>;
  var valueColumn0 = c.c2 as ChartColumn<number>;

  var pivot = c.c1 == null ?
    toPivotTable(data, c.c0!, [c.c2, c.c3, c.c4, c.c5, c.c6].filter(cn => cn != undefined) as ChartColumn<number>[]) :
    groupedPivotTable(data, c.c0!, c.c1, c.c2 as ChartColumn<number>);

  var keyValues = ChartUtils.completeValues(keyColumn, pivot.rows.map(v => v.rowValue), parameters['CompleteValues'], chartRequest.filterOptions, ChartUtils.insertPoint(keyColumn, valueColumn0));

  var x = d3.scaleBand()
    .domain(keyValues.map(v => keyColumn.getKey(v)))
    .range([0, xRule.size('content')]);

  var allValues = pivot.rows.flatMap(r => pivot.columns.map(function (c) { return r.values[c.key] && r.values[c.key].value; }));

  var y = scaleFor(valueColumn0, allValues, 0, yRule.size('content'), parameters["Scale"]);

  var interMagin = parseInt(parameters["HorizontalMargin"]);
  var labelInterMagin = 2;

  var xSubscale = d3.scaleBand()
    .domain(pivot.columns.map(s => s.key))
    .range([interMagin, x.bandwidth() - interMagin]);

  var columnsInOrder = pivot.columns.orderBy(a => a.key);
  var rowsInOrder = pivot.rows.orderBy(r => keyColumn.getKey(r.rowValue));
  var color = ChartUtils.colorCategory(parameters, columnsInOrder.map(s => s.key), memo);

  var detector = ChartClient.getActiveDetector(dashboardFilter, chartRequest);

  return (
    <svg direction="ltr" width={width} height={height} role="img">
      <title id="multiColumnsChartTitle">{ChartMessage._0Of1_2Per3.niceToString(symbolNiceName(D3ChartScript.MultiColumns), getQueryNiceName(chartRequest.queryKey), [keyColumn.title, valueColumn0.title].join(", "), [c.c1, c.c3, c.c4, c.c5, c.c6].filter(cn => cn != undefined).map(cn => cn.title).join(", "))}</title>
      <XKeyTicks xRule={xRule} yRule={yRule} keyValues={keyValues} keyColumn={keyColumn} x={x} isActive={detector && (val => detector!({ c0: val }))} onDrillDown={(v, e) => onDrillDown({ c0: v }, e)} />
      <g opacity={dashboardFilter ? .5 : undefined}>
        <YScaleTicks xRule={xRule} yRule={yRule} valueColumn={valueColumn0} y={y} />
      </g>
      {columnsInOrder.map(s => <g key={s.key} className="shape-serie"
        transform={translate(xRule.start('content'), yRule.end('content'))} >

        {rowsInOrder
          .filter(r => r.values[s.key] != undefined)
          .map(r => {
            var row = r.values[s.key];
            if (row == undefined)
              return undefined;

            var active = detector?.(row.rowClick);
            var key = keyColumn.getKey(r.rowValue);

            var posy: number;
            var height: number;

            const scaleName = parameters["Scale"];

            if (scaleName == "MinZeroMax") {
              posy = row.value < 0 ? y(0) : y(row.value);
              height = row.value < 0 ? y(0) - y(row.value) : y(row.value) - y(0);
            }
            else {
              posy = y(row.value);
              height = y(row.value);
            }

            return (
              <g className="hover-group" key={key} transform={translate(x(key)! + xSubscale(s.key)!, -posy)}>
                <rect
                  className="shape sf-transition hover-target"
                  opacity={active == false ? .5 : undefined}
                  fill={s.color || color(s.key)}
                  transform={(initialLoad ? scale(1, 0) : scale(1, 1))}
                  width={xSubscale.bandwidth()}
                  height={height}
                  onClick={e => onDrillDown(row.rowClick, e)}
                  role="button"
                  tabIndex={0}
                  cursor="pointer"
                  onKeyDown={e => {
                    if (e.key === "Enter" || e.key === " ") {
                      e.preventDefault();
                      (onclick as any)?.(e);
                    }
                  }}>
                  <title>
                    {row.valueTitle}
                  </title>
                </rect>

                {x.bandwidth() > 15 && parseFloat(parameters["NumberOpacity"]) > 0 &&
                  <TextIfFits className="number-label sf-transition"
                    transform={translate((xSubscale.bandwidth() / 2) + labelInterMagin, height / 2) + rotate(-90)}
                    maxWidth={height}
                    onClick={e => onDrillDown(row.rowClick, e)}
                    role="button"
                    tabIndex={0}
                    cursor="pointer"
                    onKeyDown={e => {
                      if (e.key === "Enter" || e.key === " ") {
                        e.preventDefault();
                        (onclick as any)?.(e);
                      }
                    }}
                    opacity={parameters["NumberOpacity"]}
                    fill={parameters["NumberColor"]}
                    dominantBaseline="middle"
                    textAnchor="middle"
                    fontWeight="bold">
                    {row.valueNiceName}
                    <title>
                      {row.valueTitle}
                    </title>
                  </TextIfFits>
                }

              </g>
            );
          })}

      </g>
      )}

      <Legend pivot={pivot} xRule={xRule} yRule={yRule} color={color} isActive={c.c1 && detector && (row => detector!({ c1: row.value }))} onDrillDown={c.c1 && ((s, e) => onDrillDown({ c1: s.value }, e))}/>

      <InitialMessage data={data} x={xRule.middle("content")} y={yRule.middle("content")} loading={loading} />
      <g opacity={dashboardFilter ? .5 : undefined}>
        <XAxis xRule={xRule} yRule={yRule} />
        <YAxis xRule={xRule} yRule={yRule} />
      </g>
    </svg>
  );
}
