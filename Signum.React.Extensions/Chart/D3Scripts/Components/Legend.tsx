import * as React from 'react'
import * as d3 from 'd3'
import { translate } from './ChartUtils';
import TextEllipsis from './TextEllipsis';
import { Rule } from './Rule';
import { PivotTable } from './PivotTable';

interface LegendProps {
  pivot: PivotTable;
  xRule: Rule;
  yRule: Rule;
  color: d3.ScaleOrdinal<string, string>;
}

export default class Legend extends React.Component<LegendProps> {
  render() {
    const { pivot, xRule, yRule, color } = this.props;

    var legendScale = d3.scaleBand()
      .domain(pivot.columns.map((s, i) => i.toString()))
      .range([0, xRule.size('content')]);


    if (legendScale.bandwidth() <= 50)
      return null;

    var legendMargin = yRule.size('legend') + 4;

    return (
      <g>
        <g className="color-legend" transform={translate(xRule.start('content'), yRule.start('legend'))}>
          {pivot.columns.map((s, i) => <rect key={s.key} className="color-rect" transform={translate(legendScale(i.toString())!, 0)}
            width={yRule.size('legend')}
            height={yRule.size('legend')}
            fill={s.color || color(s.key)} />)}
        </g>

        <g className="color-legend" transform={translate(xRule.start('content') + legendMargin, yRule.middle('legend') + 1)}>
          {pivot.columns.map((s, i) => <TextEllipsis key={s.key} transform={translate(legendScale(i.toString())!, 0)}
            maxWidth={legendScale.bandwidth() - legendMargin} className="color-text"
            dominantBaseline="middle">
            {s.niceName!}
          </TextEllipsis>)}
        </g>
      </g>
    );
  }
}
