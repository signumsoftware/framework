import * as React from 'react'
import * as d3 from 'd3'
import {  ChartTable } from '../../ChartClient';
import { Rule } from './Rule';
import { JavascriptMessage, SearchMessage } from '@framework/Signum.Entities';
import { SearchControl } from '@framework/Search';
import { useInterval } from '@framework/Hooks';

interface InitialMessageProps {
  x?: number;
  y?: number;
  loading: boolean;
  data?: ChartTable;
}

export default function InitialMessage(p: InitialMessageProps) {

  var dots = useInterval(p.loading ? 1000 : null, 0, d => (d + 1) % 4);

  if (p.loading)
    return (
      <text x={p.x} y={p.y} className="sf-initial-message loading">
        {JavascriptMessage.loading.niceToString() + ".".repeat(dots) + " ".repeat(3 - dots)}
      </text >
    );

  if (p.data == null)
    return (
      <text x={p.x} y={p.y} className="sf-initial-message search">
        {JavascriptMessage.searchForResults.niceToString()}
      </text >
    );

  if (p.data.rows.length == 0)
    return (
      <text x={p.x} y={p.y} className="sf-initial-message no-results">
        {SearchMessage.NoResultsFound.niceToString()}
      </text >
    );

  return null;
}
