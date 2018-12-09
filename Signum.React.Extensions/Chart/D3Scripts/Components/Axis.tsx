import * as React from 'react'
import * as d3 from 'd3'
import * as ChartUtils from './ChartUtils';
import { translate } from './ChartUtils';
import TextEllipsis from './TextEllipsis';
import { Rule } from './Rule';

export function XAxis({ xRule, yRule }: { xRule: Rule, yRule: Rule }) {
  return (
    <g className="x-axis" transform={translate(xRule.start('content'), yRule.end('content'))}>
      <line className="x-axis" x2={xRule.size('content')} stroke="Black" />
    </g>
  );
}

export function YAxis({ xRule, yRule }: { xRule: Rule, yRule: Rule }) {
  return (
    <g className="y-axis" transform={translate(xRule.start('content'), yRule.start('content'))}>
      <line className="y-axis" y2={yRule.size('content')} stroke="Black" />
    </g>
  );
}
