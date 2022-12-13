import * as React from 'react'
import { NumericTextBox, ValueLine, isNumber } from '@framework/Lines/ValueLine';
import { useForceUpdate } from '@framework/Hooks'
import { toNumberFormat } from '@framework/Reflection';
import { Change, diffLines, diffWords } from 'diff';


export interface LineDiff {
  lineChange: Change;
  lineDetail: Array<Change>;
}

export function DiffDocument(p: { first: string, second: string }) {
  
  const [margin, setMargin] = React.useState<number | null>(4);

  return (
    <div>
      <div>
        <label>
          <input type="checkbox" className="form-check-input" checked={margin != null} onChange={() => setMargin(margin == null ? DiffDocument.defaultMarginLines : null)} />
          <span className="mx-2">Show only</span><NumericTextBox format={toNumberFormat("0")} value={margin == null ? 4 : margin} onChange={num => setMargin(num == null ? 0 : Math.max(num, 0))}
            validateKey={isNumber} /> lines arround each change</label>
      </div>
      <div>
        <DiffDocumentSimple first={p.first} second={p.second} margin={margin} />
      </div>
    </div>
  );
}

DiffDocument.defaultMarginLines = 4 as (number | null);

export function DiffDocumentSimple(p: { first: string, second: string, margin?: number | null }) {

  const linesDiff = React.useMemo<Array<LineDiff>>(() => {

    var diffs = diffLines(p.first, p.second);
    var linesD: Array<LineDiff> = [];

    for (var i = 0; i < diffs.length; i++) {
      var change = diffs[i];
      if (change.count != null && change.count > 1) {
        change.value.split("\r\n").notNull().map(v => linesD.push({ lineChange: { value: v + "\r\n", count: 1 }, lineDetail: [] }));
      }
      else {
        if (change.removed) {
          if (i + 1 < diffs.length && diffs[i + 1].added) {
            var nextChange = diffs[i + 1];
            var wordDiffs = diffWords(change.value, nextChange.value);
            linesD.push({ lineChange: change, lineDetail: wordDiffs });
            linesD.push({ lineChange: nextChange, lineDetail: wordDiffs });
            i++;
            continue;
          }
          linesD.push({ lineChange: change, lineDetail: [] });
        }
      }
    }

    return [...linesD]
  }, [p.first, p.second]);


  var indices = p.margin == null ? Array.range(0, linesDiff.length) :
    expandNumbers(linesDiff.map((a, i) => a.lineChange.added || a.lineChange.removed ? i : null).filter(n => n != null) as number[], linesDiff.length, p.margin);

  return <pre className="m-0">{indices.map((ix, i) => {
    if (typeof ix == "object")
      return [<span key={i} style={{ backgroundColor: "#DDD" }}><span> ----- {ix.numLines} Lines Removed ----- </span><br /></span>];

    var line = linesDiff[ix];

    var color = line.lineChange.added ? "#CEF3CE" : line.lineChange.removed ? "#FFD1D1" : undefined;

    if (line.lineDetail.length > 0) {
      if (line.lineChange.removed)
        return (<span key={i} style={{ backgroundColor: color }}>
          {line.lineDetail.filter(c => !c.added).map((c, j) => {
            var changeColor = c.added ? "#72F272" : c.removed ? "#FF8B8B" : undefined;
            return <span key={j} style={{ backgroundColor: changeColor }}>{c.value}</span>;
          })}
        </span>);
      if (line.lineChange.added)
        return (<span key={i} style={{ backgroundColor: color }}>
          {line.lineDetail.filter(c => !c.removed).map((c, j) => {
            var changeColor = c.added ? "#72F272" : c.removed ? "#FF8B8B" : undefined;
            return <span key={j} style={{ backgroundColor: changeColor }}>{c.value}</span>;
          })}
        </span>);
    }
    else
      return <span key={i} style={{ backgroundColor: color }}>{line.lineChange.value}</span>
  })}</pre >;
}

interface LinesRemoved {
  numLines: number;
}

export function expandNumbers(changes: number[], max: number, margin: number): (number | LinesRemoved)[] {

  if (changes.length == 0)
    return [];

  const result: (number | LinesRemoved)[] = [];
  var lastChange = changes[0];

  var prev0 = lastChange - margin;

  function pushRange(from: number, to: number) {
    for (var j = from; j <= to; j++) {
      result.push(j);
    }
  }

  if (prev0 <= 0) {
    pushRange(0, lastChange);
  } else {
    result.push({ numLines: prev0 });
    pushRange(prev0, lastChange);
  }

  for (var i = 1; i < changes.length; i++) {
    const nextLastChange = lastChange + margin;
    const newChange = changes[i];
    const prevNewChange = newChange - margin;

    if (nextLastChange + 1 < prevNewChange) {
      pushRange(lastChange + 1, nextLastChange)
      result.push({ numLines: prevNewChange - nextLastChange });
      pushRange(prevNewChange, newChange);
    } else {
      pushRange(lastChange + 1, newChange);
    }
    lastChange = newChange;
  }

  const nextN = lastChange + margin;
  if (nextN < max) {
    pushRange(lastChange + 1, nextN);
    result.push({ numLines: max - nextN });
  } else {
    pushRange(lastChange + 1, max - 1);
  }

  return result;
}
