import * as React from 'react'
import * as d3 from 'd3'
import D3ChartBase from './D3ChartBase';
import * as ChartUtils from '../Templates/ChartUtils';
import { translate, scale, rotate, skewX, skewY, matrix, scaleFor, rule, ellipsis } from '../Templates/ChartUtils';
import { ChartTable, ChartColumn } from '../ChartClient';
import ReactChartBase from './ReactChartBase';
import { TextEllipsis } from './Line';


export default class ColumnsChart extends ReactChartBase {

  renderChart(data: ChartTable, width: number, height: number): React.ReactElement<any> {

    var keyColumn = data.columns.c0!;
    var valueColumn = data.columns.c1! as ChartColumn<number>;

    var xRule = rule({
      _1: 5,
      title: 15,
      _2: 10,
      labels: parseInt(data.parameters["UnitMargin"]),
      _3: 5,
      ticks: 4,
      content: '*',
      _4: 10,
    }, width);
    //xRule.debugX(chart)

    var yRule = rule({
      _1: 5,
      legend: 15,
      _2: 5,
      content: '*',
      ticks: 4,
      _3: data.parameters["Labels"] == "Margin" ? 5 : 0,
      labels: data.parameters["Labels"] == "Margin" ? parseInt(data.parameters["LabelsMargin"]) : 0,
      _4: 10,
      title: 15,
      _5: 5,
    }, height);
    //yRule.debugY(chart);

    var keyValues = ChartUtils.completeValues(keyColumn, data.rows.map(r => keyColumn.getValue(r)), data.parameters['CompleteValues'], ChartUtils.insertPoint(keyColumn, valueColumn));

    var x = d3.scaleBand()
      .domain(keyValues.map(v => keyColumn.getKey(v)))
      .range([0, xRule.size('content')]);
    
    var y = scaleFor(valueColumn, data.rows.map(r => valueColumn.getValue(r)), 0, yRule.size('content'), data.parameters["Scale"]);

    var yTicks = y.ticks(10);
    var yTickFormat = y.tickFormat(height / 50);

    var color = d3.scaleOrdinal(ChartUtils.getColorScheme(data.parameters["ColorCategory"], parseInt(data.parameters["ColorCategorySteps"]!)))
      .domain(data.rows.map(r => keyColumn.getValueKey(r)!));

    var size = yRule.size('content');
    var labelMargin = 10;

    return (
      <svg direction="rtl" width={width} height={height}>

        <g className="x-title" transform={translate(xRule.middle('content'), yRule.middle('title'))}>
          <text className="x-title" textAnchor="middle" dominantBaseline="middle">
            {keyColumn.title}
          </text>
        </g>

        <g className="y-line" transform={translate(xRule.start('content'), yRule.end('content'))}>
          {yTicks.map(t => <line className="y-line"
            x2={xRule.size('content')}
            y1={-y(t)}
            y2={-y(t)}
            stroke="LightGray" />)}
        </g>


        <g className="y-tick" transform={translate(xRule.start('ticks'), yRule.end('content'))}>
          {yTicks.map(t => <line className="y-tick"
            x2={xRule.size('ticks')}
            y1={-y(t)}
            y2={-y(t)}
            stroke="Black" />)}
        </g>

        <g className="y-label" transform={translate(xRule.end('labels'), yRule.end('content'))}>
          {yTicks.map(t => <text className="y-label"
            y={-y(t)}
            dominantBaseline="middle"
            textAnchor="end">
            {yTickFormat}
          </text>)}
        </g>

        <g className="y-label" transform={translate(xRule.middle('title'), yRule.middle('content')) + rotate(270)}>
          <text className="y-label" textAnchor="middle" dominantBaseline="middle">
            {valueColumn.title}
          </text>
        </g>


        {/*PAINT CHART*/}

        <g className="shape" transform={translate(xRule.start('content'), yRule.end('content'))}>
          {data.rows.map(r => <rect className="shape"
            y={-y(valueColumn.getValue(r))}
            height={y(valueColumn.getValue(r))}
            width={x.bandwidth()}
            x={x(keyColumn.getValueKey(r))!}
            fill={keyColumn.getValueColor(r) || color(keyColumn.getValueKey(r))}
            cursor="pointer"
            stroke={x.bandwidth() > 4 ? '#fff' : undefined}
            onClick={e => this.props.onDrillDown(r)}>
            <title>
              {keyColumn.getValueNiceName(r) + ': ' + valueColumn.getValueNiceName(r)}
            </title>
          </rect>)}
        </g>

        {x.bandwidth() > 15 && 
          (data.parameters["Labels"] == "Margin" ?
            <g className="x-label" transform={translate(xRule.start('content'), yRule.start('labels'))}>
              {data.rows.map(r => <TextEllipsis maxWidth={yRule.size('labels')} padding={labelMargin} className="x-label"
                transform={translate(x(keyColumn.getValueKey(r))! + x.bandwidth() / 2, 0) + rotate(-90)}
                dominantBaseline="middle"
                fontWeight="bold"
                fill={(keyColumn.getValueColor(r) || color(keyColumn.getValueKey(r)))}
                textAnchor="end"
                cursor="pointer"
                onClick={de => this.props.onDrillDown(r)}>
                {keyColumn.getValueNiceName(r)}
              </TextEllipsis>)}
          </g> : 
          data.parameters["Labels"] == "Inside" ? 
            <g className="x-label" transform={translate(xRule.start('content'), yRule.end('content'))}>
              {data.rows.map(r => {
                var posy = y(valueColumn.getValue(r));
                return (
                  <TextEllipsis maxWidth={posy >= size / 2 ? posy : size - posy} padding={labelMargin} className="x-label"
                    transform={translate(x(keyColumn.getValueKey(r))! + x.bandwidth() / 2, -y(valueColumn.getValue(r))) + rotate(-90)}
                    dominantBaseline="middle"
                    fontWeight="bold"
                    fill={y(valueColumn.getValue(r)) >= size / 2 ? '#fff' : (keyColumn.getValueColor(r) || color(keyColumn.getValueKey(r)))}
                    dx={y(valueColumn.getValue(r)) >= size / 2 ? -labelMargin : labelMargin}
                    textAnchor={y(valueColumn.getValue(r)) >= size / 2 ? 'end' : 'start'}
                    onClick={e => this.props.onDrillDown(r)}
                    cursor="pointer">
                    {keyColumn.getValueNiceName(r)}
                  </TextEllipsis>);
              })}
            </g> : null
          )}

        {parseFloat(data.parameters["NumberOpacity"]) > 0 &&
          <g className="numbers-label" transform={translate(xRule.start('content'), yRule.end('content'))}>
            {data.rows
              .filter(r => y(valueColumn.getValue(r)) > 10)
              .map(r => <text className="number-label"
                transform={translate(x(keyColumn.getValueKey(r))! + x.bandwidth() / 2, -y(valueColumn.getValue(r)) / 2) + rotate(-90)}
                fill={data.parameters["NumberColor"] || "#000"}
                dominantBaseline="central"
                opacity={data.parameters["NumberOpacity"]}
                textAnchor="middle"
                fontWeight="bold"
                cursor="pointer"
                onClick={e => this.props.onDrillDown(r)}>
                {valueColumn.getValueNiceName(r)}
              </text>)}
          </g>}

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
