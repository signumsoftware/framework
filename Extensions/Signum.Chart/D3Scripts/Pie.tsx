import * as React from 'react'
import * as d3 from 'd3'
import { ChartClient, ChartScriptProps, ChartRow, ChartTable, ChartColumn } from '../ChartClient';
import * as ChartUtils from './Components/ChartUtils';
import { translate, scale, rotate, skewX, skewY, matrix, scaleFor } from './Components/ChartUtils';
import InitialMessage from './Components/InitialMessage';
import { TextRectangle } from './StackedLines';
import TextEllipsis from './Components/TextEllipsis';
import { getQueryNiceName, symbolNiceName, toNumberFormat } from '@framework/Reflection';
import { Color } from '../../../Signum/React/Basics/Color';
import { ChartMessage, D3ChartScript } from '../Signum.Chart';

export default function renderPie({ data, width, height, parameters, loading, onDrillDown, initialLoad, memo, chartRequest, dashboardFilter }: ChartScriptProps): React.ReactElement<any> {

  if (data == null || data.rows.length == 0)
    return (
      <svg direction="ltr" width={width} height={height}>
        <InitialMessage data={data} x={width / 2} y={height / 2} loading={loading} />
      </svg>
    );

  var keyColumn = data.columns.c0!;
  var valueColumn = data.columns.c1! as ChartColumn<number>;

  var pInnerRadius = parameters.InnerRadious || "0";
  var pSort = parameters.Sort;
  var pValue= parameters.Value;
  var pPercent = parameters.Percent;
  var pTotal = parameters.Total;
  var dataTotal = data.rows.sum(r => valueColumn.getValue(r));

  var size = d3.scaleLinear()
    .domain([0, d3.max(data.rows, r => valueColumn.getValue(r))!])
    .range([0, 1]);
  var outerRadious = d3.min([width / 2, height])! / 3;
  var rInner = outerRadious * parseFloat(pInnerRadius);
  var color = ChartUtils.colorCategory(parameters, data.rows.map(r => keyColumn.getValueKey(r)), memo);

  var pie = d3.pie<ChartRow>()
    .sort(pSort == "Ascending" ? ((a, b) => d3.descending(size(valueColumn.getValue(a)), size(valueColumn.getValue(b)))) :
      pSort == "Descending" ? ((a, b) => d3.ascending(size(valueColumn.getValue(a)), size(valueColumn.getValue(b)))) :
        (a, b) => 0)
    .value(r => size(valueColumn.getValue(r)));

  var arc = d3.arc<d3.PieArcDatum<ChartRow>>()
    .outerRadius(outerRadious)
    .innerRadius(rInner);

  var legendRadius = 1.1;

  var detector = ChartClient.getActiveDetector(dashboardFilter, chartRequest);

  var orderedPie = pie(data.rows).orderBy(s => keyColumn.getValueKey(s.data));
  var numFormat = toNumberFormat('0.#K');
  return (
    <svg direction="ltr" width={width} height={height} role="img">
      <title id="pieChartTitle">{ChartMessage._0Of1_2.niceToString(symbolNiceName(D3ChartScript.Pie), getQueryNiceName(chartRequest.queryKey), [valueColumn.title, keyColumn.title].join(", "))}</title>
      <g className="shape" transform={translate(width / 2, height / 2)}>
        {orderedPie.map(slice => {
          var active = detector?.(slice.data);
          var m = (slice.endAngle + slice.startAngle) / 2;
          var cuadr = Math.floor(12 * m / (2 * Math.PI));
          var active = detector?.(slice.data);

          var isRight = m < Math.PI;
          var percentText = Number(valueColumn.getValue(slice.data) / dataTotal).toLocaleString(undefined, { style: 'percent', minimumFractionDigits: 1 });
          var valueText = numFormat.format(valueColumn.getValue(slice.data));
          var textAnchor = isRight ? 'start' : 'end';
          var sliceColor = keyColumn.getValueColor(slice.data) ?? color(keyColumn.getValueKey(slice.data));
          var textColor = Color.parse(sliceColor).opositePole().toString();
          var arcHeight = Math.abs(Math.sin(slice.endAngle) * outerRadious * legendRadius - Math.sin(slice.startAngle) * outerRadious * legendRadius);
          return (
            <g key={slice.index} className="slice hover-group">
              <title>{`${keyColumn.getValueNiceName(slice.data)}: ${valueText}`}</title>
              <path className="shape sf-transition hover-target" d={arc(slice)!}
                opacity={active == false ? .5 : undefined}
                stroke={active == true ? "var(--bs-body-color)" : undefined}
                strokeWidth={active == true ? 3 : undefined}
                transform={initialLoad ? scale(0, 0) : scale(1, 1)}
                fill={sliceColor}
                shapeRendering="initial"
                role="button"
                tabIndex={0}
                cursor="pointer"
                onKeyDown={(e: React.KeyboardEvent<SVGRectElement>) => {
                  if (e.key === "Enter" || e.key === " ") {
                    e.preventDefault();
                    (onclick as any)?.(e);
                  }
                }}
                onClick={e => onDrillDown(slice.data, e)}>
              </path>
              <SliceText value={pValue == 'OnArc' ? valueText : undefined} percent={pPercent == 'OnArc' ? percentText : undefined} slice={slice} innerRadius={rInner} outerRadius={outerRadious} color={textColor} />
              <g key={slice.index} className="color-legend">
                {arcHeight > 20 && <TextValueRectangle className="color-legend sf-chart-strong sf-transition"
                  rectangleAtts={{ fill: "transparent" }}
                  rectMaxWidth={(width / 2) - Math.abs(Math.sin(m) * outerRadious * legendRadius)}
                  isRight={isRight}
                  transform={translate(
                    Math.sin(m) * outerRadious * legendRadius,
                    -Math.cos(m) * outerRadious * legendRadius)}
                  opacity={active == false ? .5 : undefined}
                  textAnchor={textAnchor}
                  dominantBaseline="central"
                  fontWeight={active == true ? "bold" : undefined}
                  fill={keyColumn.getValueColor(slice.data) ?? color(keyColumn.getValueKey(slice.data))}
                  value={((slice.endAngle - slice.startAngle) < (Math.PI / 64)) ? '' : concatValuePercent(pValue == 'OnLabel' ? valueText : undefined, pPercent == "OnLabel" ? percentText : undefined)}
                  onClick={e => onDrillDown(slice.data, e)}>
                  {((slice.endAngle - slice.startAngle) < (Math.PI / 64)) ? '' : keyColumn.getValueNiceName(slice.data)}
                </TextValueRectangle>}
              </g>
            </g>
          );
        })}
        {pTotal == 'Yes' && <TextRectangle className="color-legend sf-chart-strong sf-transition"
          rectangleAtts={{ fill: "transparent" }}
          textAnchor={"middle"}
          dominantBaseline="central"
          fontWeight={"bold"}
          fill={'navy'}>
          {numFormat.format(dataTotal)}
        </TextRectangle>}
      </g>
      <InitialMessage data={data} x={width / 2} y={height / 2} loading={loading} />
    </svg>
  );
}

