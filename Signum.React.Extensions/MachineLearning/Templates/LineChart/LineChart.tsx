import * as React from 'react';
import * as d3 from "d3";


interface Point {
    x: number;
    y: number;
}

interface LineChartSerie {
    color: string; 
    name: string;
    values: Point[] 
}

interface LineChartProps {
    series: LineChartSerie[];
    width: number;
    height: number;
}

export default class LineChart extends React.Component<LineChartProps> {
    render() {
        const { width, height, series } = this.props;

        var allValues = series.flatMap(s => s.values);

        var scaleX = d3.scaleLinear()
            .domain([allValues.min(a => a.x), allValues.max(a => a.x)])
            .range([0, width]);


        var scaleY = d3.scaleLinear()
            .domain([allValues.min(a => a.y), allValues.max(a => a.y)])
            .range([0, height]);

        var line = d3.line<Point>()
            .curve(d3.curveCardinal)
            .x(p => scaleX(p.x))
            .y(p => scaleY(p.y));

        return (
            <div>
                <svg width={width} height={height}>
                    <g transform="translate(2,2)">
                        {series.map((s, i) => <path className="line" d={line(s.values) || undefined} stroke={s.color} strokeWidth={"1.5px"} />)}
                    </g>
                </svg>
            </div>
        );
    }
}


