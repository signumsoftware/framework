import * as React from 'react'
import * as d3 from 'd3'
import * as ChartClient from '../ChartClient';
import * as ChartUtils from '../Templates/ChartUtils';
import { translate, scale, rotate, skewX, skewY, matrix, scaleFor, rule, ellipsis, Folder, isFolder, Root, isRoot } from '../Templates/ChartUtils';
import { ChartRow, ChartTable } from '../ChartClient';
import ReactChartBase from './ReactChartBase';
import TextEllipsis from './Components/TextEllipsis';


export default class BubblePackChart extends ReactChartBase {

  renderChart(data: ChartClient.ChartTable, width: number, height: number): React.ReactElement<any> {

    var keyColumn = data.columns.c0!;
    var valueColumn = data.columns.c1! as ChartClient.ChartColumn<number>;
    var parentColumn = data.columns.c2;
    var colorScaleColumn = data.columns.c3 as ChartClient.ChartColumn<number> | undefined;
    var colorSchemeColumn = data.columns.c4;

    if (width == 0 || height == 0)
      return <svg direction="rtl" width={width} height={height}></svg>;

    var color: (v: ChartRow) => string | undefined;
    if (colorScaleColumn) {
      var scaleFunc = scaleFor(colorScaleColumn, data.rows.map(r => colorScaleColumn!.getValue(r)), 0, 1, data.parameters["ColorScale"]);
      var colorInterpolator = ChartUtils.getColorInterpolation(data.parameters["ColorInterpolate"]);
      color = r => colorInterpolator && colorInterpolator(scaleFunc(colorScaleColumn!.getValue(r)));
    }
    else if (colorSchemeColumn) {
      var scheme = ChartUtils.getColorScheme(data.parameters["ColorCategory"], parseInt(data.parameters["ColorCategorySteps"]));
      var categoryColor = d3.scaleOrdinal(scheme).domain(data.rows.map(r => colorSchemeColumn!.getValueKey(r)));
      color = r => colorSchemeColumn!.getColor(r) || categoryColor(colorSchemeColumn!.getValueKey(r));
    }
    else {
      var scheme = ChartUtils.getColorScheme(data.parameters["ColorCategory"], parseInt(data.parameters["ColorCategorySteps"]));
      var categoryColor = d3.scaleOrdinal(scheme).domain(data.rows.map(r => keyColumn.getValueKey(r)));
      color = r => keyColumn.getValueColor(r) || categoryColor(keyColumn.getValueKey(r));
    }

    var folderColor: null | ((folder: unknown) => string) = null;
    if (parentColumn) {
      var scheme = ChartUtils.getColorScheme(data.parameters["ColorCategory"], parseInt(data.parameters["ColorCategorySteps"]));
      var categoryColor = d3.scaleOrdinal(scheme).domain(data.rows.map(r => parentColumn!.getValueKey(r)));
      folderColor = folder => parentColumn!.getColor(folder) || categoryColor(parentColumn!.getKey(folder));
    }

    var format = d3.format(",d");

    var root = ChartUtils.stratifyTokens(data, keyColumn, parentColumn);

    //root.sum(d => valueColumn.getValue(d as ChartRow))
    //.sort(function (a, b) { return b.value - a.value; })

    var size = scaleFor(valueColumn, data.rows.map(r => valueColumn.getValue(r)), 0, 1, data.parameters["Scale"]);

    root.sum(r => r == null ? 0 : size(valueColumn.getValue(r as ChartRow)));

    var bubble = d3.pack<ChartRow | Folder | Root>()
      .size([width, height])
      .padding(2);

    const circularRoot = bubble(root);
    
    var nodes = circularRoot.descendants().filter(d => !isRoot(d.data)) as d3.HierarchyCircularNode<ChartRow | Folder>[];

    var showNumber = parseFloat(data.parameters["NumberOpacity"]) > 0;
    var numberSizeLimit = parseInt(data.parameters["NumberSizeLimit"]);

    return (
      <svg direction="rtl" width={width} height={height}>
        {
          nodes.map(d => <g className="node" transform={translate(d.x, d.y)} cursor="pointer"
            onClick={e => isFolder(d.data) ? this.props.onDrillDown({ c2: d.data.folder }) : this.props.onDrillDown(d.data)}>
            <circle shapeRendering="initial" r={d.r} fill={isFolder(d.data) ? folderColor!(d.data.folder) : color(d.data)!}
              fillOpacity={data.parameters["FillOpacity"] || undefined}
              stroke={data.parameters["StrokeColor"] || (isFolder(d.data) ? folderColor!(d.data.folder) : (color(d.data) || undefined))}
              strokeWidth={data.parameters["StrokeWidth"]} strokeOpacity={1} />
            {!isFolder(d.data) &&
              <TextEllipsis maxWidth={d.r * 2} padding={1}
                dominantBaseline="central" textAnchor="middle" dy={showNumber && d.r > numberSizeLimit ? "-0.5em" : undefined}>
                {keyColumn.getValueNiceName(d.data as ChartRow)}
              </TextEllipsis>
            }
            {showNumber && d.r > numberSizeLimit && !isFolder(d.data) &&
              <text fill={data.parameters["NumberColor"] || "#000"}
                dominantBaseline="central"
                textAnchor="middle"
                fontWeight="bold"
                opacity={parseFloat(data.parameters["NumberOpacity"]) * d.r / 30}
                dy=".5em">
                {valueColumn.getValueNiceName(d.data as ChartRow)}
              </text>
            }
            <title>
              {isFolder(d.data) ? parentColumn!.getNiceName(d.data.folder) :
                (keyColumn.getValueNiceName(d.data as ChartRow) + (parentColumn == null ? '' : (' (' + parentColumn.getValueNiceName(d.data as ChartRow) + ')')))}:
              {isFolder(d.data) ? format(size.invert(d.value!)) :
                (valueColumn.getValueNiceName(d.data)
                  + (colorScaleColumn == null ? '' : (' (' + colorScaleColumn.getValueNiceName(d.data) + ')'))
                  + (colorSchemeColumn == null ? '' : (' (' + colorSchemeColumn.getValueNiceName(d.data) + ')'))
                )}
            </title>
          </g>)
        }
      </svg>
    );
  }
}
