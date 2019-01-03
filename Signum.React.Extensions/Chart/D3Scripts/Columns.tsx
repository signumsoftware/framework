import * as React from 'react'
import * as d3 from 'd3'
import * as ChartUtils from './Components/ChartUtils';
import { translate, scale, rotate, skewX, skewY, matrix, scaleFor } from './Components/ChartUtils';
import { ChartTable, ChartColumn, ChartScriptProps } from '../ChartClient';
import TextEllipsis from './Components/TextEllipsis';
import { XKeyTicks, YScaleTicks, XTitle } from './Components/Ticks';
import { XAxis, YAxis } from './Components/Axis';
import { Rule } from './Components/Rule';
import InitialMessage from './Components/InitialMessage';


export default function renderColumns({ data, width, height, parameters, loading, onDrillDown, initialLoad }: ChartScriptProps): React.ReactElement<any> {

  var xRule = new Rule({
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

  var yRule = new Rule({
    _1: 5,
    legend: 15,
    _2: 5,
    content: '*',
    ticks: 4,
    _3: parameters["Labels"] == "Margin" ? 5 : 0,
    labels: parameters["Labels"] == "Margin" ? parseInt(parameters["LabelsMargin"]) : 0,
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

  var keyValues = ChartUtils.completeValues(keyColumn, data.rows.map(r => keyColumn.getValue(r)), parameters['CompleteValues'], ChartUtils.insertPoint(keyColumn, valueColumn));

  var x = d3.scaleBand()
    .domain(keyValues.map(v => keyColumn.getKey(v)))
    .range([0, xRule.size('content')]);

  var y = scaleFor(valueColumn, data.rows.map(r => valueColumn.getValue(r)), 0, yRule.size('content'), parameters["Scale"]);
  
  var orderedRows = data.rows.orderBy(r => keyColumn.getValueKey(r));
  var color = ChartUtils.colorCategory(parameters, orderedRows.map(r => keyColumn.getValueKey(r)!));

  var size = yRule.size('content');
  var labelMargin = 10;

  return (
    <svg direction="ltr" width={width} height={height}>

      <XTitle xRule={xRule} yRule={yRule} keyColumn={keyColumn} />
      <YScaleTicks xRule={xRule} yRule={yRule} valueColumn={valueColumn} y={y} />

      {/*PAINT CHART*/}

      <g className="shape" transform={translate(xRule.start('content'), yRule.end('content'))}>
        {orderedRows.map(r => <rect key={keyColumn.getValueKey(r)} className="shape sf-transition"
          transform={(initialLoad ? scale(1, 0) : scale(1, 1)) + translate(x(keyColumn.getValueKey(r))!, -y(valueColumn.getValue(r))!)}
          height={y(valueColumn.getValue(r))}
          width={x.bandwidth()}
          fill={keyColumn.getValueColor(r) || color(keyColumn.getValueKey(r))}
          cursor="pointer"
          stroke={x.bandwidth() > 4 ? '#fff' : undefined}
          onClick={e => onDrillDown(r)}>
          <title>
            {keyColumn.getValueNiceName(r) + ': ' + valueColumn.getValueNiceName(r)}
          </title>
        </rect>)}
      </g>

      {x.bandwidth() > 15 &&
        (parameters["Labels"] == "Margin" ?
          <g className="x-label" transform={translate(xRule.start('content'), yRule.start('labels'))}>
            {orderedRows.map(r => <TextEllipsis key={keyColumn.getValueKey(r)} maxWidth={yRule.size('labels')} padding={labelMargin} className="x-label sf-transition"
              transform={translate(x(keyColumn.getValueKey(r))! + x.bandwidth() / 2, 0) + rotate(-90)}
              dominantBaseline="middle"
              fontWeight="bold"
              fill={(keyColumn.getValueColor(r) || color(keyColumn.getValueKey(r)))}
              textAnchor="end"
              cursor="pointer"
              onClick={de => onDrillDown(r)}>
              {keyColumn.getValueNiceName(r)}
            </TextEllipsis>)}
          </g> :
          parameters["Labels"] == "Inside" ?
            <g className="x-label" transform={translate(xRule.start('content'), yRule.end('content'))}>
              {orderedRows.map(r => {
                var posy = y(valueColumn.getValue(r));
                return (
                  <TextEllipsis key={keyColumn.getValueKey(r)} maxWidth={posy >= size / 2 ? posy : size - posy} padding={labelMargin} className="x-label sf-transition"
                    transform={translate(x(keyColumn.getValueKey(r))! + x.bandwidth() / 2, -y(valueColumn.getValue(r))) + rotate(-90)}
                    dominantBaseline="middle"
                    fontWeight="bold"
                    fill={y(valueColumn.getValue(r)) >= size / 2 ? '#fff' : (keyColumn.getValueColor(r) || color(keyColumn.getValueKey(r)))}
                    dx={y(valueColumn.getValue(r)) >= size / 2 ? -labelMargin : labelMargin}
                    textAnchor={y(valueColumn.getValue(r)) >= size / 2 ? 'end' : 'start'}
                    onClick={e => onDrillDown(r)}
                    cursor="pointer">
                    {keyColumn.getValueNiceName(r)}
                  </TextEllipsis>);
              })}
            </g> : null
        )}

      {parseFloat(parameters["NumberOpacity"]) > 0 &&
        <g className="numbers-label" transform={translate(xRule.start('content'), yRule.end('content'))}>
          {orderedRows
            .filter(r => y(valueColumn.getValue(r)) > 10)
            .map(r => <text key={keyColumn.getValueKey(r)} className="number-label sf-transition"
              transform={translate(x(keyColumn.getValueKey(r))! + x.bandwidth() / 2, -y(valueColumn.getValue(r)) / 2) + rotate(-90)}
              fill={parameters["NumberColor"] || "#000"}
              dominantBaseline="middle"
              opacity={parameters["NumberOpacity"]}
              textAnchor="middle"
              fontWeight="bold"
              cursor="pointer"
              onClick={e => onDrillDown(r)}>
              {valueColumn.getValueNiceName(r)}
            </text>)}
        </g>}

      <InitialMessage data={data} x={xRule.middle("content")} y={yRule.middle("content")} loading={loading} />
      <XAxis xRule={xRule} yRule={yRule} />
      <YAxis xRule={xRule} yRule={yRule} />
    </svg>
  );
}
