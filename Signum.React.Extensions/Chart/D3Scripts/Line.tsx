import * as React from 'react'
import * as d3 from 'd3'
import D3ChartBase from './D3ChartBase';
import * as ChartUtils from '../Templates/ChartUtils';
import { translate, scale, rotate, skewX, skewY, matrix, scaleFor, rule, ellipsis } from '../Templates/ChartUtils';
import { ChartTable, ChartColumn, ChartRow } from '../ChartClient';
import { KeyCodes } from '@framework/Components';
import ReactChartBase from './ReactChartBase';
import TextEllipsis from './Components/TextEllipsis';
import { XKeyTicks, YScaleTicks } from './Components/Ticks';
import { XAxis, YAxis } from './Components/Axis';


export default class LineChart extends ReactChartBase {

  renderChart(data: ChartTable, width: number, height: number): React.ReactElement<any> {

    var keyColumn = data.columns.c0! as ChartColumn<unknown>;
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
      _2: parseFloat(data.parameters["NumberOpacity"]) > 0 ? 20 : 5,
      content: '*',
      ticks: 4,
      _3: 5,
      labels0: 15,
      labels1: 15,
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

    var rowByKey = data.rows.toObject(r => keyColumn.getValueKey(r));

    var line = d3.line<unknown>()
      .defined(key => rowByKey[keyColumn.getKey(key)] != null)
      .x(key => x(keyColumn.getKey(key))!)
      .y(key => rowByKey[keyColumn.getKey(key)] && -y(valueColumn.getValue(rowByKey[keyColumn.getKey(key)])))
      .curve(ChartUtils.getCurveByName(data.parameters["Interpolate"]!)!);//"linear"

    var color = data.parameters["Color"]!;// 'steelblue'

    return (
      <svg direction="ltr" width={width} height={height}>

        <XKeyTicks xRule={xRule} yRule={yRule} keyValues={keyValues} keyColumn={keyColumn} x={x} showLines={true} />
        <YScaleTicks xRule={xRule} yRule={yRule} valueColumn={valueColumn} y={y} />

        {/*PAINT CHART'*/}
        <g className="shape" transform={translate(xRule.start('content') + (x.bandwidth() / 2), yRule.end('content'))}>
          <path className="shape" stroke={color} fill="none" strokeWidth={3} shapeRendering="initial" d={line(keyValues)!} />
        </g>

        {/*paint graph - hover area trigger*/}
        <g className="hover-trigger" transform={translate(xRule.start('content') + (x.bandwidth() / 2), yRule.end('content'))}>
          {keyValues
            .filter(key => rowByKey[keyColumn.getKey(key)] != null)
            .map(key => <circle key={keyColumn.getKey(key)}
              className="hover-trigger"
              fill="#fff"
              fillOpacity={0}
              stroke="none"
              cursor="pointer"
              r={15}
              cx={x(keyColumn.getKey(key))!}
              cy={-y(valueColumn.getValue(rowByKey[keyColumn.getKey(key)]))}
              onClick={e => this.props.onDrillDown(rowByKey[keyColumn.getKey(key)])}>
              <title>
                {keyColumn.getNiceName(key) + ': ' + valueColumn.getValueNiceName(rowByKey[keyColumn.getKey(key)])}
              </title>
            </circle>)}
        </g>

        {/*paint graph - points*/}
        <g className="point" transform={translate(xRule.start('content') + (x.bandwidth() / 2), yRule.end('content'))}>
          {keyValues
            .filter(key => rowByKey[keyColumn.getKey(key)] != null)
            .map(key => <circle key={keyColumn.getKey(key)}
              className="point"
              stroke={color}
              strokeWidth={2}
              fill="white"
              r={5}
              cx={x(keyColumn.getKey(key))!}
              cy={-y(valueColumn.getValue(rowByKey[keyColumn.getKey(key)]))}
              onClick={e => this.props.onDrillDown(rowByKey[keyColumn.getKey(key)])}
              cursor="pointer"
              shapeRendering="initial">
              <title>
                {keyColumn.getNiceName(key) + ': ' + valueColumn.getValueNiceName(rowByKey[keyColumn.getKey(key)])}
              </title>
            </circle>)}
        </g>

        { /*Point labels*/
          parseFloat(data.parameters["NumberOpacity"]!) > 0 &&
          <g className="point-label" transform={translate(xRule.start('content') + (x.bandwidth() / 2), yRule.end('content'))}>
            {keyValues
              .filter(key => rowByKey[keyColumn.getKey(key)] != null)
              .map(key => <text key={keyColumn.getKey(key)}
                className="point-label"
                r={5}
                x={x(keyColumn.getKey(key))!}
                y={-y(valueColumn.getValue(rowByKey[keyColumn.getKey(key)])) - 10}
                opacity={parseFloat(data.parameters["NumberOpacity"]!)}
                textAnchor="middle"
                onClick={e => this.props.onDrillDown(rowByKey[keyColumn.getKey(key)])}
                cursor="pointer"
                shapeRendering="initial">
                {valueColumn.getValueNiceName(rowByKey[keyColumn.getKey(key)])}
              </text>)}
          </g>
        }

        <XAxis xRule={xRule} yRule={yRule} />
        <YAxis xRule={xRule} yRule={yRule} />
        
      </svg>
    );
  }



}
