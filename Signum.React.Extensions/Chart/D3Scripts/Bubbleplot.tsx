import * as React from 'react'
import * as d3 from 'd3'
import * as ChartClient from '../ChartClient';
import * as ChartUtils from './Components/ChartUtils';
import { translate, scale, rotate, skewX, skewY, matrix, scaleFor } from './Components/ChartUtils';
import { ChartRow } from '../ChartClient';
import { XScaleTicks, YScaleTicks } from './Components/Ticks';
import { XAxis, YAxis } from './Components/Axis';
import TextEllipsis from './Components/TextEllipsis';
import { Rule } from './Components/Rule';
import InitialMessage from './Components/InitialMessage';


export default function renderBubbleplot({ data, width, height, parameters, loading, onDrillDown }: ChartClient.ChartScriptProps): React.ReactElement<any> {
  
  var xRule = new Rule({
    _1: 5,
    title: 15,
    _2: 5,
    labels: parseInt(parameters["UnitMargin"]),
    _3: 5,
    ticks: 4,
    content: '*',
    _4: 5,
  }, width);
  //xRule.debugX(chart)

  var yRule = new Rule({
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

  if (data == null || data.rows.length == 0)
    return (
      <svg direction="ltr" width={width} height={height}>
        <InitialMessage data={data} x={xRule.middle("content")} y={yRule.middle("content")} loading={loading} />
        <XAxis xRule={xRule} yRule={yRule} />
        <YAxis xRule={xRule} yRule={yRule} />
      </svg>
    );

  var colorKeyColumn = data.columns.c0!;
  var horizontalColumn = data.columns.c1! as ChartClient.ChartColumn<number>;
  var verticalColumn = data.columns.c2 as ChartClient.ChartColumn<number>;
  var sizeColumn = data.columns.c3 as ChartClient.ChartColumn<number>;

  var x = scaleFor(horizontalColumn, data.rows.map(r => horizontalColumn.getValue(r)), 0, xRule.size('content'), parameters["HorizontalScale"]);

  var y = scaleFor(verticalColumn, data.rows.map(r => verticalColumn.getValue(r)), 0, yRule.size('content'), parameters["VerticalScale"]);

  var xTickSize = verticalColumn.type == "Date" || verticalColumn.type == "DateTime" ? 100 : 60;

  var color: (r: ChartRow) => string;
  if (parameters["ColorScale"] == "Ordinal") {
    var scheme = ChartUtils.getColorScheme(parameters["ColorCategory"], parseInt(parameters["ColorCategorySteps"]));
    var categoryColor = d3.scaleOrdinal(scheme).domain(data.rows.map(r => colorKeyColumn.getValueKey(r)));
    color = r => colorKeyColumn.getValueColor(r) || categoryColor(colorKeyColumn.getValueKey(r));
  } else {
    var scaleFunc = scaleFor(colorKeyColumn, data.rows.map(r => colorKeyColumn.getValue(r) as number), 0, 1, parameters["ColorScale"]);
    var colorInterpolate = parameters["ColorInterpolate"];
    var colorInterpolation = ChartUtils.getColorInterpolation(colorInterpolate)!;
    color = r => colorInterpolation(scaleFunc(colorKeyColumn.getValue(r) as number))
  }
  var sizeList = data.rows.map(r => sizeColumn.getValue(r));

  var sizeTemp = scaleFor(sizeColumn, sizeList, 0, 1, parameters["SizeScale"]);

  var totalSizeTemp = d3.sum(data.rows, r => sizeTemp(sizeColumn.getValue(r)));

  var sizeScale = scaleFor(sizeColumn, sizeList, 0, (xRule.size('content') * yRule.size('content')) / (totalSizeTemp * 3), parameters["SizeScale"]);


  return (
    <svg direction="ltr" width={width} height={height}>
      <XScaleTicks xRule={xRule} yRule={yRule} valueColumn={horizontalColumn} x={x} />
      <YScaleTicks xRule={xRule} yRule={yRule} valueColumn={verticalColumn} y={y} />


      {data.rows.orderByDescending(r => sizeColumn.getValue(r)).map((r, i) => <g key={i}
        className="shape-serie"
        transform={translate(xRule.start('content'), yRule.end('content'))}
        cursor="pointer"
        onClick={e => onDrillDown(r)}
      >
        <circle className="shape"
          stroke={colorKeyColumn.getValueColor(r) || color(r)}
          strokeWidth={3} fill={colorKeyColumn.getValueColor(r) || color(r)}
          fillOpacity={parseFloat(parameters["FillOpacity"])}
          shapeRendering="initial"
          r={Math.sqrt(sizeScale(sizeColumn.getValue(r)) / Math.PI)}
          cx={x(horizontalColumn.getValue(r))} cy={-y(verticalColumn.getValue(r))} />

        {
          parameters["ShowLabel"] == 'Yes' &&
          <TextEllipsis maxWidth={Math.sqrt(sizeScale(sizeColumn.getValue(r)) / Math.PI) * 2}
            padding={0} etcText=""
            className="number-label"
            x={x(horizontalColumn.getValue(r))}
            y={-y(verticalColumn.getValue(r))}
            fill={parameters["LabelColor"] || colorKeyColumn.getValueColor(r) || color(r)}
            dominantBaseline="middle"
            textAnchor="middle"
            fontWeight="bold">
            {sizeColumn.getValueNiceName(r)}
          </TextEllipsis>
        }

        <title>
          {colorKeyColumn.getValueNiceName(r) +
            ("\n" + horizontalColumn.title + ": " + horizontalColumn.getValueNiceName(r)) +
            ("\n" + verticalColumn.title + ": " + verticalColumn.getValueNiceName(r)) +
            ("\n" + sizeColumn.title + ": " + sizeColumn.getValueNiceName(r))}
        </title>

      </g>)}

      <InitialMessage data={data} x={xRule.middle("content")} y={yRule.middle("content")} loading={loading} />
      <XAxis xRule={xRule} yRule={yRule} />
      <YAxis xRule={xRule} yRule={yRule} />
    </svg>
  );
}
