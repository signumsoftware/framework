import * as React from 'react'
import * as d3 from 'd3'
import * as ChartClient from '../ChartClient';
import * as ChartUtils from './Components/ChartUtils';
import { translate, scale, rotate, skewX, skewY, matrix, scaleFor } from './Components/ChartUtils';
import { ChartRow } from '../ChartClient';
import { YScaleTicks, XScaleTicks } from './Components/Ticks';
import { XAxis, YAxis } from './Components/Axis';
import { Rule } from './Components/Rule';
import InitialMessage from './Components/InitialMessage';


export default function renderScatterplot({ data, width, height, parameters, loading, onDrillDown, initialLoad }: ChartClient.ChartScriptProps): React.ReactElement<any> {

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
  var verticalColumn = data.columns.c2! as ChartClient.ChartColumn<number>;

  var x = scaleFor(horizontalColumn, data.rows.map(horizontalColumn.getValue), 0, xRule.size('content'), parameters["HorizontalScale"]);

  var y = scaleFor(verticalColumn, data.rows.map(verticalColumn.getValue), 0, yRule.size('content'), parameters["VerticalScale"]);

  var pointSize = parseInt(parameters["PointSize"]);

  var color: (val: ChartRow) => string;
  if (parameters["ColorScale"] == "Ordinal" || (colorKeyColumn.type != "Integer" && colorKeyColumn.type != "Real")) {
    var categoryColor = ChartUtils.colorCategory(parameters, data.rows.map(colorKeyColumn.getValueKey));
    color = r => colorKeyColumn.getValueColor(r) || categoryColor(colorKeyColumn.getValueKey(r));

  } else {
    var scaleFunc = scaleFor(colorKeyColumn, data.rows.map(colorKeyColumn.getValue) as number[], 0, 1, parameters["ColorScale"]);
    var colorInterpolate = parameters["ColorInterpolate"];
    var colorInterpolation = ChartUtils.getColorInterpolation(colorInterpolate);
    color = r => colorInterpolation!(scaleFunc(colorKeyColumn.getValue(r) as number));
  }

  return (
    <>
      <svg direction="ltr" width={width} height={height}>
        <XScaleTicks xRule={xRule} yRule={yRule} valueColumn={horizontalColumn} x={x} />
        <YScaleTicks xRule={xRule} yRule={yRule} valueColumn={verticalColumn} y={y} />

        {parameters["DrawingMode"] == "Svg" &&
          data.rows.map((r, i) => <g key={i} className="shape-serie sf-transition"
            transform={translate(xRule.start('content'), yRule.end('content')) + (initialLoad ? scale(1, 0) : scale(1, 1))}>
            <circle className="shape sf-transition"
              transform={translate(x(horizontalColumn.getValue(r)), -y(verticalColumn.getValue(r)))}
              stroke={colorKeyColumn.getValueColor(r) || color(r)}
              fill={colorKeyColumn.getValueColor(r) || color(r)}
              shapeRendering="initial"
              r={pointSize}
              onClick={e => onDrillDown(r)}
              cursor="pointer">
              <title>
                {colorKeyColumn.getValueNiceName(r) +
                  ("\n" + horizontalColumn.title + ": " + horizontalColumn.getValueNiceName(r)) +
                  ("\n" + verticalColumn.title + ": " + verticalColumn.getValueNiceName(r))}
              </title>
            </circle>
          </g>)
        }

        <InitialMessage data={data} x={xRule.middle("content")} y={yRule.middle("content")} loading={loading} />

        <XAxis xRule={xRule} yRule={yRule} />
        <YAxis xRule={xRule} yRule={yRule} />
      </svg>
      {parameters["DrawingMode"] != "Svg" &&
        <CanvasScatterplot
          color={color}
          colorKeyColumn={colorKeyColumn}
          horizontalColumn={horizontalColumn}
          verticalColumn={verticalColumn}
          onDrillDown={onDrillDown}
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

class CanvasScatterplot extends React.Component<{
  xRule: Rule,
  yRule: Rule,
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

