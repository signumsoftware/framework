import * as React from 'react'
import * as d3 from 'd3'
import D3ChartBase from './D3ChartBase';
import * as ChartClient from '../ChartClient';
import * as ChartUtils from '../Templates/ChartUtils';
import { translate, scale, rotate, skewX, skewY, matrix, scaleFor, rule, ellipsis } from '../Templates/ChartUtils';
import { ChartColumn, ChartRow } from '../ChartClient';
import { Dic } from '../../../../Framework/Signum.React/Scripts/Globals';
import ReactChartBase from './ReactChartBase';
import { XKeyTicks, YKeyTicks } from './Components/Ticks';
import { XAxis, YAxis } from './Components/Axis';


export default class PunchcardChart extends ReactChartBase {

  renderChart(data: ChartClient.ChartTable, width: number, height: number): React.ReactElement<any> {

    var horizontalColumn = data.columns.c0!;
    var verticalColumn = data.columns.c1!;
    var sizeColumn = data.columns.c2! as ChartColumn<number> | undefined;
    var colorColumn = data.columns.c3! as ChartColumn<number> | undefined;
    var opacityColumn = data.columns.c4! as ChartColumn<number> | undefined;
    var innerSizeColumn = data.columns.c5! as ChartColumn<number> | undefined;
    var orderingColumn = data.columns.c6! as ChartColumn<number> | undefined;

    function groupAndSort(rows: ChartRow[], shortType: string, column: ChartColumn<unknown>, completeValues: string | null | undefined): unknown[] {
      var dictionary = rows.groupToObject(r => "k" + column.getValueKey(r));

      var values = Dic.getValues(dictionary).map(array => column.getValue(array[0]));

      var extendedValues = ChartUtils.completeValues(column, values, completeValues, "After");
      switch (shortType) {
        case "Ascending": return extendedValues.orderBy(a => a);
        case "AscendingToStr": return extendedValues.orderBy(a => column.getNiceName(a));
        case "AscendingKey": return extendedValues.orderBy(a => column.getKey(a));
        case "AscendingSumOrder": return extendedValues.orderBy(a => getSum(dictionary["k" + column.getKey(a)]));
        case "Descending": return extendedValues.orderByDescending(a => a);
        case "DescendingToStr": return extendedValues.orderByDescending(a => column.getNiceName(a));
        case "DescendingKey": return extendedValues.orderByDescending(a => column.getKey(a));
        case "DescendingSumOrder": return extendedValues.orderByDescending(a => getSum(dictionary["k" + column.getKey(a)]));
        default: return extendedValues;
      }
    }

    function getSum(elements: ChartRow[] | undefined): number {

      if (elements == undefined)
        return 0;

      if (orderingColumn == null)
        return 0;

      if ((elements as any).__sum__ !== undefined)
        return (elements as any).__sum__;

      return (elements as any).__sum__ = elements.reduce<number>((acum, r) => acum + orderingColumn!.getValue(r) || 0, 0);
    }

    var horizontalKeys = groupAndSort(data.rows, data.parameters["XSort"]!, horizontalColumn, data.parameters['CompleteHorizontalValues']);
    var verticalKeys = groupAndSort(data.rows, data.parameters["YSort"]!, verticalColumn, data.parameters['CompleteVerticalValues']);

    var xRule = rule({
      _1: 5,
      title: 15,
      _2: 10,
      labels: parseInt(data.parameters["XMargin"]),
      _3: 5,
      ticks: 4,
      content: '*',
      _4: 10,
    }, width);
    //xRule.debugX(chart)

    var yRule = rule({
      _1: 5,
      content: '*',
      ticks: 4,
      _2: 5,
      labels0: 15,
      labels1: 15,
      _3: 10,
      title: 15,
      _4: 5,
    }, height);
    //yRule.debugY(chart);

    var x = d3.scaleBand()
      .domain(horizontalKeys.map(horizontalColumn.getKey))
      .range([0, xRule.size('content')]);

    var y = d3.scaleBand()
      .domain(verticalKeys.map(verticalColumn.getKey))
      .range([0, yRule.size('content')]);

    var color: null | ((row: number) => string) = null;
    if (colorColumn != null) {
      var scaleFunc = scaleFor(colorColumn, data.rows.map(colorColumn.getValue), 0, 1, data.parameters["ColorScale"]);
      var colorInterpolator = ChartUtils.getColorInterpolation(data.parameters["ColorInterpolate"]);
      color = v => colorInterpolator!(scaleFunc(v))
    }

    var opacity: null | ((row: number) => number) = null;
    if (opacityColumn != null) {
      opacity = scaleFor(opacityColumn, data.rows.map(opacityColumn.getValue), 0, 1, data.parameters["OpacityScale"]);
    }

    var shape = data.parameters["Shape"];
    var innerSize = null
    if (innerSizeColumn != null) {
      innerSize = scaleFor(innerSizeColumn, data.rows.map(innerSizeColumn.getValue), 0, 100, data.parameters["OpacityScale"])
    }


    function configureShape(column: ChartColumn<number> | undefined, rowValue: (r: ChartRow) => number, extra: (r: ChartRow) => React.SVGAttributes<SVGElement>): { numberOpacity: (val: number) => number, renderer: (r: ChartRow) => React.ReactElement<any> } | undefined {

      if (shape == "Circle") {

        var circleSize = Math.min(x.bandwidth(), y.bandwidth()) * 0.45;
        var area: (n: number) => number = column == null ?
          (() => circleSize * circleSize) :
          scaleFor(column, data.rows.map(column.getValue), 0, circleSize * circleSize, data.parameters["SizeScale"]);

        return {
          numberOpacity: n => area(n) / 500,
          renderer: r => <circle
            cx={x(horizontalColumn.getValueKey(r))!}
            cy={-y(verticalColumn.getValueKey(r))!}
            r={Math.sqrt(area(rowValue(r)))}
            {...extra(r)} />
        };
      } else if (shape == "Rectangle") {

        var area: (n: number) => number = column == null ?
          (() => x.bandwidth() * y.bandwidth()) :
          scaleFor(column, data.rows.map(column.getValue), 0, x.bandwidth() * y.bandwidth(), data.parameters["SizeScale"]);
        var ratio = x.bandwidth() / y.bandwidth();
        var recWidth = (r: ChartRow) => Math.sqrt(area(rowValue(r)) * ratio);
        var recHeight = (r: ChartRow) => Math.sqrt(area(rowValue(r)) / ratio);

        return {
          numberOpacity: n => area(n) / 500,
          renderer: r => <rect
            x={x(horizontalColumn.getValueKey(r))! - recWidth(r) / 2}
            y={-y(verticalColumn.getValueKey(r))! - recHeight(r) / 2}
            width={recWidth(r)}
            height={recHeight(r)}
            {...extra(r)}
          />
        };
      } else if (shape == "ProgressBar") {
        var progressWidth: (n: number) => number = column == null ?
          () => x.bandwidth() :
          scaleFor(column, data.rows.map(column.getValue), 0, x.bandwidth(), data.parameters["SizeScale"]);

        return {
          numberOpacity: n => 1,
          renderer: r => <rect
            x={x(horizontalColumn.getValueKey(r))! - x.bandwidth() / 2}
            y={-y(verticalColumn.getValueKey(r))! - y.bandwidth() / 2}
            width={progressWidth(rowValue(r))}
            height={y.bandwidth()}
            {...extra(r)} />
        };
      }
      else
        return undefined;
    }

    var fillOpacity = (r: ChartRow) => parseFloat(data.parameters["FillOpacity"]) * (opacity != null ? opacity(opacityColumn!.getValue(r)) : 1);

    var tr = translate(xRule.start('content') + x.bandwidth() / 2, yRule.end('content') - y.bandwidth() / 2);

    var mainShape = configureShape(sizeColumn, r => sizeColumn ? sizeColumn.getValue(r) : 0,
      r => ({
        transform: tr,
        shapeRendering: "initial",
        fillOpacity: fillOpacity(r),
        fill: color == null ? (data.parameters["FillColor"] || 'black') : color(colorColumn!.getValue(r)),
        stroke: data.parameters["StrokeColor"] || (color == null ? 'black' : color(colorColumn!.getValue(r))),
        strokeWidth: data.parameters["StrokeWidth"],
        strokeOpacity: (opacity != null ? opacity(opacityColumn!.getValue(r)) : 1)
      }));


    var ist = data.parameters["InnerSizeType"];

    var innerShape = innerSizeColumn == null ? null :
      configureShape(
        ist == "Relative" ? sizeColumn :
          ist == "Absolute" ? sizeColumn :
          /*ist == "Independent" ?*/ innerSizeColumn,
        ist == "Relative" ? r => sizeColumn ? sizeColumn.getValue(r) * innerSizeColumn!.getValue(r) : 0 :
          ist == "Absolute" ? r => innerSizeColumn!.getValue(r) :
            /*ist == "Independent" ?*/ r => innerSizeColumn!.getValue(r),
        r => ({
          transform: tr,
          shapeRendering: "initial",
          fillOpacity: fillOpacity(r),
          fill: data.parameters["InnerFillColor"] || 'black'
        })
      );

    return (
      <svg direction="ltr" width={width} height={height}>
        <XKeyTicks keyColumn={horizontalColumn} keyValues={horizontalKeys} xRule={xRule} yRule={yRule} x={x} showLines={true} />
        <YKeyTicks keyColumn={verticalColumn} keyValues={verticalKeys} xRule={xRule} yRule={yRule} y={y} showLines={true} showLabels={true}  />

        {data.rows.map((r, i) =>
          <g key={i.toString()} className="chart-groups"
            cursor="pointer"
            onClick={e => this.props.onDrillDown(r)}>
            {mainShape && mainShape.renderer(r)}
            {innerShape && innerShape.renderer(r)}
            {
              parseFloat(data.parameters["NumberOpacity"]) > 0 &&
              <text className="punch"
                transform={tr}
                x={x(horizontalColumn.getValueKey(r))!} y={-y(verticalColumn.getValueKey(r))!}
                fill={data.parameters["NumberColor"]}
                dominantBaseline="middle"
                opacity={parseFloat(data.parameters["NumberOpacity"]) * (!mainShape ? 0 : mainShape.numberOpacity!(sizeColumn ? 0 : sizeColumn!.getValue(r)))}
                textAnchor="middle"
                fontWeight="bold">
                {sizeColumn ? sizeColumn.getValueNiceName(r) :
                  innerSizeColumn != null ? (ist == "Relative" ? percentage(innerSizeColumn.getValue(r)) : innerSizeColumn.getValue(r)) :
                    colorColumn != null ? colorColumn.getValue(r) :
                      opacityColumn != null ? opacityColumn.getValue(r) : null}
              </text>
            }
            <title>
              {horizontalColumn.getValueNiceName(r) + ', ' + verticalColumn.getValueNiceName(r) +
                (sizeColumn == null ? "" : ("\n" + sizeColumn.title + ": " + sizeColumn.getValueNiceName(r))) +
                (colorColumn == null ? "" : ("\n" + colorColumn.title + ": " + colorColumn.getValueNiceName(r))) +
                (opacityColumn == null ? "" : ("\n" + opacityColumn.title + ": " + opacityColumn.getValueNiceName(r))) +
                (innerSizeColumn == null ? "" : ("\n" + innerSizeColumn.title + ": " + (ist == "Relative" ? percentage(innerSizeColumn.getValue(r)) : innerSizeColumn.getValueNiceName(r)))) +
                (orderingColumn == null ? "" : ("\n" + orderingColumn.title + ": " + orderingColumn.getValueNiceName(r)))}
            </title>
          </g>
        )}
        <XAxis xRule={xRule} yRule={yRule} />
        <YAxis xRule={xRule} yRule={yRule} />
      </svg>
    );
  }
}



function percentage(v: number) { return Math.floor(v * 10000) / 100 + "%"; }
