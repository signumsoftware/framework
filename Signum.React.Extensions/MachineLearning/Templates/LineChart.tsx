import * as React from 'react';
import * as d3 from "d3";

export interface Point {
  x: number;
  y: number;
}

export interface LineChartSerie {
  color: string;
  name: string;
  title?: string;
  values: Point[];
  minValue?: number;
  maxValue?: number;
  strokeWidth?: string;

}

interface LineChartProps {
  series: LineChartSerie[];
  width?: number;
  height: number;
}

export default class LineChart extends React.Component<LineChartProps, { width?: number; logMode: boolean; }> {
  constructor(p: LineChartProps) {
    super(p);
    this.state = { width: undefined, logMode: false };
  }

  divRef?: HTMLDivElement | null;
  handleSetRef = (d: HTMLDivElement | null) => {
    if (this.divRef == null && d != null && this.props.width == null) {
      this.divRef = d;
      setTimeout(() => {
        this.setState({ width: d.getBoundingClientRect().width })
      }, 100);

    }
  }

  render() {
    let width = this.props.width;

    if (width == null)
      width = this.state.width;

    return (
      <div ref={d => this.handleSetRef(d)} style={{ width: this.props.width == null ? "100%" : this.props.width }} onDoubleClick={() => this.setState({ logMode: !this.state.logMode })}>
        {width != null && this.renderSvg(width)}
      </div>
    );
  }

  renderSvg(width: number) {

    let { height, series } = this.props;

    var allValues = series.flatMap(s => s.values);

    var scaleX = d3.scaleLinear()
      .domain([allValues.min(a => a.x)!, allValues.max(a => a.x)!])
      .range([2, width - 4]);

    return (
      <svg width={width} height={height}>
        {series.map((s, i) => this.renderSerie(scaleX, height, s, i))}
        <line x1={0} x2={width} y1={height - 20} y2={height - 20} stroke="black" strokeWidth={1} />
        {series.map((s, i) => (
          <g key={i}>
            {s.title && <title>{s.title}</title>}
            <text x={(i / series.length) * width} y={height - 4} style={{ fill: s.color }}>{s.name}</text>
          </g>)
        )}
      </svg>
    );
  }

  renderSerie(scaleX: d3.ScaleLinear<number, number>, height: number, s: LineChartSerie, index: number) {

    var minValue: number = s.minValue != null ? s.minValue : s.values.min(a => a.y)!;
    var maxValue: number = s.maxValue != null ? s.maxValue : s.values.max(a => a.y)!;

    var scaleY = this.state.logMode ?
      d3.scaleLog().domain([Math.max(0.0001, minValue), maxValue]).range([height - 20, 2]) :
      d3.scaleLinear().clamp(true).domain([minValue, maxValue]).range([height - 20, 2]);

    var line = d3.line<Point>()
      .curve(d3.curveLinear)
      .x(p => scaleX(p.x))
      .y(p => scaleY(p.y));

    return (
      <g key={index}>
        <path className="line" fill="none" d={line(s.values) || undefined} style={{
          stroke: s.color, strokeWidth: s.strokeWidth
        }}>
        </path>
        <title>{`${s.name} (${s.title || " - "})`}</title>
      </g>
    );
  }
}


