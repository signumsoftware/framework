import * as React from 'react'
import * as d3 from 'd3'
import { ChartClient, ChartColumn, ChartRow, ChartScriptProps } from '../ChartClient';
import * as ChartUtils from './Components/ChartUtils';
import { translate, scale, rotate, skewX, skewY, matrix, scaleFor } from './Components/ChartUtils';
import { XScaleTicks, YScaleTicks } from './Components/Ticks';
import { XAxis, YAxis } from './Components/Axis';
import TextEllipsis from './Components/TextEllipsis';
import { Rule } from './Components/Rule';
import InitialMessage from './Components/InitialMessage';


export default function renderBubbleplot({ data, width, height, parameters, loading, onDrillDown, initialLoad, memo, dashboardFilter, chartRequest }: ChartScriptProps): React.ReactElement<any> {

  var xRule = Rule.create({
    _1: 5,
    title: 15,
    _2: 5,
    labels: parseInt(parameters["UnitMargin"]),
    _3: 5,
    ticks: 4,
    content: '*',
    _margin: parameters["RightMargin"],
    _4: 5,
  }, width);
  //xRule.debugX(chart)

  var yRule = Rule.create({
    _1: 5,
    _topMargin: parameters["TopMargin"],
    content: '*',
    ticks: 4,
    _2: 5,
    labels: 10,
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

  var keyColumn = data.columns.c0!;
  var horizontalColumn = data.columns.c1! as ChartColumn<number>;
  var verticalColumn = data.columns.c2 as ChartColumn<number>;
  var sizeColumn = data.columns.c3 as ChartColumn<number>;
  var colorScaleColumn = data.columns.c4 as ChartColumn<number> | undefined;
  var colorSchemeColumn = data.columns.c5;

  var x = scaleFor(horizontalColumn, data.rows.map(r => horizontalColumn.getValue(r)), 0, xRule.size('content'), parameters["HorizontalScale"]);

  var y = scaleFor(verticalColumn, data.rows.map(r => verticalColumn.getValue(r)), 0, yRule.size('content'), parameters["VerticalScale"]);

  var orderRows = data.rows.orderBy(r => keyColumn.getValueKey(r));
  var color: (r: ChartRow) => string | undefined;
  if (colorScaleColumn) {
    var scaleFunc = scaleFor(colorScaleColumn, data.rows.map(r => colorScaleColumn!.getValue(r)), 0, 1, parameters["ColorScale"]);
    var colorInterpolator = ChartUtils.getColorInterpolation(parameters["ColorInterpolate"]);
    color = r => colorInterpolator && colorInterpolator(scaleFunc(colorScaleColumn!.getValue(r))!);
  }
  else if (colorSchemeColumn) {
    var categoryColor = ChartUtils.colorCategory(parameters, data.rows.map(r => colorSchemeColumn!.getValueKey(r)), memo, "colorSchemeColumn");
    color = r => colorSchemeColumn!.getColor(r) ?? categoryColor(colorSchemeColumn!.getValueKey(r));
  }
  else {
    var categoryColor = ChartUtils.colorCategory(parameters, data.rows.map(r => keyColumn.getValueKey(r)), memo, "colorCategory");
    color = r => keyColumn.getValueColor(r) ?? categoryColor(keyColumn.getValueKey(r));
  }

  var sizeList = data.rows.map(r => sizeColumn.getValue(r));

  var sizeTemp = scaleFor(sizeColumn, sizeList, 0, 1, parameters["SizeScale"]);

  var totalSizeTemp = d3.sum(data.rows, r => sizeTemp(sizeColumn.getValue(r)));

  var sizeScale = scaleFor(sizeColumn, sizeList, 0, (xRule.size('content') * yRule.size('content')) / (totalSizeTemp * 3), parameters["SizeScale"]);

  var keyColumns: ChartColumn<any>[] = data.columns.entity ? [data.columns.entity] :
    [keyColumn, horizontalColumn, verticalColumn].filter(a => a.token && a.token.queryTokenType != "Aggregate")

  var detector = ChartClient.getActiveDetector(dashboardFilter, chartRequest);

  return (
    <svg direction="ltr" width={width} height={height}>
      <g opacity={dashboardFilter ? .5 : undefined}>
        <XScaleTicks xRule={xRule} yRule={yRule} valueColumn={horizontalColumn} x={x} />
        <YScaleTicks xRule={xRule} yRule={yRule} valueColumn={verticalColumn} y={y} />
      </g>
      <g className="panel" transform={translate(xRule.start('content'), yRule.end('content'))}>
        {orderRows.map(r => {
          const active = detector?.(r);

          return (
            <g key={keyColumns.map(c => c.getValueKey(r)).join("/")}
              className="shape-serie sf-transition hover-group"
              opacity={active == false ? .5 : undefined}
              transform={translate(x(horizontalColumn.getValue(r))!, -y(verticalColumn.getValue(r))!) + (initialLoad ? scale(0, 0) : scale(1, 1))}
              cursor="pointer"
              onClick={e => onDrillDown(r, e)}
            >
              <circle className="shape sf-transition hover-target"
                stroke={active == true ? "var(--bs-body-color)" : keyColumn.getValueColor(r) ?? color(r)}
                strokeWidth={3} fill={keyColumn.getValueColor(r) ?? color(r)}
                fillOpacity={parseFloat(parameters["FillOpacity"])}
                shapeRendering="initial"
                r={Math.sqrt(sizeScale(sizeColumn.getValue(r))! / Math.PI)} />

              {
                parameters["ShowLabel"] == 'Yes' &&
                <TextEllipsis maxWidth={Math.sqrt(sizeScale(sizeColumn.getValue(r))! / Math.PI) * 2}
                  padding={0} etcText=""
                  className="number-label"
                  fill={parameters["LabelColor"] ?? keyColumn.getValueColor(r) ?? color(r)}
                  dominantBaseline="middle"
                  textAnchor="middle"
                  fontWeight="bold">
                  {sizeColumn.getValueNiceName(r)}
                </TextEllipsis>
              }

              <title>
                {keyColumn.getValueNiceName(r) +
                  ("\n" + horizontalColumn.title + ": " + horizontalColumn.getValueNiceName(r)) +
                  ("\n" + verticalColumn.title + ": " + verticalColumn.getValueNiceName(r)) +
                  ("\n" + sizeColumn.title + ": " + sizeColumn.getValueNiceName(r))}
              </title>

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
