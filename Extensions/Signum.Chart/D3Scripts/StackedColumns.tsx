import * as React from 'react'
import * as d3 from 'd3'
import * as ChartUtils from './Components/ChartUtils';
import { translate, scale, rotate, skewX, skewY, matrix, scaleFor } from './Components/ChartUtils';
import { PivotRow, toPivotTable, groupedPivotTable } from './Components/PivotTable';
import { ChartClient, ChartScriptProps, ChartTable, ChartColumn } from '../ChartClient';
import { XKeyTicks, YScaleTicks, XTitle } from './Components/Ticks';
import Legend from './Components/Legend';
import { XAxis, YAxis } from './Components/Axis';
import TextEllipsis from './Components/TextEllipsis';
import { Rule } from './Components/Rule';
import InitialMessage from './Components/InitialMessage';
import TextIfFits from './Components/TextIfFits';


export default function renderStackedColumns({ data, width, height, parameters, loading, onDrillDown, initialLoad, chartRequest, memo, dashboardFilter }: ChartScriptProps): React.ReactElement<any> {

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

  var labelsPadding = 5;
  var labelsMargin = parseInt(parameters["LabelsMargin"]);

  var yRule = Rule.create({
    _1: 10,
    legend: 15,
    _2: 5,
    _labelTopMargin: parameters["Labels"] == "Inside" ? labelsPadding + labelsMargin : 0,
    content: '*',
    ticks: 4,
    _3: parameters["Labels"] == "Margin" ? labelsPadding : 0,
    labels: parameters["Labels"] == "Margin" ? labelsMargin : 0,
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

  var x = d3.scaleBand()
    .domain(keyValues.map(v => keyColumn.getKey(v)))
    .range([0, xRule.size('content')]);

  var pStack = parameters["Stack"];

  var stack = d3.stack<PivotRow>()
    .offset(ChartUtils.getStackOffset(pStack)!)
    .order(ChartUtils.getStackOrder(parameters["Order"])!)
    .keys(pivot.columns.map(d => d.key))
    .value((r, k) => r.values[k]?.value ?? 0);

  var stackedSeries = stack(pivot.rows);

  var rowsByKey = pivot.rows.toObject(r => keyColumn.getKey(r.rowValue));

  var max = d3.max(stackedSeries, s => d3.max(s, vs => vs[1]))!;
  var min = d3.min(stackedSeries, s => d3.min(s, vs => vs[0]))!;

  var y = d3.scaleLinear()
    .domain([min, max])
    .range([0, yRule.size('content')]);

  var rowsInOrder = pivot.rows.orderBy(r => keyColumn.getKey(r.rowValue));
  var color = ChartUtils.colorCategory(parameters, pivot.columns.map(c => c.key), memo);
  var colorByKey = pivot.columns.toObject(a => a.key, a => a.color);

  var format = pStack == "expand" ? d3.format(".0%") :
    pStack == "zero" ? valueColumn0.getNiceName :
      (n: number) => valueColumn0.getNiceName(n) + "?";

  var size = yRule.size('content') + yRule.size("_labelTopMargin");

  var detector = ChartClient.getActiveDetector(dashboardFilter, chartRequest);

  const bandMargin = x.bandwidth() > 20 ? 2 : x.bandwidth() > 10 ? 1 : 0;

  return (
    <svg direction="ltr" width={width} height={height}>
      <g opacity={dashboardFilter ? .5 : undefined}>
        <XTitle xRule={xRule} yRule={yRule} keyColumn={keyColumn} />
        <YScaleTicks xRule={xRule} yRule={yRule} valueColumn={valueColumn0} y={y} format={format} />
      </g>
      {stackedSeries.orderBy(s => s.key).map(s => <g key={s.key} className="shape-serie"
        transform={translate(xRule.start('content'), yRule.end('content'))} >

        {s.map(r => {
          var row = r.data.values[s.key];
          if (row == undefined)
            return undefined;

          var key = keyColumn.getKey(r.data.rowValue);
          var rowByKey = rowsByKey[key];

          const totalCount = stackedSeries.sum(s => rowByKey.values[s.key]?.value ?? 0);

          var active = detector?.(row.rowClick);

          return (
            <g className="hover-group" key={keyColumn.getKey(r.data.rowValue)}>
              <rect className="shape sf-transition hover-target"
                transform={translate(x(keyColumn.getKey(r.data.rowValue))! + bandMargin, -y(r[1])!) + (initialLoad ? scale(1, 0) : scale(1, 1))}
                opacity={active == false ? .5 : undefined}
                fill={colorByKey[s.key] ?? color(s.key)}
                width={x.bandwidth() - bandMargin * 2}
                height={y(r[1])! - y(r[0])!}
                onClick={e => onDrillDown(row.rowClick, e)}
                cursor="pointer">
                <title>
                  {row.valueTitle}
                </title>
              </rect>
              {parseFloat(parameters["NumberOpacity"]) > 0 && x.bandwidth() > 15 &&
                <TextIfFits className="number-label sf-transition"
                  maxWidth={y(r[1])! - y(r[0])!}
                  transform={translate(
                    x(keyColumn.getKey(r.data.rowValue))! + x.bandwidth() / 2,
                    -y(r[0])! * 0.5 - y(r[1])! * 0.5
                  ) + rotate(-90)}
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
                </TextIfFits>}
            </g>
          );
        }).notNull()}

      </g>)}

      {x.bandwidth() > 15 && (
        parameters["Labels"] == "Margin" ?
          <g className="x-label" transform={translate(xRule.start('content'), yRule.start('labels'))}>
            {keyValues.map(k => {

              var key = keyColumn.getKey(k);

              var active = detector?.({ c0: k });

              return (
                <TextEllipsis key={key}
                  maxWidth={yRule.size('labels')}
                  className="x-label sf-transition sf-pointer"
                  onClick={e => onDrillDown({ c0: k }, e)}
                  opacity={active == false ? .5 : undefined}
                  fontWeight={active == true ? "bold" : undefined}
                  transform={translate(x(key)! + x.bandwidth() / 2, 0) + rotate(-90)}
                  dominantBaseline="middle"
                  fill="var(--bs-body-color)"
                  shapeRendering="geometricPrecision"
                  textAnchor="end">
                  {keyColumn.getNiceName(k)}
                </TextEllipsis>
              );
            })}
          </g> :
          parameters["Labels"] == "Inside" ?
            <g className="x-label" transform={translate(xRule.start('content'), yRule.end('content'))}>
              {keyValues.map((k, i) => {
                var dataKey = keyColumn.getKey(k);
                var row = rowsByKey[dataKey];
                var posy = row == null ? 0 :  y(stackedSeries[stackedSeries.length - 1][pivot.rows.indexOf(row)][1])!;

                var active = detector?.({ c0: k });

                return (<TextEllipsis key={keyColumn.getKey(k)}
                  maxWidth={size - posy}
                  onClick={e => onDrillDown({ c0: k }, e)}
                  opacity={active == false ? .5 : undefined}
                  fontWeight={active == true ? "bold" : undefined}
                  className="x-label sf-transition sf-pointer"
                  transform={translate(x(keyColumn.getKey(k))! + x.bandwidth() / 2,  -posy) + rotate(-90)}
                  dominantBaseline="middle"
                  fill={'#000'}
                  dx={labelsPadding}
                  textAnchor="start">
                  {keyColumn.getNiceName(k)}
                </TextEllipsis>);
              })}
            </g> : undefined
      )}

      <Legend pivot={pivot} xRule={xRule} yRule={yRule} color={color} isActive={c.c1 && detector && (row => detector!({ c1: row.value }))} onDrillDown={c.c1 && ((s, e) => onDrillDown({ c1: s.value }, e))} />

      <InitialMessage data={data} x={xRule.middle("content")} y={yRule.middle("content")} loading={loading} />
      <g opacity={dashboardFilter ? .5 : undefined}>
        <XAxis xRule={xRule} yRule={yRule} />
        <YAxis xRule={xRule} yRule={yRule} />
      </g>
    </svg>
  );
}
