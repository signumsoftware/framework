import * as React from 'react'
import * as d3 from 'd3'
import * as ChartUtils from './Components/ChartUtils';
import { translate, scale, rotate, skewX, skewY, matrix, scaleFor } from './Components/ChartUtils';
import { ChartTable, ChartColumn, ChartRow, ChartScriptProps } from '../ChartClient';
import { KeyCodes } from '@framework/Components';
import TextEllipsis from './Components/TextEllipsis';
import { XKeyTicks, XScaleTicks, YScaleTicks } from './Components/Ticks';
import { XAxis, YAxis } from './Components/Axis';
import { Rule } from './Components/Rule';
import InitialMessage from './Components/InitialMessage';
import { MemoRepository } from './Components/ReactChart';
import { ChartColumnType } from 'Chart/Signum.Entities.Chart';

export default function renderLine({ data, width, height, parameters, loading, chartRequest, onDrillDown, initialLoad, memo, dashboardFilter }: ChartScriptProps): React.ReactElement<any> {


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
    _2: parseFloat(parameters["NumberOpacity"]) > 0 ? 20 : 10,
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

  var keyColumn = data.columns.c0! as ChartColumn<unknown>;
  var valueColumn = data.columns.c1! as ChartColumn<number>;

  var hasHorizontalScale = parameters["HorizontalScale"] != "Bands";


  var keyValues = ChartUtils.completeValues(keyColumn, data.rows.map(r => keyColumn.getValue(r)), parameters['CompleteValues'], chartRequest.filterOptions, ChartUtils.insertPoint(keyColumn, valueColumn));
  var x = hasHorizontalScale ?
    scaleFor(keyColumn, data.rows.map(r => keyColumn.getValue(r) as number), 0, xRule.size('content'), parameters["HorizontalScale"]) :
    d3.scaleBand()
      .domain(keyValues.map(v => keyColumn.getKey(v)))
      .range([0, xRule.size('content')]);

  var y = scaleFor(valueColumn, data.rows.map(r => valueColumn.getValue(r)), 0, yRule.size('content'), parameters["VerticalScale"]);

  var detector = dashboardFilter?.getActiveDetector(chartRequest);

  return (
    <svg direction="ltr" width={width} height={height}>
      {hasHorizontalScale ?
        <XScaleTicks xRule={xRule} yRule={yRule} valueColumn={keyColumn as ChartColumn<number>} x={x as d3.ScaleContinuousNumeric<number, number>} /> :
        <XKeyTicks xRule={xRule} yRule={yRule} keyValues={keyValues} keyColumn={keyColumn} x={x as d3.ScaleBand<string>} showLines={(x as d3.ScaleBand<string>).bandwidth() > 5}
          isActive={detector && (val => detector!({ c0: val }))} onDrillDown={(v, e) => onDrillDown({ c0: v }, e)} />
      }
      <g opacity={dashboardFilter ? .5 : undefined}>
        <YScaleTicks xRule={xRule} yRule={yRule} valueColumn={valueColumn} y={y}  />
      </g>

      {paintLine({ xRule, yRule, x, y, keyValues, data, parameters, hasHorizontalScale, onDrillDown, initialLoad, memo, detector })}

      <InitialMessage data={data} x={xRule.middle("content")} y={yRule.middle("content")} loading={loading} />
      <g opacity={dashboardFilter ? .5 : undefined}>
        <XAxis xRule={xRule} yRule={yRule} />
        <YAxis xRule={xRule} yRule={yRule} />
      </g>
    </svg>
  );
}

export interface ChartScriptHorizontalProps {
  xRule: Rule<"content">;
  yRule: Rule<"content" | "labels">;
  hasHorizontalScale: boolean;
  x: d3.ScaleBand<string> | d3.ScaleContinuousNumeric<number, number>;
  y: d3.ScaleContinuousNumeric<number, number>;
  keyValues: unknown[];
  data: ChartTable;
  parameters: { [name: string]: string },
  onDrillDown: (row: ChartRow, e: React.MouseEvent<any> | MouseEvent) => void;
  initialLoad: boolean;
  memo: MemoRepository;
  detector?: (row: ChartRow) => boolean;
}

