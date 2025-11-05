import * as React from 'react'
import * as d3 from 'd3'
import * as ChartUtils from './Components/ChartUtils';
import { translate, scale, rotate, skewX, skewY, matrix, scaleFor } from './Components/ChartUtils';
import { PivotRow, toPivotTable, groupedPivotTable } from './Components/PivotTable';
import { ChartClient, ChartTable, ChartColumn, ChartScriptProps } from '../ChartClient';
import Legend from './Components/Legend';
import TextEllipsis from './Components/TextEllipsis';
import { XScaleTicks, YKeyTicks } from './Components/Ticks';
import { XAxis, YAxis } from './Components/Axis';
import { Rule } from './Components/Rule';
import InitialMessage from './Components/InitialMessage';
import TextIfFits from './Components/TextIfFits';
import { ChartMessage, D3ChartScript } from '../Signum.Chart';
import { symbolNiceName, getQueryNiceName } from '@framework/Reflection';


export default function renderMultiBars({ data, width, height, parameters, loading, onDrillDown, initialLoad, chartRequest, memo, dashboardFilter }: ChartScriptProps): React.ReactElement<any> {

  var xRule = Rule.create({
    _1: 5,
    title: 15,
    _2: 10,
    labels: parseInt(parameters["LabelMargin"]),
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

  var pivot = c.c1 == null ?
    toPivotTable(data, c.c0!, [c.c2, c.c3, c.c4, c.c5, c.c6].filter(cn => cn != undefined) as ChartColumn<number>[]) :
    groupedPivotTable(data, c.c0!, c.c1, c.c2 as ChartColumn<number>);

  var allValues = pivot.rows.flatMap(r => pivot.columns.map(function (c) { return r.values[c.key] && r.values[c.key].value; }));

  var x = scaleFor(valueColumn0, allValues, 0, xRule.size('content'), parameters["Scale"]);

  var keyValues = ChartUtils.completeValues(keyColumn, pivot.rows.map(r => r.rowValue), parameters['CompleteValues'], chartRequest.filterOptions, ChartUtils.insertPoint(keyColumn, valueColumn0));

  var y = d3.scaleBand()
    .domain(keyValues.map(v => keyColumn.getKey(v)))
    .range([0, yRule.size('content')]);

  var interMagin = 2;

  var columnsInOrder = pivot.columns.orderBy(a => a.key);
  var rowsInOrder = pivot.rows.orderBy(r => keyColumn.getKey(r.rowValue));
  var color = ChartUtils.colorCategory(parameters, columnsInOrder.map(s => s.key), memo);

  var ySubscale = d3.scaleBand()
    .domain(pivot.columns.map(s => s.key))
    .range([interMagin, y.bandwidth() - interMagin]);

  var detector = ChartClient.getActiveDetector(dashboardFilter, chartRequest);

  return (
    <svg direction="ltr" width={width} height={height} role="img">
      <title id="multiBarsChartTitle">{ChartMessage._0Of1_2Per3.niceToString(symbolNiceName(D3ChartScript.MultiBars), getQueryNiceName(chartRequest.queryKey), [keyColumn.title, valueColumn0.title].join(", "), [c.c1, c.c3, c.c4, c.c5, c.c6].filter(cn => cn != undefined).map(cn => cn.title).join(", "))}</title>
      <g opacity={dashboardFilter ? .5 : undefined}>
        <XScaleTicks xRule={xRule} yRule={yRule} valueColumn={valueColumn0} x={x} />
      </g>
      <YKeyTicks xRule={xRule} yRule={yRule} keyValues={keyValues} keyColumn={keyColumn} y={y} showLabels={true} isActive={detector && (val => detector!({ c0: val }))} onDrillDown={(v, e) => onDrillDown({ c0: v }, e)} />

      {columnsInOrder.map(s => <g key={s.key} className="shape-serie"
        transform={translate(xRule.start('content'), yRule.end('content'))} >

        {
          rowsInOrder
            .filter(r => r.values[s.key] != undefined)
            .map(r => {
              var row = r.values[s.key];
              if (row == undefined)
                return undefined;

              var active = detector?.(row.rowClick);
              var key = keyColumn.getKey(r.rowValue);

              var posx: number;
              var width: number;

              const scaleName = parameters["Scale"];

              if (scaleName == "MinZeroMax") {
                posx = row.value < 0 ? x(row.value) : x(0);
                width = row.value < 0 ? x(0) - x(row.value) : x(row.value) - x(0);
              }
              else {
                posx = 0;
                width = x(row.value);
              }

              return (
                <g className="hover-group" key={key} transform={translate(posx, -y(key)! - ySubscale(s.key)! - ySubscale.bandwidth())}>
                  <rect className="shape sf-transition hover-target"
                    opacity={active == false ? .5 : undefined}
                    fill={s.color || color(s.key)}
                    transform={initialLoad ? scale(0, 1) : scale(1, 1)}
                    height={ySubscale.bandwidth()}
                    width={width}
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
                  {
                    ySubscale.bandwidth() > 15 && parseFloat(parameters["NumberOpacity"]) > 0 &&
                    <TextIfFits className="number-label sf-transition"
                      maxWidth={width}
                      transform={translate(width / 2, (ySubscale.bandwidth() / 2) + interMagin)}
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
              )
            })
        }

      </g>)}

      <Legend pivot={pivot} xRule={xRule} yRule={yRule} color={color} isActive={c.c1 && detector && (row => detector!({ c1: row.value }))} onDrillDown={c.c1 && ((s, e) => onDrillDown({ c1: s.value }, e))} />

      <InitialMessage data={data} x={xRule.middle("content")} y={yRule.middle("content")} loading={loading} />
      <g opacity={dashboardFilter ? .5 : undefined}>
        <XAxis xRule={xRule} yRule={yRule} />
        <YAxis xRule={xRule} yRule={yRule} />
      </g>
    </svg>
  );
}
