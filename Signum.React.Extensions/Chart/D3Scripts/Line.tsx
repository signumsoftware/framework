import * as React from 'react'
import * as d3 from 'd3'
import D3ChartBase from './D3ChartBase';
import * as ChartUtils from '../Templates/ChartUtils';
import { translate, scale, rotate, skewX, skewY, matrix, scaleFor, rule, ellipsis } from '../Templates/ChartUtils';
import { ChartTable, ChartColumn, ChartRow } from '../ChartClient';
import { KeyCodes } from '@framework/Components';
import ReactChartBase from './ReactChartBase';


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

    var yTicks = y.ticks(height / 50);
    var yTickFormat = y.tickFormat(height / 50);

    var line = d3.line<unknown>()
      .defined(key => rowByKey[keyColumn.getKey(key)] != null)
      .x(key => x(keyColumn.getKey(key))!)
      .y(key => rowByKey[keyColumn.getKey(key)] && -y(valueColumn.getValue(rowByKey[keyColumn.getKey(key)])))
      .curve(ChartUtils.getCurveByName(data.parameters["Interpolate"]!)!);//"linear"

    var color = data.parameters["Color"]!;// 'steelblue'

    return (
      <svg direction="rtl" width={width} height={height}>
        <g className="x-tick" transform={translate(xRule.start('content') + (x.bandwidth() / 2), yRule.start('ticks'))}>
          {keyValues.map((r, i) => <line className="x-tick"
            y2={yRule.start('labels' + (i % 2)) - yRule.start('ticks')}
            x1={x(keyColumn.getKey(r))!}
            x2={x(keyColumn.getKey(r))!}
            stroke="Black" />)}
        </g>
        {(x.bandwidth() * 2) > 60 &&
          <g className="x-label" transform={translate(xRule.start('content') + (x.bandwidth() / 2), yRule.middle('labels0'))}>
            {keyValues.map((r, i) => <TextEllipsis maxWidth={x.bandwidth() * 2} className="x-label"
              x={x(keyColumn.getKey(r))!}
              y={yRule.middle('labels' + (i % 2)) - yRule.middle('labels0')}
              dominant-baseline="middle"
              text-anchor="middle">
              {keyColumn.getNiceName(r)}
            </TextEllipsis>)}
          </g>
        }
        <g className="x-title" transform={translate(xRule.middle('content'), yRule.middle('title'))}>
          <text className="x-title" text-anchor="middle" dominant-baseline="middle">
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
            dominant-baseline="middle"
            text-anchor="end">
            {yTickFormat}
          </text>)}
        </g>

        <g className="y-label" transform={translate(xRule.middle('title'), yRule.middle('content')) + rotate(270)}>
          <text className="y-label" text-anchor="middle" dominant-baseline="middle">
            {valueColumn.title}
          </text>
        </g>

        {/*PAINT CHART'*/}
        <g className="shape" transform={translate(xRule.start('content') + (x.bandwidth() / 2), yRule.end('content'))}>
          <path className="shape" stroke={color} fill="none" stroke-width={3} shape-rendering="initial" d={line(keyValues)!} />
        </g>

        {/*paint graph - hover area trigger*/}
        <g className="hover-trigger" transform={translate(xRule.start('content') + (x.bandwidth() / 2), yRule.end('content'))}>
          {keyValues
            .filter(key => rowByKey[keyColumn.getKey(key)] != null)
            .map(key => <circle className="hover-trigger" fill="#fff" fillOpacity={0} stroke="none" cursor="pointer" r={15}
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
            .map(key => <circle className="point"
              fill={color}
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
              .map(key => <text className="point-label"
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


export class TextEllipsis extends React.Component<{ maxWidth: number, padding?: number } & React.SVGProps<SVGTextElement>> {

  txt?: SVGTextElement | null;

  render() {

    var { maxWidth, padding, children, ...atts } = this.props;

    return (
      <text ref={t => this.txt = t} {...atts} >
        {children || ""}
      </text>
    );
  }


  componentDidMount() {

    var width = this.props.maxWidth;
    if (this.props.padding)
      width -= this.props.padding * 2;

    let txtElement = this.txt!;
    let textLength = txtElement.getComputedTextLength();
    let text = txtElement.textContent!;
    while (textLength > width && text.length > 0) {
      text = text.slice(0, -1);
      while (text[text.length - 1] == ' ' && text.length > 0)
        text = text.slice(0, -1);
      txtElement.textContent = text + '…';
      textLength = txtElement.getComputedTextLength();
    }
  }
}
