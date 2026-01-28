import * as React from 'react'
import * as d3 from 'd3'
import * as ChartUtils from './Components/ChartUtils';
import { translate, scale, rotate, skewX, skewY, matrix, scaleFor } from './Components/ChartUtils';
import { ChartClient, ChartColumn, ChartRow, ChartScriptProps } from '../ChartClient';
import { XKeyTicks, YScaleTicks, YKeyTicks, XScaleTicks } from './Components/Ticks';
import { XAxis, YAxis } from './Components/Axis';
import { Rule } from './Components/Rule';
import InitialMessage from './Components/InitialMessage';
import TextIfFits from './Components/TextIfFits';
import TextEllipsis from './Components/TextEllipsis';
import { ChartMessage, ChartScriptSymbol, D3ChartScript } from '../Signum.Chart';
import { getQueryNiceName, symbolNiceName } from '@framework/Reflection';


export default function renderBars({ data, width, height, parameters, loading, onDrillDown, initialLoad, chartRequest, memo, dashboardFilter }: ChartScriptProps): React.ReactElement<any> {

  const isMargin = parameters["Labels"] == "Margin" || parameters["Labels"] == "MarginAll";
  const isInside = parameters["Labels"] == "Inside" || parameters["Labels"] == "InsideAll";
  const isAll = parameters["Labels"] == "MarginAll" || parameters["Labels"] == "InsideAll";

  var labelsMargin = parseInt(parameters["LabelsMargin"]);
  var labelsPadding = 5;

  var xRule = Rule.create({
    _1: 5,
    title: 15,
    _2: 10,
    labels: isMargin ? labelsMargin : 0,
    _3: isMargin ? labelsPadding : 0,
    ticks: 4,
    content: '*',
    _4: 5,
  }, width);

  var yRule = Rule.create({
    _1: 5,
    content: '*',
    ticks: 4,
    _2: 5,
    labels: 10,
    _3: 10,
    title: 15,
    _4: 5,
  }, height);

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


  var x = scaleFor(valueColumn, data.rows.map(r => valueColumn.getValue(r)), 0, xRule.size('content') - (isInside ? labelsMargin + labelsPadding : 0), parameters['Scale']);

  var keyValues = ChartUtils.completeValues(keyColumn, data.rows.map(r => keyColumn.getValue(r)), parameters['CompleteValues'], chartRequest.filterOptions, ChartUtils.insertPoint(keyColumn, valueColumn));

  var y = d3.scaleBand()
    .domain(keyValues.map(v => keyColumn.getKey(v)))
    .range([0, yRule.size('content')]);

  var orderedRows = data.rows.orderBy(r => keyColumn.getValueKey(r));
  var color = ChartUtils.colorCategory(parameters, orderedRows.map(r => keyColumn.getValueKey(r)), memo);

  var size = xRule.size('content');
  var labelsPadding = 10;

  var rowsByKey = data.rows.toObject(r => keyColumn.getValueKey(r));

  var detector = ChartClient.getActiveDetector(dashboardFilter, chartRequest);

  const bandMargin = y.bandwidth() > 20 ? 2 : 0;

  return (
    <svg
      direction="ltr"
      width={width}
      height={height}
      role="img">
      <title id="barChartTitle">{ChartMessage._0Of1_2.niceToString(symbolNiceName(D3ChartScript.Bars), getQueryNiceName(chartRequest.queryKey), [valueColumn.title, keyColumn.title].join(", "))}</title>
      <g opacity={dashboardFilter ? .5 : undefined}>
        <XScaleTicks xRule={xRule} yRule={yRule} valueColumn={valueColumn} x={x} />
      </g>
      <YKeyTicks xRule={xRule} yRule={yRule} keyValues={keyValues} keyColumn={keyColumn} y={y} showLabels={false} isActive={detector && (val => detector!({ c0: val }))} onDrillDown={(v, e) => onDrillDown({ c0: v }, e)} />

      {/*PAINT GRAPH*/}
      <g className="shape" transform={translate(xRule.start('content'), yRule.start('content'))}>
        {keyValues.map(k => {

          var key = keyColumn.getKey(k);

          var row: ChartRow | undefined = rowsByKey[key];
          var active = detector?.(row);

          var posx: number;
          var insidex: number;
          var marginx: number;
          var _width: number;

          const scaleName = parameters["Scale"];
          const value = row ? valueColumn.getValue(row) : 0;

          if (scaleName == "MinZeroMax") {
            posx = value < 0 ? x(value) : x(0);
            _width = value < 0 ? x(0) - x(value) : x(value) - x(0);
            insidex = _width;
            marginx = -posx - labelsPadding;
          }
          else {
            posx = 0;
            _width = x(value);
            insidex = _width;
            marginx = -labelsPadding;
          }

          return (
            <g className="hover-group" key={key} transform={translate(posx, y(key)! + bandMargin)}>
              {row && <rect className="shape sf-transition hover-target"
                opacity={active == false ? .5 : undefined}
                transform={(initialLoad ? scale(0, 1) : scale(1, 1))}
                width={_width}
                height={y.bandwidth() - bandMargin * 2}
                fill={keyColumn.getValueColor(row) ?? color(key)}
                onClick={e => onDrillDown(row!, e)}
                role="button"
                tabIndex={0}
                focusable={true}
                cursor="pointer"
                onKeyDown={e => {
                  if (e.key === "Enter" || e.key === " ") {
                   e.preventDefault();
                  (onclick as any)?.(e);
                  }
                }}>
                <title>
                  {keyColumn.getValueNiceName(row) + ': ' + valueColumn.getValueNiceName(row)}
                </title>
              </rect>
              }
              {y.bandwidth() > 15 && (isAll || row != null) &&
                (isMargin ?
                  <g className="y-label" transform={translate(marginx, y.bandwidth() / 2)}>
                    <TextEllipsis
                    maxWidth={xRule.size('labels')}
                    className="y-label sf-transition"
                    fill={(keyColumn.getColor(k) ?? color(key))}
                    dominantBaseline="middle"
                    textAnchor="end"
                    fontWeight="bold"
                    onClick={e => onDrillDown({ c0: k }, e)}
                    aria-label={keyColumn.getNiceName(k)}                  >
                      {keyColumn.getNiceName(k)}
                    </TextEllipsis>)
                  </g> :
                  isInside ?
                    <g className="y-label" transform={translate(labelsPadding, y.bandwidth() / 2)}>
                      <TextEllipsis
                        transform={translate(insidex, 0)}
                        maxWidth={size - insidex}
                        className="y-label sf-transition"
                        fill={(keyColumn.getColor(k) ?? color(key))}
                        dominantBaseline="middle"
                        fontWeight="bold"
                        onClick={e => onDrillDown({ c0: k }, e)}>
                        {keyColumn.getNiceName(k)}
                      </TextEllipsis>
                    </g> : null
                )}
              {y.bandwidth() > 15 && parseFloat(parameters["NumberOpacity"]) > 0 && row &&
                <g className="numbers-label">
                  <TextIfFits
                    transform={translate(_width / 2, y.bandwidth() / 2)}
                    maxWidth={_width}
                    className="number-label sf-transition"
                    fill={parameters["NumberColor"] ?? "#000"}
                    dominantBaseline="middle"
                    opacity={parameters["NumberOpacity"]}
                    textAnchor="middle"
                    fontWeight="bold"
                    onClick={e => onDrillDown(row!, e)}>
                    {valueColumn.getValueNiceName(row)}
                  </TextIfFits>
                </g>
              }
            </g>
          );
        })}
      </g>
      <InitialMessage data={data} x={xRule.middle("content")} y={yRule.middle("content")} loading={loading} />
      <g opacity={dashboardFilter ? .5 : undefined}>
        <XAxis xRule={xRule} yRule={yRule} />
        <YAxis xRule={xRule} yRule={yRule} />
      </g>
    </svg>
  );
}
