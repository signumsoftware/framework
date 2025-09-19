import * as React from 'react'
import * as d3 from 'd3'
import * as ChartUtils from './Components/ChartUtils';
import { translate, scale, rotate, skewX, skewY, matrix, scaleFor } from './Components/ChartUtils';
import { PivotRow, groupedPivotTable, toPivotTable } from './Components/PivotTable';
import { ChartClient, ChartTable, ChartColumn, ChartRow, ChartScriptProps } from '../ChartClient';
import { XKeyTicks, XScaleTicks, YScaleTicks } from './Components/Ticks';
import Legend from './Components/Legend';
import { XAxis, YAxis } from './Components/Axis';
import { Rule } from './Components/Rule';
import InitialMessage from './Components/InitialMessage';


export default function renderStackedLines({ data, width, height, parameters, loading, onDrillDown, initialLoad, chartRequest, memo, dashboardFilter }: ChartScriptProps): React.ReactElement<any> {

  var xRule = Rule.create({
    _1: 5,
    title: 15,
    _2: 10,
    labels: parseInt(parameters["HorizontalMargin"]),
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

  var pValueAsPercent = parameters.ValueAsPercent;

  var pivot = c.c1 == null ?
    toPivotTable(data, c.c0!, [c.c2, c.c3, c.c4, c.c5, c.c6].filter(cn => cn != undefined) as ChartColumn<number>[]) :
    groupedPivotTable(data, c.c0!, c.c1, c.c2 as ChartColumn<number>);

  var hasHorizontalScale = parameters["HorizontalScale"] != "Bands";

  var keyValues: unknown[] = ChartUtils.completeValues(keyColumn, pivot.rows.map(r => r.rowValue), parameters['CompleteValues'], chartRequest.filterOptions, ChartUtils.insertPoint(keyColumn, valueColumn0));

  var x = hasHorizontalScale ?
    scaleFor(keyColumn, data.rows.map(r => keyColumn.getValue(r) as number), 0, xRule.size('content'), parameters["HorizontalScale"]) :
    d3.scaleBand()
    .domain(keyValues.map(v => keyColumn.getKey(v)))
    .range([0, xRule.size('content')]);

  var pStack = parameters["Stack"];

  var rowsByKey = pivot.rows.toObject(r => keyColumn.getKey(r.rowValue));

  var stack = d3.stack<unknown>()
    .offset(ChartUtils.getStackOffset(pStack)!)
    .order(ChartUtils.getStackOrder(parameters["Order"])!)
    .keys(pivot.columns.map(d => d.key))
    .value((r, k) => rowsByKey[keyColumn.getKey(r)]?.values[k]?.value ?? 0);

  var stackedSeries = stack(keyValues);

  var max = d3.max(stackedSeries, s => d3.max(s, v => v[1]))!;
  var min = d3.min(stackedSeries, s => d3.min(s, v => v[0]))!;

  var y = d3.scaleLinear()
    .domain([min, max])
    .range([0, yRule.size('content')]);

  var color = ChartUtils.colorCategory(parameters, pivot.columns.map(s => s.key), memo);
  var colorByKey = pivot.columns.toObject(a => a.key, a => a.color);

  var pInterpolate = parameters["Interpolate"];

  const getX: (row: d3.SeriesPoint<unknown>) => number =
    hasHorizontalScale ?
      (row => (x as d3.ScaleContinuousNumeric<number, number>)(row.data as number)) :
      (row => (x as d3.ScaleBand<string>)(keyColumn.getKey(row.data))!); 

  var area = d3.area<d3.SeriesPoint<unknown>>()
    .x(v => getX(v)!)
    .y0(v => -y(v[0])!)
    .y1(v => -y(v[1])!)
    .curve(ChartUtils.getCurveByName(pInterpolate) as d3.CurveFactory);

  var columnsByKey = pivot.columns.toObject(a => a.key);

  var format = pStack == "expand" ? d3.format(".0%") :
    pStack == "zero" ? valueColumn0.getNiceName :
      (n: number) => valueColumn0.getNiceName(n) + "?";;

  var rectRadious = 2;

  var detector = ChartClient.getActiveDetector(dashboardFilter, chartRequest);
  var bw = hasHorizontalScale ? 0 : (x as d3.ScaleBand<string>).bandwidth();

  return (
    <svg direction="ltr" width={width} height={height}>
      
      {hasHorizontalScale ?
        <XScaleTicks xRule={xRule} yRule={yRule} valueColumn={keyColumn as ChartColumn<number>} x={x as d3.ScaleContinuousNumeric<number, number>} /> :
        <XKeyTicks xRule={xRule} yRule={yRule} keyValues={keyValues} keyColumn={keyColumn} x={x as d3.ScaleBand<string>} isActive={detector && (val => detector!({ c0: val }))} onDrillDown={(v, e) => onDrillDown({ c0: v }, e)} />
      }
        <g opacity={dashboardFilter ? .5 : undefined}>
        <YScaleTicks xRule={xRule} yRule={yRule} valueColumn={valueColumn0} y={y} format={format} />
      </g>
      {stackedSeries.orderBy(s => s.key).map(s => <g key={s.key} opacity={dashboardFilter && !(c.c1 && detector?.({ c1: columnsByKey[s.key].value }) == true) ? .5 : undefined} className="shape-serie"
        transform={translate(xRule.start('content') + bw / 2, yRule.end('content'))}>
        <path className="shape sf-transition" fill={colorByKey[s.key] ?? color(s.key)} shapeRendering="initial" d={area(s)!} transform={(initialLoad ? scale(1, 0) : scale(1, 1))}>
          <title>
            {columnsByKey[s.key].niceName!}
          </title>
        </path>
      </g>)}

      {stackedSeries.orderBy(s => s.key).map((s) => <g key={s.key} className="hover-trigger-serie"
        transform={translate(xRule.start('content') + bw / 2, yRule.end('content'))}>

        {s.orderBy(v => keyColumn.getKey(v.data))
          .map(v => {
            var dataKey = keyColumn.getKey(v.data);
            var row = rowsByKey[dataKey]?.values[s.key];
            if (row == undefined)
              return null;

            var rowByKey = rowsByKey[dataKey];

            const totalCount = stackedSeries.sum(s => rowByKey.values[s.key]?.value ?? 0);

            if ((y(v[1])! - y(v[0])!)! <= 10)
              return null;

            var active = detector?.(row.rowClick);

            return (
              <g className="hover-group" key={dataKey} >
                <rect className="point sf-transition hover-target"
                  transform={translate(getX(v)! - rectRadious, -y(v[1])!)}
                  width={2 * rectRadious}
                  fillOpacity={active == true ? undefined : .2}
                  fill={active == true ? "var(--bs-body-color)" : colorByKey[s.key] ?? color(s.key)}
                  height={y(v[1])! - y(v[0])!}
                  onClick={e => onDrillDown(row.rowClick, e)}
                  cursor="pointer">
                  <title>
                    {row.valueTitle}
                  </title>
                </rect>

                {(bw > 15 || hasHorizontalScale) && parseFloat(parameters["NumberOpacity"]) > 0 &&
                  <TextRectangle className="number-label sf-transition"
                    rectangleAtts={{
                      fill: active == true ? "var(--bs-body-color)" : colorByKey[s.key] ?? color(s.key),
                      opacity: active == false ? .5 : parameters["NumberOpacity"],
                      stroke: active == true ? "var(--bs-body-color)" : "none",
                      strokeWidth: active == true ? 2 : undefined,
                      className: "hover-target"
                    }}
                    transform={translate(getX(v)!, -y(v[1])! * 0.5 - y(v[0])! * 0.5)}
                    fill={parameters["NumberColor"]}
                    dominantBaseline="middle"
                    onClick={e => onDrillDown(row.rowClick, e)}
                    textAnchor="middle"
                    fontWeight="bold">
                    {pValueAsPercent == "Yes"
                      ? totalCount > 0 ? (row.value / totalCount).toLocaleString(undefined, { style: 'percent', minimumFractionDigits: 0 }) : '0%'
                      : row.valueNiceName}
                    <title>
                      {pValueAsPercent == "Yes"
                        ? totalCount > 0 ? (row.value / totalCount).toLocaleString(undefined, { style: 'percent', minimumFractionDigits: 0 }) : '0%'
                        : row.valueTitle}
                    </title>
                  </TextRectangle>
                }

              </g>
            );
          })}

      </g>
      )}
      <Legend pivot={pivot} xRule={xRule} yRule={yRule} color={color} isActive={c.c1 && detector && (row => detector!({ c1: row.value }))} onDrillDown={c.c1 && ((s, e) => onDrillDown({ c1: s.value }, e))} />

      <g opacity={dashboardFilter ? .5 : undefined}>
        <XAxis xRule={xRule} yRule={yRule} />
        <YAxis xRule={xRule} yRule={yRule} />
      </g>
    </svg>
  );
}


export interface TextRectangleProps extends React.SVGProps<SVGTextElement> {
  rectangleAtts?: React.SVGProps<SVGRectElement>;
}


export function TextRectangle({ rectangleAtts, children, ...atts }: TextRectangleProps): React.JSX.Element {

  const txt = React.useRef<SVGTextElement>(null);
  const rect = React.useRef<SVGRectElement>(null);

  React.useEffect(() => {
    if (rect.current) {

      var bbox = txt.current!.getBoundingClientRect();

      rect.current.setAttribute("width", bbox.width + 4 + "px");
      rect.current.setAttribute("x", -(bbox.width + 4) / 2 + "px");
      rect.current.setAttribute("height", bbox.height + "px");
      rect.current.setAttribute("y", -(bbox.height / 2) - 2 + "px");
    }


  }, [getString(children)]);


  return (
    <>
      <rect ref={rect} {...rectangleAtts} transform={atts.transform} height={20} />
      <text ref={txt} {...atts} >
        {children ?? ""}
      </text>
    </>
  );
}


function getString(children: React.ReactNode) {
  return React.Children.toArray(children)[0] as string;
}
