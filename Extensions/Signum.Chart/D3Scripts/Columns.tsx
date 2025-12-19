import * as React from 'react'
import * as d3 from 'd3'
import * as ChartUtils from './Components/ChartUtils';
import { translate, scale, rotate, skewX, skewY, matrix, scaleFor } from './Components/ChartUtils';
import { ChartClient, ChartTable, ChartColumn,  ChartRow, ChartScriptProps } from '../ChartClient';
import TextEllipsis from './Components/TextEllipsis';
import { XKeyTicks, YScaleTicks, XTitle } from './Components/Ticks';
import { XAxis, YAxis } from './Components/Axis';
import { Rule } from './Components/Rule';
import InitialMessage from './Components/InitialMessage';
import type { ChartScriptHorizontalProps } from './Line';
import TextIfFits from './Components/TextIfFits';
import { ChartMessage, D3ChartScript } from '../Signum.Chart';
import { symbolNiceName, getQueryNiceName } from '@framework/Reflection';


export default function renderColumns({ data, width, height, parameters, loading, onDrillDown, initialLoad, chartRequest, memo, dashboardFilter }: ChartScriptProps): React.ReactElement<any> {

  const isMargin = parameters["Labels"] == "Margin" || parameters["Labels"] == "MarginAll";
  const isInside = parameters["Labels"] == "Inside" || parameters["Labels"] == "InsideAll";

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
    _labelTopMargin: isInside ? labelsPadding + labelsMargin : 0,
    content: '*',
    ticks: 4,
    _3: isMargin ? labelsPadding : 0,
    labels: isMargin ? labelsMargin : 0,
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

  var keyColumn = data.columns.c0!;
  var valueColumn = data.columns.c1! as ChartColumn<number>;

  var keyValues = ChartUtils.completeValues(keyColumn, data.rows.map(r => keyColumn.getValue(r)), parameters['CompleteValues'], chartRequest.filterOptions, ChartUtils.insertPoint(keyColumn, valueColumn));

  var x = d3.scaleBand()
    .domain(keyValues.map(v => keyColumn.getKey(v)))
    .range([0, xRule.size('content')]);

  var y = scaleFor(valueColumn, data.rows.map(r => valueColumn.getValue(r)), 0, yRule.size('content'), parameters["Scale"]);

  var detector = ChartClient.getActiveDetector(dashboardFilter, chartRequest);

  return (
    <svg
      direction="ltr"
      width={width}
      height={height}
      role="img">
      <title id="columnChartTitle">{ChartMessage._0Of1_2.niceToString(symbolNiceName(D3ChartScript.Columns), getQueryNiceName(chartRequest.queryKey), [valueColumn.title, keyColumn.title].join(", "))}</title>
      <g opacity={dashboardFilter ? .5 : undefined}>
        <XTitle xRule={xRule} yRule={yRule} keyColumn={keyColumn} />
        <YScaleTicks xRule={xRule} yRule={yRule} valueColumn={valueColumn} y={y} />
      </g>

      {paintColumns({ xRule, yRule, x, y, keyValues, data, parameters, hasHorizontalScale: false, initialLoad, onDrillDown, colIndex: 0, colCount: 1, memo, detector })}

      <InitialMessage data={data} x={xRule.middle("content")} y={yRule.middle("content")} loading={loading} />
      <g opacity={dashboardFilter ? .5 : undefined}>
        <XAxis xRule={xRule} yRule={yRule} />
        <YAxis xRule={xRule} yRule={yRule} />
      </g>
    </svg>
  );
}