export interface TextValueRectangleProps extends React.SVGProps<SVGTextElement> {
  rectangleAtts?: React.SVGProps<SVGRectElement>;
  rectMaxWidth: number;
  isRight: boolean;
  value?: string;
}

export function TextValueRectangle({ rectangleAtts, children, value, rectMaxWidth, isRight, onClick, ...atts }: TextValueRectangleProps): React.JSX.Element {

  const txt = React.useRef<SVGTextElement>(null);
  const val = React.useRef<SVGTextElement>(null);
  const rect = React.useRef<SVGRectElement>(null);
  const [valWidth, setValWidth] = React.useState<number | null>(null);
  const [txtWidth, setTxtWidth] = React.useState<number | null>(null);

  React.useEffect(() => {
    if (txt.current && val.current) {
      let valElem = val.current!;
      valElem.textContent = value ?? '';
      let valueLength = valElem.getComputedTextLength();

      var width = rectMaxWidth - valueLength;

      let txtElem = txt.current!;
      txtElem.textContent = getString(children);
      let textLength = txtElem.getComputedTextLength();
      let text = txtElem.textContent!;
      while (textLength > width && text.length > 0) {
        text = text.slice(0, -1);
        while (text[text.length - 1] == ' ' && text.length > 0)
          text = text.slice(0, -1);
        txtElem.textContent = text + "…";
        textLength = txtElem.getComputedTextLength();
      }
      setValWidth(valueLength);
      setTxtWidth(textLength);
    }

    if (rect.current) {

      var tbox = txt.current!.getBoundingClientRect();
      var vbox = txt.current!.getBoundingClientRect();
      let w = tbox.width + vbox.width + 4;
      let h = tbox.height;
      rect.current.setAttribute("width", w + "px");
      rect.current.setAttribute("x", -(w) / 2 + "px");
      rect.current.setAttribute("height", h + "px");
      rect.current.setAttribute("y", -(h / 2) - 2 + "px");
    }


  }, [getString(children), value, rectMaxWidth]);

  const interactive = typeof onClick === "function";
  const accessibilityPropsOnClick = interactive
    ? {
      role: "button",
      tabIndex: 0,
      cursor: "pointer",
      onKeyDown: (e: React.KeyboardEvent<SVGTextElement>) => {
        if (e.key === "Enter" || e.key === " ") {
          e.preventDefault();
          (onClick as any)?.(e);
        }
      },
    }
    : {};

  return (
    <g transform={atts.transform} {...accessibilityPropsOnClick}>
      <rect ref={rect} {...rectangleAtts} height={20} transform={translate(0, 0)} />
      <text ref={txt} {...atts} transform={translate(!isRight && valWidth ? -valWidth : 0, 0)} >
        {children ?? ""}
      </text>
      <text ref={val} {...atts} transform={translate(isRight && txtWidth ? txtWidth : 0, 0)} >
        {value ?? ""}
      </text>
    </g>
  );
}

