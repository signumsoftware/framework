import * as React from 'react'
import * as d3 from 'd3'
import * as ChartUtils from './Components/ChartUtils';
import { translate, scale, rotate, skewX, skewY, matrix, scaleFor } from './Components/ChartUtils';
import { PivotRow, toPivotTable, groupedPivotTable } from './Components/PivotTable';
import { ChartClient, ChartTable, ChartColumn, ChartScriptProps } from '../ChartClient';
import { XScaleTicks, YKeyTicks } from './Components/Ticks';
import Legend from './Components/Legend';
import { XAxis, YAxis } from './Components/Axis';
import TextEllipsis from './Components/TextEllipsis';
import { Rule } from './Components/Rule';
import InitialMessage from './Components/InitialMessage';
import TextIfFits from './Components/TextIfFits';
import { ChartMessage, D3ChartScript } from '../Signum.Chart';
import { symbolNiceName, getQueryNiceName } from '@framework/Reflection';


export default function renderStackedBars({ data, width, height, parameters, loading, onDrillDown, initialLoad, chartRequest, memo, dashboardFilter }: ChartScriptProps): React.ReactElement<any> {

  var labelsMargin = parseInt(parameters["LabelsMargin"]);
  var labelsPadding = 5;

  var xRule = Rule.create({
    _1: 5,
    title: 15,
    _2: 10,
    labels: parameters["Labels"] == "Margin" ? labelsMargin: 0,
    _3: parameters["Labels"] == "Margin" ? labelsPadding : 0,
    ticks: 4,
    content: '*',
    _4: 5,
  }, width);
  //xRule.debugX(chart)

  var yRule = Rule.create({
    _1: 10,
    legend: 15,
    _2: 5,
    content: '*',
    ticks: 4,
    _3: 5,
    labels: 10,
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

  var pValueAsPercent = parameters.ValueAsPercent;

  var pivot = c.c1 == null ?
    toPivotTable(data, c.c0!, [c.c2, c.c3, c.c4, c.c5, c.c6].filter(cn => cn != undefined) as ChartColumn<number>[]) :
    groupedPivotTable(data, c.c0!, c.c1, c.c2 as ChartColumn<number>);


  var keyValues = ChartUtils.completeValues(keyColumn, pivot.rows.map(r => r.rowValue), parameters['CompleteValues'], chartRequest.filterOptions, ChartUtils.insertPoint(keyColumn, valueColumn0));
  
  var y = d3.scaleBand()
    .domain(keyValues.map(v => keyColumn.getKey(v)))
    .range([0, yRule.size('content')]);
  
  var pStack = parameters["Stack"];

  var stack = d3.stack<PivotRow>()
    .offset(ChartUtils.getStackOffset(pStack)!)
    .order(ChartUtils.getStackOrder(parameters["Order"])!)
    .keys(pivot.columns.map(d => d.key))
    .value((r, k) => r.values[k]?.value ?? 0);

  var stackedSeries = stack(pivot.rows);

  var rowsByKey = pivot.rows.toObject(r => keyColumn.getKey(r.rowValue));
  
  var max = d3.max(stackedSeries, s => d3.max(s, v => v[1]))!;
  var min = d3.min(stackedSeries, s => d3.min(s, v => v[0]))!;


  var x = d3.scaleLinear()
    .domain([min, max])
    .range([0, xRule.size('content') - (parameters["Labels"] == "Inside" ? (labelsMargin + labelsPadding) : 0)]);

  var color = ChartUtils.colorCategory(parameters, pivot.columns.map(s => s.key), memo);
  var colorByKey = pivot.columns.toObject(a => a.key, a => a.color);

  var format = pStack == "expand" ? d3.format(".0%") :
    pStack == "zero" ? valueColumn0.getNiceName :
      (n: number) => valueColumn0.getNiceName(n) + "?";

  var labelPadding = 5;

  var size = xRule.size('content');

  var detector = ChartClient.getActiveDetector(dashboardFilter, chartRequest);

  const bandMargin = y.bandwidth() > 20 ? 2 : y.bandwidth() > 10 ? 1 : 0;

  return (
    <svg direction="ltr" width={width} height={height} role="img">
      <title id="stackedBarsChartTitle">{ChartMessage._0Of1_2Per3.niceToString(symbolNiceName(D3ChartScript.StackedBars), getQueryNiceName(chartRequest.queryKey), [keyColumn.title, valueColumn0.title].join(", "), [c.c1, c.c3, c.c4, c.c5, c.c6].filter(cn => cn != undefined).map(cn => cn.title).join(", "))}</title>  
      <g opacity={dashboardFilter ? .5 : undefined}>
        <XScaleTicks xRule={xRule} yRule={yRule} valueColumn={valueColumn0} x={x} format={format} />
      </g>
      <YKeyTicks xRule={xRule} yRule={yRule} keyValues={keyValues} keyColumn={keyColumn} y={y} showLabels={false} isActive={detector && (val => detector!({ c0: val }))} onDrillDown={(v, e) => onDrillDown({ c0: v }, e)} />


      {stackedSeries.orderBy(s => s.key).map(s => <g key={s.key} className="shape-serie"
        transform={translate(xRule.start('content'), yRule.start('content'))}>

        {s.filter(r => r.data.values[s.key] != undefined)
          .map(r => {
            var row = r.data.values[s.key];
            if (row == undefined)
              return undefined;

            var key = keyColumn.getKey(r.data.rowValue);
            var rowByKey = rowsByKey[key];

            const totalCount = stackedSeries.sum(s => rowByKey.values[s.key]?.value ?? 0);

            var active = detector?.(row.rowClick);
            
            return (
              <g className="hover-group" key={key}>
                <rect className="shape sf-transition hover-target"
                  transform={translate(x(r[0])!, y(key)! + bandMargin) + (initialLoad ? scale(0, 1) : scale(1, 1))}
                  opacity={active == false ? .5 : undefined}
                  fill={colorByKey[s.key] ?? color(s.key)}
                  height={y.bandwidth() - bandMargin * 2}
                  width={x(r[1])! - x(r[0])!}
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
                {y.bandwidth() > 15 && parseFloat(parameters["NumberOpacity"]) > 0 &&
                  <TextIfFits className="number-label sf-transition"
                    transform={translate(
                      x(r[0])! * 0.5 + x(r[1])! * 0.5,
                      y(keyColumn.getKey(r.data.rowValue))! + y.bandwidth() / 2
                    )}
                    maxWidth={x(r[1])! - x(r[0])!}
                    onClick={e => onDrillDown(r.data.values[s.key].rowClick, e)}
                    fill={parameters["NumberColor"]}
                    dominantBaseline="middle"
                    opacity={parameters["NumberOpacity"]}
                    textAnchor="middle"
                    fontWeight="bold">
                    {pValueAsPercent == "Yes"
                      ? totalCount > 0 ? (row.value / totalCount).toLocaleString(undefined, { style: 'percent', minimumFractionDigits: 0 }) : '0%'
                      : r.data.values[s.key].valueNiceName}
                    <title>
                      {pValueAsPercent == "Yes"
                        ? totalCount > 0 ? (row.value / totalCount).toLocaleString(undefined, { style: 'percent', minimumFractionDigits: 0 }) : '0%'
                        : r.data.values[s.key].valueTitle}
                    </title>
                  </TextIfFits>
                }
              </g>
            );
          }).notNull()}

      </g>)}

      {y.bandwidth() > 15 && pivot.columns.length > 0 && (
        parameters["Labels"] == "Margin" ?
          <g className="y-label" transform={translate(xRule.end('labels'), yRule.start('content') + y.bandwidth() / 2)}>
            {keyValues.map(k => {
              var key = keyColumn.getKey(k);
              var active = detector?.({ c0: k });
              return (
                <TextEllipsis key={key}
                  maxWidth={xRule.size('labels')}
                  className="y-label sf-transition sf-pointer"
                  onClick={e => onDrillDown({ c0: k }, e)}
                  opacity={active == false ? .5 : undefined}
                  fontWeight={active == true ? "bold" : undefined}
                  y={y(key)!}
                  dominantBaseline="middle"
                  textAnchor="end">
                  {keyColumn.getNiceName(k)}
                </TextEllipsis>
              );
            })}
          </g> :
          parameters["Labels"] == "Inside" ?
            <g className="y-axis-tick-label" transform={translate(xRule.start('content'), yRule.start('content') + y.bandwidth() / 2)}>
              {keyValues.map((k, i) => {
                var key = keyColumn.getKey(k);
                var row = rowsByKey[key];
                var posx = row == null ? 0 : x(stackedSeries[stackedSeries.length - 1][pivot.rows.indexOf(row)][1])!;

                var active = detector?.({ c0: k });
                return (<TextEllipsis key={key}
                  transform={translate(posx, y(key)!)}
                  maxWidth={size - posx}
                  onClick={e => onDrillDown({ c0: k }, e)}
                  opacity={active == false ? .5 : undefined}
                  fontWeight={active == true ? "bold" : undefined}
                  className="y-axis-tick-label sf-chart-strong sf-transition sf-pointer"
                  dx={labelsPadding}
                  textAnchor="start"
                  fill={'#000'}
                  dominantBaseline="middle">
                  {keyColumn.getNiceName(k)}
                </TextEllipsis>);
              })}
            </g> : null
      )}

      <Legend pivot={pivot} xRule={xRule} yRule={yRule} color={color} isActive={c.c1 && detector && (row => detector!({ c1: row.value }))} onDrillDown={c.c1 && ((s, e) => onDrillDown({ c1: s.value }, e))} />

      <InitialMessage data={data} x={xRule.middle("content")} y={yRule.middle("content")} loading={loading} />
      <XAxis xRule={xRule} yRule={yRule} />
      <YAxis xRule={xRule} yRule={yRule} />
    </svg>
  );
}
