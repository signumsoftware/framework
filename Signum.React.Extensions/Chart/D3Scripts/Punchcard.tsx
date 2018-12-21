import * as React from 'react'
import * as d3 from 'd3'
import * as ChartClient from '../ChartClient';
import * as ChartUtils from './Components/ChartUtils';
import { translate, scale, rotate, skewX, skewY, matrix, scaleFor } from './Components/ChartUtils';
import { ChartColumn, ChartRow } from '../ChartClient';
import { Dic } from '../../../../Framework/Signum.React/Scripts/Globals';
import { XKeyTicks, YKeyTicks } from './Components/Ticks';
import { XAxis, YAxis } from './Components/Axis';
import { Rule } from './Components/Rule';
import InitialMessage from './Components/InitialMessage';


export default function renderPunchcard({ data, width, height, parameters, loading, onDrillDown, initialLoad }: ChartClient.ChartScriptProps): React.ReactElement<any> {

  var xRule = new Rule({
    _1: 5,
    title: 15,
    _2: 10,
    labels: parseInt(parameters["XMargin"]),
    _3: 5,
    ticks: 4,
    content: '*',
    _4: 10,
  }, width);
  //xRule.debugX(chart)

  var yRule = new Rule({
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

  if (data == null || data.rows.length == 0)
    return (
      <svg direction="ltr" width={width} height={height}>
        <InitialMessage data={data} x={xRule.middle("content")} y={yRule.middle("content")} loading={loading} />
        <XAxis xRule={xRule} yRule={yRule} />
        <YAxis xRule={xRule} yRule={yRule} />
      </svg>
    );

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

  var horizontalKeys = groupAndSort(data.rows, parameters["XSort"]!, horizontalColumn, parameters['CompleteHorizontalValues']);
  var verticalKeys = groupAndSort(data.rows, parameters["YSort"]!, verticalColumn, parameters['CompleteVerticalValues']);



  var x = d3.scaleBand()
    .domain(horizontalKeys.map(horizontalColumn.getKey))
    .range([0, xRule.size('content')]);

  var y = d3.scaleBand()
    .domain(verticalKeys.map(verticalColumn.getKey))
    .range([0, yRule.size('content')]);

  var color: null | ((row: number) => string) = null;
  if (colorColumn != null) {
    var scaleFunc = scaleFor(colorColumn, data.rows.map(colorColumn.getValue), 0, 1, parameters["ColorScale"]);
    var colorInterpolator = ChartUtils.getColorInterpolation(parameters["ColorInterpolate"]);
    color = v => colorInterpolator!(scaleFunc(v))
  }

  var opacity: null | ((row: number) => number) = null;
  if (opacityColumn != null) {
    opacity = scaleFor(opacityColumn, data.rows.map(opacityColumn.getValue), 0, 1, parameters["OpacityScale"]);
  }

  var shape = parameters["Shape"];
  var innerSize = null
  if (innerSizeColumn != null) {
    innerSize = scaleFor(innerSizeColumn, data.rows.map(innerSizeColumn.getValue), 0, 100, parameters["OpacityScale"])
  }

  var scaleTransform = initialLoad ? scale(0, 0) : scale(1, 1);


  function configureShape(column: ChartColumn<number> | undefined, rowValue: (r: ChartRow) => number, extra: (r: ChartRow) => React.SVGAttributes<SVGElement>): { numberOpacity: (val: number) => number, renderer: (r: ChartRow) => React.ReactElement<any> } | undefined {

    if (shape == "Circle") {

      var circleSize = Math.min(x.bandwidth(), y.bandwidth()) * 0.45;
      var area: (n: number) => number = column == null ?
        (() => circleSize * circleSize) :
        scaleFor(column, data!.rows.map(column.getValue), 0, circleSize * circleSize, parameters["SizeScale"]);

      return {
        numberOpacity: n => { return area(n) / (15 * 15); },
        renderer: r => <circle
          transform={translate(
            x(horizontalColumn.getValueKey(r))!,
            -y(verticalColumn.getValueKey(r))!
          ) + scaleTransform}
          r={Math.sqrt(area(rowValue(r)))}
          {...extra(r)} />
      };
    } else if (shape == "Rectangle") {

      var area: (n: number) => number = column == null ?
        (() => x.bandwidth() * y.bandwidth()) :
        scaleFor(column, data!.rows.map(column.getValue), 0, x.bandwidth() * y.bandwidth(), parameters["SizeScale"]);
      var ratio = x.bandwidth() / y.bandwidth();
      var recWidth = (r: ChartRow) => Math.sqrt(area(rowValue(r)) * ratio);
      var recHeight = (r: ChartRow) => Math.sqrt(area(rowValue(r)) / ratio);

      return {
        numberOpacity: n => area(n) / (22 * 22),
        renderer: r => <rect transform={translate(
          x(horizontalColumn.getValueKey(r))! - recWidth(r) / 2,
          -y(verticalColumn.getValueKey(r))! - recHeight(r) / 2
        ) + scaleTransform}
          width={recWidth(r)}
          height={recHeight(r)}
          {...extra(r)}
        />
      };
    } else if (shape == "ProgressBar") {
      var progressWidth: (n: number) => number = column == null ?
        () => x.bandwidth() :
        scaleFor(column, data!.rows.map(column.getValue), 0, x.bandwidth(), parameters["SizeScale"]);

      return {
        numberOpacity: n => 1,
        renderer: r => <rect transform={translate(
            x(horizontalColumn.getValueKey(r))! - x.bandwidth() / 2,
            -y(verticalColumn.getValueKey(r))! - y.bandwidth() / 2
        ) + scaleTransform}
          width={progressWidth(rowValue(r))}
          height={y.bandwidth()}
          {...extra(r)} />
      };
    }
    else
      return undefined;
  }

  var fillOpacity = (r: ChartRow) => parseFloat(parameters["FillOpacity"]) * (opacity != null ? opacity(opacityColumn!.getValue(r)) : 1);
  
  var mainShape = configureShape(sizeColumn, r => sizeColumn ? sizeColumn.getValue(r) : 0,
    r => ({
      className:"punch sf-transition",
      shapeRendering: "initial",
      fillOpacity: fillOpacity(r),
      fill: color == null ? (parameters["FillColor"] || 'black') : color(colorColumn!.getValue(r)),
      stroke: parameters["StrokeColor"] || (color == null ? 'black' : color(colorColumn!.getValue(r))),
      strokeWidth: parameters["StrokeWidth"],
      strokeOpacity: (opacity != null ? opacity(opacityColumn!.getValue(r)) : 1)
    }));


  var ist = parameters["InnerSizeType"];

  var innerShape = innerSizeColumn == null ? null :
    configureShape(
      ist == "Relative" ? sizeColumn :
        ist == "Absolute" ? sizeColumn :
          /*ist == "Independent" ?*/ innerSizeColumn,
      ist == "Relative" ? r => sizeColumn ? sizeColumn.getValue(r) * innerSizeColumn!.getValue(r) : 0 :
        ist == "Absolute" ? r => innerSizeColumn!.getValue(r) :
            /*ist == "Independent" ?*/ r => innerSizeColumn!.getValue(r),
      r => ({
        className: "punch-inner sf-transition",
        shapeRendering: "initial",
        fillOpacity: fillOpacity(r),
        fill: parameters["InnerFillColor"] || 'black'
      })
    );

  return (
    <svg direction="ltr" width={width} height={height}>
      <XKeyTicks keyColumn={horizontalColumn} keyValues={horizontalKeys} xRule={xRule} yRule={yRule} x={x} showLines={true} />
      <YKeyTicks keyColumn={verticalColumn} keyValues={verticalKeys} xRule={xRule} yRule={yRule} y={y} showLines={true} showLabels={true} />
      <g className="punch-panel" transform={translate(xRule.start('content') + x.bandwidth() / 2, yRule.end('content') - y.bandwidth() / 2)}>
      {data.rows
        .orderBy(horizontalColumn.getValueKey)
        .orderBy(verticalColumn.getValueKey)
        .map(r =>
          <g key={horizontalColumn.getValueKey(r) + "-" + verticalColumn.getValueKey(r)} className="chart-groups sf-transition"
          cursor="pointer"
          onClick={e => onDrillDown(r)}>
          {mainShape && mainShape.renderer(r)}
          {innerShape && innerShape.renderer(r)}
          {
              parseFloat(parameters["NumberOpacity"]) > 0 &&
              <text className="punch-text sf-transition" transform={translate(
                x(horizontalColumn.getValueKey(r))!,
                -y(verticalColumn.getValueKey(r))!
              )}
              fill={parameters["NumberColor"]}
              dominantBaseline="middle"
              opacity={parseFloat(parameters["NumberOpacity"]) * (!mainShape ? 0 : mainShape.numberOpacity!(!sizeColumn ? 0 : sizeColumn.getValue(r)))}
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
      </g>>
      <XAxis xRule={xRule} yRule={yRule} />
      <YAxis xRule={xRule} yRule={yRule} />
    </svg>
  );
}

function percentage(v: number) { return Math.floor(v * 10000) / 100 + "%"; }
