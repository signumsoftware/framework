import * as React from 'react'
import * as d3 from 'd3'
import { ChartClient, ChartColumn, ChartRow, ChartScriptProps, ChartTable } from '../ChartClient';
import * as ChartUtils from './Components/ChartUtils';
import { translate, scale, rotate, skewX, skewY, matrix, scaleFor } from './Components/ChartUtils';
import { YScaleTicks, XScaleTicks } from './Components/Ticks';
import { XAxis, YAxis } from './Components/Axis';
import { Rule } from './Components/Rule';
import InitialMessage from './Components/InitialMessage';
import { ChartMessage, ChartRequestModel, D3ChartScript } from '../Signum.Chart';
import { DashboardFilter } from '../../Signum.Dashboard/View/DashboardFilterController';
import { symbolNiceName, getQueryNiceName } from '@framework/Reflection';


export default function renderScatterplot({ data, width, height, parameters, loading, onDrillDown, initialLoad, memo, chartRequest, dashboardFilter }: ChartScriptProps): React.ReactElement<any> {

  var xRule = Rule.create({
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

  var yRule = Rule.create({
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


  var keyColumn = data.columns.c0!;
  var horizontalColumn = data.columns.c1! as ChartColumn<number>;
  var verticalColumn = data.columns.c2! as ChartColumn<number>;
  var horizontalColumn2 = data.columns.c3 as ChartColumn<number> | undefined;
  var verticalColumn2 = data.columns.c4 as ChartColumn<number> | undefined;
  var colorScaleColumn = data.columns.c5 as ChartColumn<number> | undefined;
  var colorSchemeColumn = data.columns.c6;

  if (horizontalColumn2 && horizontalColumn2.type != horizontalColumn.type)
    throw new Error(`The type of Horizontal Column (2) ${horizontalColumn2.token} (${horizontalColumn2.type}) doesn't match the one from Horizontal Column ${horizontalColumn.token} (${horizontalColumn.type})`);


  if (verticalColumn2 && verticalColumn2.type != verticalColumn2.type)
    throw new Error(`The type of Vertical Column (2) ${verticalColumn2.token} (${verticalColumn2.type}) doesn't match the one from Vertical Column ${verticalColumn.token} (${verticalColumn.type})`);

  var x = scaleFor(horizontalColumn, data.rows.map(horizontalColumn.getValue).concat(horizontalColumn2 ? data.rows.map(horizontalColumn2.getValue) : []), 0, xRule.size('content'), parameters["HorizontalScale"]);
  var y = scaleFor(verticalColumn, data.rows.map(verticalColumn.getValue).concat(verticalColumn2 ? data.rows.map(verticalColumn2.getValue) : []), 0, yRule.size('content'), parameters["VerticalScale"]);

  var pointSize = parseInt(parameters["PointSize"]);

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

  var keyColumns: ChartColumn<any>[] = data.columns.entity ? [data.columns.entity] :
    [keyColumn, horizontalColumn, verticalColumn].filter(a => a.token && a.token.queryTokenType != "Aggregate")

  var aggregateColumns: ChartColumn<any>[] = data.columns.entity ? [data.columns.entity] :
    [keyColumn, horizontalColumn, verticalColumn, horizontalColumn2, verticalColumn2].filter(cn => cn != undefined).filter(a => a.token && a.token.queryTokenType == "Aggregate")

  var titleMessage = (aggregateColumns.length != 0) ?
    ChartMessage._0Of1_2Per3.niceToString(symbolNiceName(D3ChartScript.Scatterplot), getQueryNiceName(chartRequest.queryKey), keyColumns.map(cn => cn.title).join(", "), aggregateColumns.map(cn => cn.title).join(", ")) :
    ChartMessage._0Of1_2.niceToString(symbolNiceName(D3ChartScript.Scatterplot), getQueryNiceName(chartRequest.queryKey), keyColumns.map(cn => cn.title).join(", "));
  return (
    <>
      <svg direction="ltr" width={width} height={height} role="img">
        <title id="scatterplotlCoodinatesChartTitle">{titleMessage}</title>
        <g opacity={dashboardFilter ? .5 : undefined}>
          <XScaleTicks xRule={xRule} yRule={yRule} valueColumn={horizontalColumn} x={x} />
          <YScaleTicks xRule={xRule} yRule={yRule} valueColumn={verticalColumn} y={y} />
        </g>
        {parameters["DrawingMode"] == "Svg" &&
          <SvgScatterplot data={data} keyColumns={keyColumns} xRule={xRule} yRule={yRule} initialLoad={initialLoad}
            x={x}
            y={y}
            horizontalColumn={horizontalColumn}
            verticalColumn={verticalColumn}
            horizontalColumn2={horizontalColumn2}
            verticalColumn2={verticalColumn2}
            colorKeyColumn={keyColumn}
            color={color}
            pointSize={pointSize} 
            chartRequest={chartRequest}
            dashboardFilter={dashboardFilter}
            onDrillDown={onDrillDown} />
        }

        <InitialMessage data={data} x={xRule.middle("content")} y={yRule.middle("content")} loading={loading} />

        <g opacity={dashboardFilter ? .5 : undefined}>
          <XAxis xRule={xRule} yRule={yRule} />
          <YAxis xRule={xRule} yRule={yRule} />
        </g>
      </svg>
      {parameters["DrawingMode"] != "Svg" &&
        <CanvasScatterplot
          color={color}
          colorKeyColumn={keyColumn}
          horizontalColumn={horizontalColumn}
          verticalColumn={verticalColumn}
          horizontalColumn2={horizontalColumn2}
          verticalColumn2={verticalColumn2}
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

function SvgScatterplot({ data, keyColumns, xRule, yRule, initialLoad, y, x,
  horizontalColumn, verticalColumn, horizontalColumn2, verticalColumn2,
  colorKeyColumn, color, onDrillDown, pointSize, dashboardFilter, chartRequest }: {
    data: ChartTable,
    keyColumns: ChartColumn<any>[],
    xRule: Rule<"content">,
    yRule: Rule<"content">,
    initialLoad: boolean,
    x: d3.ScaleContinuousNumeric<number, number, never>,
    y: d3.ScaleContinuousNumeric<number, number, never>,
    horizontalColumn: ChartColumn<number>,
    horizontalColumn2?: ChartColumn<number>,
    verticalColumn: ChartColumn<number>,
    verticalColumn2?: ChartColumn<number>,
    colorKeyColumn: ChartColumn<unknown>,
    color: (val: ChartRow) => string | undefined,
    pointSize: number,
    dashboardFilter?: DashboardFilter,
    chartRequest: ChartRequestModel,
    onDrillDown: (row: ChartRow, e: MouseEvent | React.MouseEvent<any, MouseEvent>) => void
  }): React.JSX.Element {

  var detector = ChartClient.getActiveDetector(dashboardFilter, chartRequest);

  if (horizontalColumn2 == null && verticalColumn2 == null)
    return (<>{
      data.rows.map(r => {
        const active = detector?.(r);

        return (
          <g key={keyColumns.map(c => c.getValueKey(r)).join("/")} className="shape-serie sf-transition hover-group"
            opacity={active == false ? .5 : undefined}
            transform={translate(xRule.start('content'), yRule.end('content')) + (initialLoad ? scale(1, 0) : scale(1, 1))}>
            <circle className="shape sf-transition hover-target"
              cx={x(horizontalColumn.getValue(r))!}
              cy={-y(verticalColumn.getValue(r))!}
              stroke={active == true ? "var(--bs-body-color)" : colorKeyColumn.getValueColor(r) ?? color(r)}
              strokeWidth={active == true ? 3 : undefined}
              fill={colorKeyColumn.getValueColor(r) ?? color(r)}
              shapeRendering="initial"
              r={pointSize}
              onClick={e => onDrillDown(r, e)}
              role="button"
              tabIndex={0}
              cursor="pointer"
              onKeyDown={e => {
                if (e.key === "Enter" || e.key === " ") {
                  e.preventDefault();
                  (onclick as any)?.(e);
                }
              }}>
              <title>
                {colorKeyColumn.getValueNiceName(r) +
                  ("\n" + horizontalColumn.title + ": " + horizontalColumn.getValueNiceName(r)) +
                  ("\n" + verticalColumn.title + ": " + verticalColumn.getValueNiceName(r))}
              </title>
            </circle>
          </g>);

      })
    }</>);
  else {
    return (<>
      {data.rows.map(r => <g key={keyColumns.map(c => c.getValueKey(r)).join("/")} className="shape-serie sf-transition"
        transform={translate(xRule.start('content'), yRule.end('content')) + (initialLoad ? scale(1, 0) : scale(1, 1))}>
        <line className="shape sf-transition"
          x1={x(horizontalColumn.getValue(r))}
          y1={-y(verticalColumn.getValue(r))}
          x2={x((horizontalColumn2 ?? horizontalColumn).getValue(r))}
          y2={-y((verticalColumn2 ?? verticalColumn).getValue(r))}
          stroke={colorKeyColumn.getValueColor(r) ?? color(r)}
          strokeWidth={pointSize}
          fill={colorKeyColumn.getValueColor(r) ?? color(r)}
          shapeRendering="initial"
          onClick={e => onDrillDown(r, e)} role="button"
          tabIndex={0}
          cursor="pointer"
          onKeyDown={e => {
            if (e.key === "Enter" || e.key === " ") {
              e.preventDefault();
              (onclick as any)?.(e);
            }
          }} />
        <circle className="shape sf-transition"
          cx={x(horizontalColumn.getValue(r))}
          cy={-y(verticalColumn.getValue(r))}
          stroke={colorKeyColumn.getValueColor(r) ?? color(r)}
          fill={colorKeyColumn.getValueColor(r) ?? color(r)}
          shapeRendering="initial"
          r={pointSize}
          onClick={e => onDrillDown(r, e)}
          role="button"
          tabIndex={0}
          cursor="pointer"
          onKeyDown={e => {
            if (e.key === "Enter" || e.key === " ") {
              e.preventDefault();
              (onclick as any)?.(e);
            }
          }}/>
        <circle className="shape sf-transition"
          cx={x((horizontalColumn2 ?? horizontalColumn).getValue(r))}
          cy={-y((verticalColumn2 ?? verticalColumn).getValue(r))}
          stroke={colorKeyColumn.getValueColor(r) ?? color(r)}
          fill={colorKeyColumn.getValueColor(r) ?? color(r)}
          shapeRendering="initial"
          r={pointSize}
          onClick={e => onDrillDown(r, e)}
          role="button"
          tabIndex={0}
          cursor="pointer"
          onKeyDown={e => {
            if (e.key === "Enter" || e.key === " ") {
              e.preventDefault();
              (onclick as any)?.(e);
            }
          }} />
        <title>
          {colorKeyColumn.getValueNiceName(r) +
            ("\n" + horizontalColumn.title + ": " + horizontalColumn.getValueNiceName(r)) +
            (horizontalColumn2 ? ("\n" + horizontalColumn2.title + ": " + horizontalColumn2.getValueNiceName(r)) : "") +
            ("\n" + verticalColumn.title + ": " + verticalColumn.getValueNiceName(r)) +
            (verticalColumn2 ? ("\n" + verticalColumn2.title + ": " + verticalColumn2.getValueNiceName(r)) : "")
          }
        </title>

      </g>)}
    </>);
  }
}

function CanvasScatterplot(p: {
  xRule: Rule<"content">,
  yRule: Rule<"content">,
  colorKeyColumn: ChartColumn<unknown>,
  horizontalColumn: ChartColumn<number>,
  horizontalColumn2?: ChartColumn<number>,
  verticalColumn: ChartColumn<number>,
  verticalColumn2?: ChartColumn<number>,
  pointSize: number,
  data: ChartTable,
  onDrillDown: (r: ChartRow, e: MouseEvent) => void,
  color: (val: ChartRow) => string | undefined,
  x: d3.ScaleContinuousNumeric<number, number>,
  y: d3.ScaleContinuousNumeric<number, number>,
}) {

  var cRef = React.useRef<HTMLCanvasElement>(null);
  var vcRef = React.useRef<HTMLCanvasElement>(null);
  var colorDataRef = React.useRef<{ [key: string]: ChartRow }>({});

  React.useEffect(() => {

    var { xRule, yRule, horizontalColumn, verticalColumn, horizontalColumn2, verticalColumn2, colorKeyColumn, data, pointSize, onDrillDown, color, x, y } = p;
    var w = xRule.size('content');
    var h = yRule.size('content');
    var c = cRef.current!;
    var vc = vcRef.current!;

    const ctx = c.getContext("2d")!;
    const vctx = vc.getContext("2d")!;
    var colorToData: { [key: string]: ChartRow } = colorDataRef.current;
    ctx.clearRect(0, 0, w, h);
    vctx.clearRect(0, 0, w, h);
    data.rows.forEach((r, i) => {

      var c = colorKeyColumn.getValueColor(r) ?? color(r);

      ctx.fillStyle = c ?? "var(--bs-body-color)";
      ctx.strokeStyle = c ?? "var(--bs-body-color)";
      var vColor = getVirtualColor(i);
      vctx.fillStyle = vColor;
      vctx.strokeStyle = vColor;
      colorToData[vColor] = r;

      if (horizontalColumn2 == null && verticalColumn2 == null) {

        var xVal = x(horizontalColumn.getValue(r))!;
        var yVal = h - y(verticalColumn.getValue(r))!;

        ctx.beginPath();
        ctx.arc(xVal, yVal, pointSize, 0, 2 * Math.PI);
        ctx.fill();
        ctx.stroke();

        vctx.beginPath();
        vctx.arc(xVal, yVal, pointSize, 0, 2 * Math.PI);
        vctx.fill();
        vctx.stroke();
      } else {

        var xVal = x(horizontalColumn.getValue(r))!;
        var xVal2 = x((horizontalColumn2 ?? horizontalColumn).getValue(r))!;
        var yVal = h - y(verticalColumn.getValue(r))!;
        var yVal2 = h - y((verticalColumn2 ?? verticalColumn).getValue(r))!;

        ctx.lineWidth = pointSize;
        ctx.beginPath();
        ctx.moveTo(xVal, yVal);
        ctx.lineTo(xVal2, yVal2)
        ctx.stroke();

        vctx.lineWidth = pointSize;
        vctx.beginPath();
        vctx.moveTo(xVal, yVal);
        vctx.lineTo(xVal2, yVal2)
        vctx.stroke();
      }
    });

    function getVirtualColor(index: number): string {
      return d3.rgb(
        Math.floor(index / 256 / 256) % 256,
        Math.floor(index / 256) % 256,
        index % 256)
        .toString();
    }

    c.addEventListener('mousemove', function (e) {
      const imageData = vctx.getImageData(e.offsetX, e.offsetY, 1, 1);
      const color = d3.rgb.apply(null, imageData.data).toString();
      const r = colorToData[color];
      if (r) {
        c.style.cursor = "pointer";
        c.setAttribute("title", colorKeyColumn.getValueNiceName(r) +
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
        onDrillDown(p, e);
      }
    });
  });


  var { xRule, yRule } = p;

  var w = xRule.size('content');
  var h = yRule.size('content');
  return (
    <>
      <canvas ref={vcRef} width={w} height={h} style={{ position: "absolute", left: xRule.start('content') + 'px', top: yRule.start('content') + 'px', opacity: 0 }} />
      <canvas ref={cRef} width={w} height={h} style={{ position: "absolute", left: xRule.start('content') + 'px', top: yRule.start('content') + 'px' }} />
    </>
  );
}