export function paintColumns({ xRule, yRule, x : x2, y, keyValues, data, parameters, hasHorizontalScale, initialLoad, onDrillDown, colIndex, colCount, memo, detector }: ChartScriptHorizontalProps & {
  colIndex: number, colCount: number
}): React.JSX.Element {

  if (hasHorizontalScale)
    throw new Error("hasHorizontalScale is not supported");

  const x = x2 as d3.ScaleBand<string>;

  var labelsPadding = 5;

  const isMargin = parameters["Labels"] == "Margin" || parameters["Labels"] == "MarginAll";
  const isInside = parameters["Labels"] == "Inside" || parameters["Labels"] == "InsideAll";
  const isAll = parameters["Labels"] == "MarginAll" || parameters["Labels"] == "InsideAll";

  var keyColumn = data.columns.c0!;
  var valueColumn = data.columns.c1! as ChartColumn<number>;

  var orderedRows = data.rows.orderBy(r => keyColumn.getValueKey(r));
  var color = parameters["ForceColor"] ? () => parameters["ForceColor"] :
    ChartUtils.colorCategory(parameters, orderedRows.map(r => keyColumn.getValueKey(r)!), memo);

  var size = yRule.size('content') + (yRule.size("_labelTopMargin" as any) ?? 0);

  const bandMargin = x.bandwidth() > 20 ? 2 : x.bandwidth() > 10 ? 1 : 0;

  const bandwidth = ((x.bandwidth() - bandMargin * 2) / colCount);
  const bandOffset = bandwidth * colIndex + bandMargin;

  var rowsByKey = data.rows.toObject(r => keyColumn.getValueKey(r));


  return (
    <>
      <g className="shape" transform={translate(xRule.start('content') + bandOffset, yRule.end('content'))}>
        {keyValues.map(k => {
          var key = keyColumn.getKey(k);

          var row: ChartRow | undefined = rowsByKey[key];

          var active = detector?.(row);

          var posy: number;
          var height: number;

          const scaleName = parameters["Scale"];
          const value = row ? valueColumn.getValue(row) : 0;

          if (scaleName == "MinZeroMax") {
            posy = value < 0 ? y(0) : y(value);
            height = value < 0 ? y(0) - y(value) : y(value) - y(0);
          }
          else {
            posy = y(value);
            height = y(value);
          }

          return (
            <g className="hover-group" key={key} transform={translate(x(key)!, -posy)}>
              {row && <rect className="shape sf-transition hover-target"
                opacity={active == false ? .5 : undefined}
                transform={initialLoad ? scale(1, 0) : scale(1, 1)}
                height={height}
                width={bandwidth}
                fill={keyColumn.getValueColor(row) ?? color(key)}
                role="button"
                tabIndex={0}
                cursor="pointer"
                onKeyDown={(e: React.KeyboardEvent<SVGRectElement>) => {
                  if (e.key === "Enter" || e.key === " ") {
                    e.preventDefault();
                    (onclick as any)?.(e);
                  }
                }}
                onClick={e => onDrillDown(row!, e)}>
                <title>
                  {keyColumn.getValueNiceName(row) + ': ' + valueColumn.getValueNiceName(row)}
                </title>
              </rect>}
              {bandwidth > 15 && (isAll || row != null) &&
                (isMargin ?
                  <g className="x-label" transform={translate(0, labelsPadding + posy)} >
                    <TextEllipsis maxWidth={yRule.size('labels')} className="x-label sf-transition"
                      transform={translate(bandwidth / 2, 0) + rotate(-90)}
                      dominantBaseline="middle"
                      fontWeight="bold"
                      fill={(keyColumn.getColor(k) ?? color(key))}
                      textAnchor="end"
                      onClick={e => onDrillDown({ c1: k }, e)}>
                      {keyColumn.getNiceName(k)}
                    </TextEllipsis>
                  </g> :
                  isInside ?
                    <g className="x-label" >
                      <TextEllipsis
                        maxWidth={size - posy} className="x-label sf-transition"
                        transform={translate(bandwidth / 2, 0) + rotate(-90)}
                        dominantBaseline="middle"
                        fontWeight="bold"
                        fill={(keyColumn.getColor(k) ?? color(key))}
                        dx={labelsPadding}
                        textAnchor={'start'}
                        onClick={e => onDrillDown({ c0: k }, e)}>
                        {keyColumn.getNiceName(k)}
                      </TextEllipsis>
                    </g> : null
                )}
              {parseFloat(parameters["NumberOpacity"]) > 0 && bandwidth > 15 && row &&
                <g className="numbers-label" >
                  <TextIfFits className="number-label sf-transition"
                    transform={translate(bandwidth / 2, height / 2)}
                    maxWidth={height}
                    fill={parameters["NumberColor"] ?? "#000"}
                    dominantBaseline="middle"
                    opacity={parameters["NumberOpacity"]}
                    textAnchor="middle"
                    fontWeight="bold"
                    onClick={e => onDrillDown(row!, e)}>
                    {valueColumn.getValueNiceName(row)}
                  </TextIfFits>
                </g>}
            </g>
          );
        })}
      </g>
    </>
  );
}
