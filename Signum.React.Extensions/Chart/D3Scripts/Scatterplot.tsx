import * as React from 'react'
import * as d3 from 'd3'
import D3ChartBase from './D3ChartBase';
import * as ChartClient from '../ChartClient';
import * as ChartUtils from '../Templates/ChartUtils';
import { translate, scale, rotate, skewX, skewY, matrix, scaleFor, rule, ellipsis } from '../Templates/ChartUtils';
import { ChartRow } from '../ChartClient';
import ReactChartBase from './ReactChartBase';
import { YScaleTicks, XScaleTicks } from './Components/Ticks';
import { XAxis, YAxis } from './Components/Axis';


export default class ScatterplotChart extends ReactChartBase {

  renderChart(data: ChartClient.ChartTable, width: number, height: number): React.ReactNode {

    var colorKeyColumn = data.columns.c0!;
    var horizontalColumn = data.columns.c1! as ChartClient.ChartColumn<number>;
    var verticalColumn = data.columns.c2! as ChartClient.ChartColumn<number>;

    var xRule = rule({
      _1: 5,
      title: 15,
      _2: 5,
      labels: parseInt(data.parameters["UnitMargin"]),
      _3: 5,
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

    var x = scaleFor(horizontalColumn, data.rows.map(horizontalColumn.getValue), 0, xRule.size('content'), data.parameters["HorizontalScale"]);

    var y = scaleFor(verticalColumn, data.rows.map(verticalColumn.getValue), 0, yRule.size('content'), data.parameters["VerticalScale"]);


    var pointSize = parseInt(data.parameters["PointSize"]);

    var numXTicks = horizontalColumn.type == 'Date' || horizontalColumn.type == 'DateTime' ? 100 : 60;

    var xTicks = x.ticks(width / numXTicks);
    var xTickFormat = x.tickFormat(width / numXTicks);


    var color: (val: ChartRow) => string;
    if (data.parameters["ColorScale"] == "Ordinal") {
      var scheme = ChartUtils.getColorScheme(data.parameters["ColorCategory"], parseInt(data.parameters["ColorCategorySteps"]));
      var categoryColor = d3.scaleOrdinal(scheme).domain(data.rows.map(colorKeyColumn.getValueKey));
      color = r => colorKeyColumn.getValueColor(r) || categoryColor(colorKeyColumn.getValueKey(r));

    } else {
      var scaleFunc = scaleFor(colorKeyColumn, data.rows.map(colorKeyColumn.getValue) as number[], 0, 1, data.parameters["ColorScale"]);
      var colorInterpolate = data.parameters["ColorInterpolate"];
      var colorInterpolation = ChartUtils.getColorInterpolation(colorInterpolate);
      color = r => colorInterpolation!(scaleFunc(colorKeyColumn.getValue(r) as number));
    }

    return (
      <>
        <svg direction="rtl" width={width} height={height}>
          <XScaleTicks xRule={xRule} yRule={yRule} valueColumn={horizontalColumn} x={x} />
          <YScaleTicks xRule={xRule} yRule={yRule} valueColumn={verticalColumn} y={y} />

          {data.parameters["DrawingMode"] == "Svg" &&
            data.rows.map((r, i) => <g key={i} className="shape-serie"
              transform={translate(xRule.start('content'), yRule.end('content'))}>
              <circle className="shape" stroke={colorKeyColumn.getValueColor(r) || color(r)} fill={colorKeyColumn.getValueColor(r) || color(r)} shapeRendering="initial" r={pointSize} cx={x(horizontalColumn.getValue(r))} cy={-y(verticalColumn.getValue(r))} onClick={e => this.props.onDrillDown(r)} cursor="pointer">
                <title>
                  {colorKeyColumn.getValueNiceName(r) +
                    ("\n" + horizontalColumn.title + ": " + horizontalColumn.getValueNiceName(r)) +
                    ("\n" + verticalColumn.title + ": " + verticalColumn.getValueNiceName(r))}
                </title>
              </circle>
            </g>)
          }

          <XAxis xRule={xRule} yRule={yRule} />
          <YAxis xRule={xRule} yRule={yRule} />
        </svg>
        {data.parameters["DrawingMode"] != "Svg" &&
          <CanvasScatterplot
            color={color}
            colorKeyColumn={colorKeyColumn}
            horizontalColumn={horizontalColumn}
            verticalColumn={verticalColumn}
            onDrillDown={this.props.onDrillDown}
            data={data}
            x={x}
            y={y}
            pointSize={pointSize}
            xRule={xRule}
            yRule={yRule}
          />
        }
      </>
    );
  }
}

class CanvasScatterplot extends React.Component<{
  xRule: ChartUtils.Rule,
  yRule: ChartUtils.Rule,
  colorKeyColumn: ChartClient.ChartColumn<unknown>,
  horizontalColumn: ChartClient.ChartColumn<number>,
  verticalColumn: ChartClient.ChartColumn<number>,
  pointSize: number,
  data: ChartClient.ChartTable,
  onDrillDown: (e: ChartRow) => void,
  color: (val: ChartRow) => string,
  x: d3.ScaleContinuousNumeric<number, number>,
  y: d3.ScaleContinuousNumeric<number, number>,
}> {

  componentDidMount() {
    var { xRule, yRule, horizontalColumn, verticalColumn, colorKeyColumn, data, pointSize, onDrillDown, color, x, y } = this.props;

    var w = xRule.size('content');
    var h = yRule.size('content');
    var c = this.c!;
    var vc = this.vc!;

    const ctx = c.getContext("2d")!;
    const vctx = vc.getContext("2d")!;
    var colorToData: { [key: string]: ChartRow } = {};
    ctx.clearRect(0, 0, w, h);
    vctx.clearRect(0, 0, w, h);
    data.rows.forEach((r, i) => {

      var c = colorKeyColumn.getValueColor(r) || color(r);

      ctx.fillStyle = c;
      ctx.strokeStyle = c;
      var vColor = getVirtualColor(i);
      vctx.fillStyle = vColor;
      vctx.strokeStyle = vColor;
      colorToData[vColor] = r;

      var xVal = x(horizontalColumn.getValue(r));
      var yVal = h - y(verticalColumn.getValue(r));

      ctx.beginPath();
      ctx.arc(xVal, yVal, pointSize, 0, 2 * Math.PI);
      ctx.fill();
      ctx.stroke();

      vctx.beginPath();
      vctx.arc(xVal, yVal, pointSize, 0, 2 * Math.PI);
      vctx.fill();
      vctx.stroke();

    });

    var getVirtualColor = (index: number): string => d3.rgb(
      Math.floor(index / 256 / 256) % 256,
      Math.floor(index / 256) % 256,
      index % 256)
      .toString();

    c.addEventListener('mousemove', function (e) {
      const imageData = vctx.getImageData(e.offsetX, e.offsetY, 1, 1);
      const color = d3.rgb.apply(null, imageData.data).toString();
      const r = colorToData[color];
      if (r) {
        c.style.cursor = "pointer";
        c.setAttribute("title", colorKeyColumn.getNiceName(r) +
          ("\n" + horizontalColumn.title + ": " + horizontalColumn.getValueNiceName(r)) +
          ("\n" + verticalColumn.title + ": " + verticalColumn.getValueNiceName(r)));
      } else {
        c.style.cursor = "initial";
        c.setAttribute("title", "...");
      }
    });

    c.addEventListener('mouseup', e => {
      const imageData = vctx.getImageData(e.offsetX, e.offsetY, 1, 1);

      const color = d3.rgb.apply(null, imageData.data).toString();
      const p = colorToData[color];
      if (p) {
        onDrillDown(p);
      }
    });
  }

  c?: HTMLCanvasElement | null;
  vc?: HTMLCanvasElement | null;

  render() {

    var { xRule, yRule } = this.props;

    var w = xRule.size('content');
    var h = yRule.size('content');
    return (
      <>
        <canvas ref={c => this.c = c} style={{ width: w, height: h, position: "absolute", left: xRule.start('content') + 'px', top: yRule.start('content') + 'px' }} />
        <canvas ref={c => this.vc = c} style={{ width: w, height: h, position: "absolute", left: xRule.start('content') + 'px', top: yRule.start('content') + 'px' }} />
      </>
    );
  }
}

