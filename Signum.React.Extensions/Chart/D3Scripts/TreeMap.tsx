import * as React from 'react'
import * as d3 from 'd3'
import * as ChartClient from '../ChartClient';
import * as ChartUtils from '../Templates/ChartUtils';
import { translate, scale, rotate, skewX, skewY, matrix, scaleFor, rule, ellipsis, Folder, Root, isFolder } from '../Templates/ChartUtils';
import { ChartRow } from '../ChartClient';
import ReactChartBase from './ReactChartBase';
import TextEllipsis from './Components/TextEllipsis';


export default class TreeMapChart extends ReactChartBase {

  renderChart(data: ChartClient.ChartTable, width: number, height: number): React.ReactElement<any> {

    var keyColumn = data.columns.c0!;
    var valueColumn = data.columns.c1! as ChartClient.ChartColumn<number>;
    var parentColumn = data.columns.c2;
    var colorScaleColumn = data.columns.c3 as ChartClient.ChartColumn<number> | undefined;
    var colorSchemeColumn = data.columns.c4;

    if (width == 0 || height == 0)
      return <svg direction="rtl" width={width} height={height} />;

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

    var root = ChartUtils.stratifyTokens(data, keyColumn, parentColumn);

    var size = scaleFor(valueColumn, data.rows.map(r => valueColumn.getValue(r)), 0, 1, data.parameters["Scale"]);

    root.sum(r => r == null ? 0 : size(valueColumn.getValue(r as ChartRow)));

    var opacity = parentColumn ? parseFloat(data.parameters["Opacity"]) : 1;
    var padding = parentColumn ? parseInt(data.parameters["Padding"]) : 1;
    var p2 = padding / 2;

    var bubble = d3.treemap<ChartRow | Folder | Root>()
      .size([width, height])
      .round(true)
      .padding(padding);

    const treeMapRoot = bubble(root);

    var nodes = treeMapRoot.descendants().filter(d => !!d.data);

    const nodeHeight = (n: d3.HierarchyRectangularNode<any>) => n.y1 - n.y0;
    const nodeWidth = (n: d3.HierarchyRectangularNode<any>) => n.x1 - n.x0;

    var showNumber = parseFloat(data.parameters["NumberOpacity"]) > 0;

    return (
      <svg direction="rtl" width={width} height={height} >
        {nodes.map((d, i) =>
          <g key={i} className="node" transform={translate(d.x0 - p2, d.y0 - p2)}>
            {isFolder(d.data) &&
              <rect shapeRendering="initial"
                width={nodeWidth(d)}
                height={nodeHeight(d)}
                fill={parentColumn!.getColor((d.data as Folder).folder) || folderColor!((d.data as Folder).folder)}
                onClick={e => this.props.onDrillDown({ c2: (d.data as Folder).folder })} cursor="pointer">
                <title>
                  {folderColor!(((d.data as Folder).folder))}
                </title>
              </rect>
            }
            {!isFolder(d.data) &&
              <rect shapeRendering="initial" opacity={opacity}
                width={nodeWidth(d)}
                height={nodeHeight(d)}
                fill={color(d.data as ChartRow)!}
                onClick={e => this.props.onDrillDown(d.data as ChartRow)}
                cursor="pointer">
                <title>
                  {keyColumn.getValueNiceName(d.data as ChartRow) + ': ' + valueColumn.getValueNiceName(d.data as ChartRow)}
                </title>
              </rect>}

            {!isFolder(d.data) && nodeWidth(d) > 10 && nodeHeight(d) > 25 &&
              <TextEllipsis maxWidth={nodeWidth(d)} padding={4} etcText=""
                textAnchor="middle"
                dominantBaseline="middle"
                dx={nodeWidth(d) / 2}
                dy={nodeHeight(d) / 2 + (showNumber ? -6 : 0)}
                onClick={e => this.props.onDrillDown(d.data as ChartRow)} cursor="pointer">
                {keyColumn.getValueNiceName(d.data as ChartRow)}
                <title>
                  {keyColumn.getValueNiceName(d.data as ChartRow) + ': ' + valueColumn.getValueNiceName(d.data as ChartRow)}
                </title>
              </TextEllipsis>
            }

            {!isFolder(d.data) && nodeWidth(d) > 10 && nodeHeight(d) > 25 && showNumber &&
              <TextEllipsis maxWidth={nodeWidth(d)} padding={1} etcText=""
                fill={data.parameters["NumberColor"] || "#fff"}
                dominantBaseline="central"
                opacity={parseFloat(data.parameters["NumberOpacity"])}
                textAnchor="middle"
                fontWeight="bold"
                dx={nodeWidth(d) / 2}
                dy={nodeHeight(d) / 2 + 6}
                onClick={e => this.props.onDrillDown(d.data as ChartRow)} cursor="pointer">
                {valueColumn.getValueNiceName(d.data as ChartRow)}
              </TextEllipsis>
            }
          </g>)
        }
      </svg>
    );
  }
}
