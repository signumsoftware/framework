import * as React from 'react'
import * as d3 from 'd3'
import * as ChartClient from '../ChartClient';
import * as ChartUtils from './Components/ChartUtils';
import { translate, scale, rotate, skewX, skewY, matrix, scaleFor } from './Components/ChartUtils';
import { ChartTable, ChartColumn, ChartRow, ChartScriptProps } from '../ChartClient';
import { Dic } from '@framework/Globals';
import { XKeyTicks } from './Components/Ticks';
import { Rule } from './Components/Rule';
import InitialMessage from './Components/InitialMessage';

interface ColumnWithScales {
  column: ChartColumn<number>;
  scale: d3.ScaleContinuousNumeric<number, number>;
  colorScale: (r: ChartRow) => string;
}

export default function renderParallelCoordinates(p: ChartScriptProps): React.ReactElement<any> {
  return <ParallelCoordinatesImp {...p} />
}

interface ParallelCoordinatesImpState {
  selectedColumn?: string;
}

class ParallelCoordinatesImp extends React.Component<ChartScriptProps, ParallelCoordinatesImpState> {

  constructor(props: ChartScriptProps) {
    super(props);
    this.state = {};
  }

  render() {

    const { data, width, height, parameters, loading, onDrillDown, initialLoad } = this.props;
    
    var yRule = new Rule({
      _1: 5,
      title: 15,
      _2: 5,
      max: 12,
      _3: 4,
      content: '*',
      _4: 4,
      min: 12,
      _5: 5,
    }, height);

    var xRule = new Rule({
      _1: 20,
      content: '*',
      _2: 20,
    }, width);
    //xRule.debugX(chart);

    if (data == null || data.rows.length == 0)
      return (
        <svg direction="ltr" width={width} height={height}>
          <InitialMessage data={data} x={xRule.middle("content")} y={yRule.middle("content")} loading={loading} />
        </svg>
      );

    var keyColumn = data.columns.c0!;
    
    var colorInterpolate = parameters["ColorInterpolate"];
    var colorInterpolation = ChartUtils.getColorInterpolation(colorInterpolate)!;

    var cords = Dic.getValues(data.columns)
      .filter(c => c && c.name != "c0" && c.name != "entity")
      .map(p => {
        const c = p! as ChartColumn<number>;
        var values = data.rows.map(r => c.getValue(r));
        var scaleType = parameters["Scale" + c.name.after("c")];
        var scale = scaleFor(c, values, 0, yRule.size('content'), scaleType);
        var scaleFunc = scaleFor(c, values, 0, 1, scaleType);
        var colorScale = (r: ChartRow) => colorInterpolation(scaleFunc(c.getValue(r)));

        return {
          column: c,
          scale,
          colorScale
        } as ColumnWithScales;
      });

    var x = d3.scaleBand()
      .domain(cords.map(d => d.column.name))
      .rangeRound([0, xRule.size('content')]);

    var line = d3.line<{ col: ColumnWithScales, row: ChartRow }>()
      .defined(t => t.col.column.getValue(t.row) != undefined)
      .x(t => x(t.col.column.name)!)
      .y(t => - t.col.scale(t.col.column.getValue(t.row)))
      .curve(ChartUtils.getCurveByName(parameters["Interpolate"])!);//"linear"

    var boxWidth = 10;

    var selectedColumn = cords.firstOrNull(a => a.column.name == this.state.selectedColumn) || cords.first();

    return (
      <svg direction="ltr" width={width} height={height}>
        <g className="x-tick" transform={translate(xRule.start('content') + x.bandwidth() / 2, yRule.start('content'))}>
          {cords.map(d => <line key={d.column.name} className="x-tick sf-transition"
            transform={translate(x(d.column.name)!, 0)}
            y2={yRule.size('content')}
            stroke="black" />)}
        </g>

        <g className="x-label" transform={translate(xRule.start('content') + x.bandwidth() / 2, yRule.middle('title'))}>
          {cords.map(d => <text key={d.column.name} className="x-label sf-transition"
            transform={translate(x(d.column.name)!, 0)}
            dominantBaseline="middle"
            textAnchor="middle"
            fontWeight="bold">
            {d.column.title}
          </text>)}
        </g>

        <g className="x-label-max" transform={translate(xRule.start('content') + x.bandwidth() / 2, yRule.middle('max'))}>
          {cords.map(d => <text key={d.column.name} className="x-label-max sf-transition"
            transform={translate(x(d.column.name)!,0)}
            dominantBaseline="middle"
            textAnchor="middle">
            {d.column.type != "Date" && d.column.type != "DateTime" ?
              d.scale.domain()[1] :
              d.column.getNiceName(d3.max(data.rows, r => d.column.getValue(r))!)}
          </text>)}
        </g>

        <g className="x-label-min" transform={translate(xRule.start('content') + x.bandwidth() / 2, yRule.middle('min'))}>
          {cords.map(d => <text key={d.column.name} className="x-label-min sf-transition"
            transform={translate(x(d.column.name)!, 0)}
            dominantBaseline="middle"
            textAnchor="middle">
            {d.column.type != "Date" && d.column.type != "DateTime" ?
              d.column.getNiceName(d.scale.domain()[0]) :
              d.column.getNiceName(d3.min(data.rows, r => d.column.getValue(r))!)}
          </text>)}
        </g>


        {data.rows.orderBy(r => keyColumn.getValueKey(r)).map((r, i) => <g key={i} className="shape-serie"
          transform={translate(xRule.start('content') + x.bandwidth() / 2, yRule.end('content'))}>
          <path
            opacity={initialLoad ? 0 : 1}
            className="shape sf-transition"
            fill="none"
            strokeWidth={1}
            stroke={selectedColumn.colorScale(r)}
            shapeRendering="initial"
            onClick={e => onDrillDown(r)}
            cursor="pointer"
            d={line(cords.map(c => ({ col: c, row: r })))!}>
            <title>
              {keyColumn.getValueNiceName(r) + "\n" +
                cords.map(c => c.column.title + ": " + c.column.getValueNiceName(r)).join("\n")}
            </title>
          </path>
        </g>)}


        <g className="x-tick-box" transform={translate(xRule.start('content') + x.bandwidth() / 2, yRule.start('content'))}>
          {cords.map(d => <rect key={d.column.name}
            transform={translate(x(d.column.name)! - boxWidth / 2, 0)}
            className="x-tick-box sf-transition"
            height={yRule.size('content')}
            width={boxWidth}
            stroke="#ccc"
            fill={selectedColumn.column.name != d.column.name ? '#ccc' : '#000'}
            fillOpacity=".2"
            onClick={e => this.setState({ selectedColumn: d.column.name })} />)}
        </g>

        <InitialMessage data={data} x={xRule.middle("content")} y={yRule.middle("content")} loading={loading} />
      </svg>
    );

  }
}