function getString(children: React.ReactNode) {
  return React.Children.toArray(children)[0] as string;
}

function concatValuePercent(value?: string, percent?: string) {
  if (value && percent)
    return `, ${value}, ${percent}`;
  if(value)
    return `, ${value}`;
  if (percent)
    return `, ${percent}`;
  return '';
}

function pointIsInArc(x: number, y: number, d3Arc: d3.PieArcDatum<ChartRow>, innerRadius: number, outerRadius: number) {
  // Center of the arc is assumed to be 0,0
  // (pt.x, pt.y) are assumed to be relative to the center
  var r1 = innerRadius, // Note: Using the innerRadius
    r2 = outerRadius,
    theta1 = d3Arc.startAngle,  theta2 = d3Arc.endAngle;

  var dist = x * x + y * y,
    angle = Math.atan2(x, -y); // Note: different coordinate system.

  angle = (angle < 0) ? (angle + Math.PI * 2) : angle;

  return (r1 * r1 <= dist) && (dist <= r2 * r2) &&
    (theta1 <= angle) && (angle <= theta2);
}


function SliceText(p: { value?: string, percent?: string, slice: d3.PieArcDatum<ChartRow>, innerRadius: number, outerRadius: number, color: string }) {
  const txt = React.useRef<SVGTextElement>(null);
  const [visible, setVisible] = React.useState(false);

  var centroid = d3.arc<d3.PieArcDatum<ChartRow>>()
    .outerRadius(p.outerRadius)
    .innerRadius(p.innerRadius)
    .centroid(p.slice);

  React.useEffect(() => {
    if (txt.current) {

      const bb = txt.current!.getBBox();
      const center = centroid;

      const topLeft = {
        x: center[0] + bb.x,
        y: center[1] + bb.y,
      };
      const topRight = {
        x: topLeft.x + bb.width,
        y: topLeft.y,
      };
      const bottomLeft = {
        x: topLeft.x,
        y: topLeft.y + bb.height,
      };
      const bottomRight = {
        x: topLeft.x + bb.width,
        y: topLeft.y + bb.height,
      };

      const visible =
        pointIsInArc(topLeft.x, topLeft.y, p.slice, p.innerRadius, p.outerRadius) &&
        pointIsInArc(topRight.x, topRight.y, p.slice, p.innerRadius, p.outerRadius) &&
        pointIsInArc(bottomLeft.x, bottomLeft.y, p.slice, p.innerRadius, p.outerRadius) &&
        pointIsInArc(bottomRight.x, bottomRight.y, p.slice, p.innerRadius, p.outerRadius);
      setVisible(visible);
    }
  }, [txt.current]);

  return (
    <g transform={`translate(${centroid[0]}, ${centroid[1]})`}>
      <text ref={txt} dy=".35em" textAnchor="middle" style={{ display: visible ? 'inherit' : 'none' }} fill={p.color}>
        {p.percent && <tspan x="0" dy="1.2em">{p.percent}</tspan>}
        {p.value && <tspan x="0" dy="1.2em">{p.value}</tspan>}
      </text>
    </g>
  );
};

