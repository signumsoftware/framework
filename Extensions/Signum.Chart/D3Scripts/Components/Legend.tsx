import * as React from 'react'
import * as d3 from 'd3'
import { translate } from './ChartUtils';
import TextEllipsis from './TextEllipsis';
import { Rule } from './Rule';
import { PivotColumn, PivotTable } from './PivotTable';

interface LegendProps {
  pivot: PivotTable;
  xRule: Rule<"content">;
  yRule: Rule<"legend">;
  color: d3.ScaleOrdinal<string, string>;
  isActive?: (pc: PivotColumn) => boolean;
  onDrillDown?: (pc: PivotColumn, e: React.MouseEvent<any> | MouseEvent) => void;
}

export default function Legend(p: LegendProps): React.JSX.Element | null {

  const { pivot, xRule, yRule, color } = p;

  var legendScale = d3.scaleBand()
    .domain(pivot.columns.map((s, i) => i.toString()))
    .range([0, xRule.size('content')]);

  if (legendScale.bandwidth() <= 50)
    return null;

  var legendMargin = yRule.size('legend') + 4;

  var textWidth = legendScale.bandwidth() - legendMargin;
  return (
    <g>
      <g className="color-legend" transform={translate(xRule.start('content'), yRule.start('legend'))}>
        {pivot.columns.map((s, i) =>
          <g key={s.key} style={p.onDrillDown && { cursor: "pointer" }} onClick={e => p.onDrillDown?.(s, e)} opacity={p.isActive && p.isActive(s) == false ? .5 : undefined}>
            <rect className="color-rect" transform={translate(legendScale(i.toString())!, 0)}
              width={yRule.size('legend')}
              height={yRule.size('legend')}
              stroke={p.isActive && p.isActive(s) == true ? "var(--bs-body-color)" : undefined}
              strokeWidth={p.isActive && p.isActive(s) == true ? 3 : undefined}
              fill={s.color ?? color(s.key)} />
            {(textWidth > 30) && <TextEllipsis transform={translate(legendScale(i.toString())! + legendMargin, yRule.size('legend') / 2 + 1)}
              maxWidth={textWidth} className="color-text"
              dominantBaseline="middle">
              {s.niceName!}
            </TextEllipsis>}
            <title>
              {s.niceName}
            </title>
          </g>
        )}
      </g>
    </g>
  );
}
