import * as React from 'react'
import * as d3 from 'd3'
import D3ChartBase from './D3ChartBase';
import * as ChartClient from '../ChartClient';
import * as ChartUtils from '../Templates/ChartUtils';
import { translate, scale, rotate, skewX, skewY, matrix, scaleFor, rule, ellipsis } from '../Templates/ChartUtils';
import { ChartRow } from '../ChartClient';
import ReactChartBase from './ReactChartBase';
import { TextEllipsis } from './Line';


export default class BarsChart extends ReactChartBase {

  renderChart(data: ChartClient.ChartTable, width: number, height: number): React.ReactElement<any> {

    var keyColumn = data.columns.c0!;
    var valueColumn = data.columns.c1! as ChartClient.ChartColumn<number>;

    var xRule = rule({
      _1: 5,
      title: 15,
      _2: 10,
      labels: data.parameters["Labels"] == "Margin" ? parseInt(data.parameters["LabelsMargin"]) : 0,
      _3: data.parameters["Labels"] == "Margin" ? 5 : 0,
      ticks: 4,
      content: '*',
      _4: 5,
    }, width);
    //xRule.debugX(chart)

    var yRule = rule({
      _1: 5,
      content: '*',
      ticks: 4,
      _2: 5,
      labels: 10,
      _3: 10,
      title: 15,
      _4: 5,
    }, height);
    //yRule.debugY(chart);


    var x = scaleFor(valueColumn, data.rows.map(r => valueColumn.getValue(r)), 0, xRule.size('content'), data.parameters['Scale']);

    var keyValues = ChartUtils.completeValues(keyColumn, data.rows.map(r => keyColumn.getValue(r)), data.parameters['CompleteValues'], ChartUtils.insertPoint(keyColumn, valueColumn));

    var y = d3.scaleBand()
      .domain(keyValues.map(v => keyColumn.getKey(v)))
      .range([0, yRule.size('content')]);

    var xTicks = x.ticks(width / 50);
    var xTickFormat = x.tickFormat(width / 50);

    var color = d3.scaleOrdinal(ChartUtils.getColorScheme(data.parameters["ColorCategory"], parseInt(data.parameters["ColorCategorySteps"]!)))
      .domain(data.rows.map(r => keyColumn.getValueKey(r)));

    var size = xRule.size('content');
    var labelMargin = 10;

    return (
      <svg direction="rtl" width={width} height={height}>
        <g className="x-lines" transform={translate(xRule.start('content'), yRule.start('content'))}>
          {xTicks.map(t => <line className="y-lines"
            x1={x(t)}
            x2={x(t)}
            y1={yRule.size('content')}
            stroke="LightGray" />)}
        </g>

        <g className="x-tick" transform={translate(xRule.start('content'), yRule.start('ticks'))}>
          {xTicks.map(t => <line className="x-tick"
            x1={x(t)}
            x2={x(t)}
            y2={yRule.size('ticks')}
            stroke="Black" />)}
        </g>

        <g className="x-label" transform={translate(xRule.start('content'), yRule.end('labels'))}>
          {xTicks.map(t => <text className="x-label"
            x={x(t)}
            textAnchor="middle">
            {xTickFormat}
          </text>)}
        </g>

        <g className="x-title" transform={translate(xRule.middle('content'), yRule.middle('title'))}>
          <text className="x-title" textAnchor="middle" dominantBaseline="central">
            {valueColumn.title || ""}
          </text>
        </g>


        <g className="y-tick" transform={translate(xRule.start('ticks'), yRule.end('content'))}>
          {data.rows.map(r => <line className="y-tick"
            x2={xRule.size('ticks')}
            y1={-y(keyColumn.getValueKey(r))!}
            y2={-y(keyColumn.getValueKey(r))!}
            stroke="Black" />)}
        </g>

        <g className="y-title" transform={translate(xRule.middle('title'), yRule.middle('content')) + rotate(270)}>
          <text className="y-title" textAnchor="middle" dominantBaseline="central">
            {keyColumn.title || ""}
          </text>
        </g>

        {/*PAINT GRAPH*/}
        <g className="shape" transform={translate(xRule.start('content'), yRule.start('content'))}>
          {data.rows.map(r => <rect className="shape"
            width={x(valueColumn.getValue(r))}
            height={y.bandwidth()}
            y={y(keyColumn.getValueKey(r))!}
            fill={keyColumn.getValueColor(r) || color(keyColumn.getValueKey(r))}
            stroke={y.bandwidth() > 4 ? '#fff' : undefined}
            onClick={e => this.props.onDrillDown(r)}
            cursor="pointer">
            <title>
              {keyColumn.getValueNiceName(r) + ': ' + valueColumn.getValueNiceName(r)}
            </title>
          </rect>)}
        </g>

        {y.bandwidth() > 15 &&
          (data.parameters["Labels"] == "Margin" ?
            <g className="y-label" transform={translate(xRule.end('labels'), yRule.start('content') + y.bandwidth() / 2)}>
              {data.rows.map(r => <TextEllipsis maxWidth={xRule.size('labels')} padding={labelMargin} className="y-label"
                y={y(keyColumn.getValueKey(r))!}
                fill={(keyColumn.getValueColor(r) || color(keyColumn.getValueKey(r)))}
                dominantBaseline="central"
                textAnchor="end"
                fontWeight="bold"
                onClick={e => this.props.onDrillDown(r)}
                cursor="pointer">
                {keyColumn.getValueNiceName(r)}
              </TextEllipsis>)}
            </g> :
            data.parameters["Labels"] == "Inside" ?
              <g className="y-label" transform={translate(xRule.start('content') + labelMargin, yRule.start('content') + y.bandwidth() / 2)}>
                {data.rows.map(r => {
                  var posx = x(valueColumn.getValue(r));
                  return (
                    <TextEllipsis maxWidth={posx >= size / 2 ? posx : size - posx} padding={labelMargin} className="y-label"
                      x={posx >= size / 2 ? 0 : posx}
                      y={y(keyColumn.getValueKey(r))!}
                      fill={x(valueColumn.getValue(r)) >= size / 2 ? '#fff' : (keyColumn.getValueColor(r) || color(keyColumn.getValueKey(r)))}
                      dominantBaseline="central"
                      fontWeight="bold"
                      onClick={e => this.props.onDrillDown(r)}
                      cursor="pointer">
                      {keyColumn.getValueNiceName(r)}
                    </TextEllipsis>
                  );
                })}
              </g> : null
          )}

        {parseFloat(data.parameters["NumberOpacity"]) > 0 &&
          <g className="numbers-label" transform={translate(xRule.start('content'), yRule.start('content'))}>
            {data.rows
              .filter(r => x(valueColumn.getValue(r)) > 20)
              .map(r => {
                var posx = x(valueColumn.getValue(r));

                return (<TextEllipsis maxWidth={posx >= size / 2 ? posx : size - posx} padding={labelMargin} className="number-label"
                  y={y(keyColumn.getValueKey(r))! + y.bandwidth() / 2}
                  x={x(valueColumn.getValue(r)) / 2}
                  fill={data.parameters["NumberColor"] || "#000"}
                  dominantBaseline="central"
                  opacity={data.parameters["NumberOpacity"]}
                  textAnchor="middle"
                  fontWeight="bold"
                  onClick={e => this.props.onDrillDown(r)}
                  cursor="pointer">
                  {valueColumn.getValueNiceName(r)}
                </TextEllipsis>);
              })}
          </g>
        }

        <g className="x-axis" transform={translate(xRule.start('content'), yRule.end('content'))}>
          <line className="x-axis" x2={xRule.size('content')} stroke="Black" />
        </g>

        <g className="y-axis" transform={translate(xRule.start('content'), yRule.start('content'))}>
          <line className="y-axis" y2={yRule.size('content')} stroke="Black" />
        </g>
      </svg>
    );
  }
}
