import * as React from 'react'
import * as d3 from 'd3'
import {  ChartTable } from '../../ChartClient';
import { Rule } from './Rule';
import { JavascriptMessage, SearchMessage } from '@framework/Signum.Entities';
import { SearchControl } from '@framework/Search';


export function useInterval<T>(interval: number, initialState: T, newState: (oldState: T) => T) {
  const [val, setVal] = React.useState(initialState);

  React.useEffect(() => {
    var insideVal = val;
    if (interval) {
      var handler = setInterval(() => {
        setVal(insideVal = newState(insideVal));
      }, interval);
      return () => clearInterval(handler);
    }
  }, [interval]);

  return val;
}

interface InitialMessageProps {
  x?: number;
  y?: number;
  loading: boolean;
  data?: ChartTable;
}

export default function InitialMessage(p: InitialMessageProps) {

  var dots = useInterval(p.loading ? 1000 : 0, 0, d => (d + 1) % 4);

  if (p.loading)
    return (
      <text x={p.x} y={p.y} style={{ fontSize: "22px", textAnchor: "middle" }} fill="#aaa">
        {JavascriptMessage.loading.niceToString() + ".".repeat(dots) + " ".repeat(3 - dots)}
      </text >
    );

  if (p.data == null)
    return (
      <text x={p.x} y={p.y} style={{ fontSize: "22px", textAnchor: "middle" }} fill="#ddd">
        {JavascriptMessage.searchForResults.niceToString()}
      </text >
    );

  if (p.data.rows.length == 0)
    return (
      <text x={p.x} y={p.y} style={{ fontSize: "22px", textAnchor: "middle" }} fill="#ffb5b5">
        {SearchMessage.NoResultsFound.niceToString()}
      </text >
    );

  return null;
}
