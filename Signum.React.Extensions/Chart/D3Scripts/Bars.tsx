import * as React from 'react'
import * as d3 from 'd3'
import * as ChartClient from '../ChartClient';
import * as ChartUtils from '../Templates/ChartUtils';
import { translate, scale, rotate, skewX, skewY, matrix, scaleFor, rule, ellipsis } from '../Templates/ChartUtils';
import { ChartRow } from '../ChartClient';
import ReactChartBase from './ReactChartBase';
import TextEllipsis from './Components/TextEllipsis';
import { XKeyTicks, YScaleTicks, YKeyTicks, XScaleTicks } from './Components/Ticks';
import { XAxis, YAxis } from './Components/Axis';


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

    var color = d3.scaleOrdinal(ChartUtils.getColorScheme(data.parameters["ColorCategory"], parseInt(data.parameters["ColorCategorySteps"]!)))
      .domain(data.rows.map(r => keyColumn.getValueKey(r)));

    var size = xRule.size('content');
    var labelMargin = 10;

    return (
      <svg direction="rtl" width={width} height={height}>

        <XScaleTicks xRule={xRule} yRule={yRule} valueColumn={valueColumn} x={x} />
        <YKeyTicks xRule={xRule} yRule={yRule} keyValues={keyValues} keyColumn={keyColumn} y={y} />

        {/*PAINT GRAPH*/}
        <g className="shape" transform={translate(xRule.start('content'), yRule.start('content'))}>
          {data.rows.map(r => <rect key={keyColumn.getValueKey(r)}
            className="shape"
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
              {data.rows.map(r => <TextEllipsis key={keyColumn.getValueKey(r)}
                maxWidth={xRule.size('labels')}
                padding={labelMargin}
                className="y-label"
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
                    <TextEllipsis key={keyColumn.getValueKey(r)}
                      maxWidth={posx >= size / 2 ? posx : size - posx}
                      padding={labelMargin}
                      className="y-label"
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

                return (<TextEllipsis key={keyColumn.getValueKey(r)}
                  maxWidth={posx >= size / 2 ? posx : size - posx}
                  padding={labelMargin}
                  className="number-label"
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

        <XAxis xRule={xRule} yRule={yRule} />
        <YAxis xRule={xRule} yRule={yRule} />
      </svg>
    );
  }
}