export function paintLine({ xRule, yRule, x, y, keyValues, data, hasHorizontalScale, parameters, onDrillDown, initialLoad, detector }: ChartScriptHorizontalProps) {

  var keyColumn = data.columns.c0! as ChartColumn<unknown>;
  var valueColumn = data.columns.c1! as ChartColumn<number>;

  var orderedRows = data.rows.orderBy(r => keyColumn.getValueKey(r)).filter(a => valueColumn.getValue(a) != null);

  var rowByKey = data.rows.toObject(r => keyColumn.getValueKey(r));

  const getX: (row: ChartRow) => number =
    hasHorizontalScale ?
      (row => (x as d3.ScaleContinuousNumeric<number, number>)(keyColumn.getValue(row) as number)) :
      (row => (x as d3.ScaleBand<string>)(keyColumn.getValueKey(row))!); 

  var line = d3.line<unknown>()
    .defined(key => rowByKey[keyColumn.getKey(key)] != null)
    .x(key => getX(rowByKey[keyColumn.getKey(key)])!)
    .y(key => -y(valueColumn.getValue(rowByKey[keyColumn.getKey(key)]))!)
    .curve(ChartUtils.getCurveByName(parameters["Interpolate"]!)!);//"linear"

  var color = parameters["Color"]!;// 'steelblue'
  var circleRadius = parseFloat(parameters["CircleRadius"]!);
  var circleStroke = parseFloat(parameters["CircleStroke"]!);
  var circleRadiusHover = parseFloat(parameters["CircleRadiusHover"]!);

  var bw = hasHorizontalScale ? 0 : (x as d3.ScaleBand<string>).bandwidth();

  if (!hasHorizontalScale) {
    if (parameters["CircleAutoReduce"]! == "Yes") {

      if (circleRadius > bw / 3)
        circleRadius = bw / 3;

      if (circleRadiusHover > bw / 2)
        circleRadiusHover = bw / 2;

      if (circleStroke > bw / 8)
        circleStroke = bw / 8;
    }

    var numberOpacity = parseFloat(parameters["NumberOpacity"]!);
    if (numberOpacity > 0 && bw < parseFloat(parameters["NumberMinWidth"]!))
      numberOpacity = 0;
  }


 
  return (
    <>
      {/*PAINT CHART'*/}
      <g className="shape" transform={translate(xRule.start('content') + (bw / 2), yRule.end('content'))}>
        <path className="shape sf-transition" stroke={color} fill="none" strokeWidth={3} shapeRendering="initial" d={line(keyValues)!} transform={initialLoad ? scale(1, 0) : scale(1, 1)} />
      </g>

      {/*paint graph - hover area trigger*/}
      {circleRadiusHover > 0 && <g className="hover-trigger" transform={translate(xRule.start('content') + (bw / 2), yRule.end('content'))}>
        {orderedRows
          .map(r => {
            var key = keyColumn.getValueKey(r);
            return (
              <circle key={key}
                transform={translate(getX(r)!, -y(valueColumn.getValue(r))!)}
                className="hover-trigger"
                fill="#fff"
                fillOpacity={0}
                stroke="none"
                cursor="pointer"
                r={circleRadiusHover}
                onClick={e => onDrillDown(r, e)}>
                <title>
                  {keyColumn.getValueNiceName(r) + ': ' + valueColumn.getValueNiceName(r)}
                </title>
              </circle>
            );
          })}
      </g>
      }

      {/*paint graph - points*/}
      {circleRadius > 0 && circleStroke > 0 && <g className="point sf-transition" transform={translate(xRule.start('content') + (bw / 2), yRule.end('content')) + (initialLoad ? scale(1, 0) : scale(1, 1))}>
        {orderedRows
          .map(r => {
            var key = keyColumn.getValueKey(r);
            var row = rowByKey[key];
            var active = detector?.(row);
            return (
              <g className="hover-group" key={key}>
                <circle
                  transform={translate(getX(r)!, -y(valueColumn.getValue(r))!)}
                  className="point sf-transition hover-target"
                  opacity={active == false ? .5 : undefined}
                  stroke={active == true ? "black" : color}
                  strokeWidth={active == true ? 3 : circleStroke}
                  fill="white"
                  r={circleRadius}
                  onClick={e => onDrillDown(row, e)}
                  cursor="pointer"
                  shapeRendering="initial">
                  <title>
                    {keyColumn.getValueNiceName(r) + ': ' + valueColumn.getValueNiceName(r)}
                  </title>
                </circle>
                { /*Point labels*/
                  numberOpacity > 0 &&
                  <g className="point-label" >
                    <text transform={translate(getX(r)!, -y(valueColumn.getValue(r))! - 10)}
                      className="point-label sf-transition"
                      r={5}
                      opacity={active == false ? .5 : active == true ? 1 : numberOpacity}
                      textAnchor="middle"
                      onClick={e => onDrillDown(r, e)}
                      cursor="pointer"
                      shapeRendering="initial">
                      {valueColumn.getValueNiceName(r)}
                    </text>)
                  </g>
                }

              </g>
            );
          })}
      </g>
      }
    </>
  );
}
