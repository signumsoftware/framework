import * as React from 'react'
import { NumericTextBox, ValueLine, isNumber } from '@framework/Lines/ValueLine';
import { useForceUpdate } from '@framework/Hooks'
import { toNumberFormat } from '@framework/Reflection';
import { Change, diffLines, diffWords } from 'diff';
import { softCast } from '../../../Signum.React/Scripts/Globals';


export interface LineOrWordsChange {
  lineChange: Change;
  wordChanges?: Array<Change>;
}

export function DiffDocument(p: { first: string, second: string }) {
  
  const [margin, setMargin] = React.useState<number | null>(4);
  const [force, setForce] = React.useState<boolean>(false);
  var formatter = toNumberFormat("N0");
  return (
    <div>
      <div>
        <label>
          <input type="checkbox" className="form-check-input" checked={margin != null} onChange={() => setMargin(margin == null ? DiffDocument.defaultMarginLines : null)} />
          <span className="mx-2">Show only</span><NumericTextBox format={toNumberFormat("0")} value={margin == null ? 4 : margin} onChange={num => setMargin(num == null ? 0 : Math.max(num, 0))}
            validateKey={isNumber} /> lines arround each change</label>
      </div>
      <div>
        {(p.first.length * p.second.length > DiffDocument.maxSize * DiffDocument.maxSize) && !force ?
          <div className="alert alert-warning mt-2" role="alert">
            The two strings are too big ({formatter.format(p.first.length)} ch. and {formatter.format(p.second.length)} ch.) and could freeze your browser...
            <br />
            <a href="#" className="btn btn-sm btn-warning mt-3" onClick={e => { e.preventDefault(); setForce(true); }}>Try anyway!</a>
          </div> :
          <DiffDocumentSimple first={p.first} second={p.second} margin={margin} />
        }
      </div>
    </div>
  );
}

DiffDocument.defaultMarginLines = 4 as (number | null);
DiffDocument.maxSize = 300000;



export function DiffDocumentSimple(p: { first: string, second: string, margin?: number | null }) {

  


  const linesDiff = React.useMemo<Array<LineOrWordsChange>>(() => {
 
    var diffs = diffLines(p.first, p.second);
    var result: Array<LineOrWordsChange> = [];

    for (var i = 0; i < diffs.length; i++) {
      var change = diffs[i];
      if (change.removed && change.count == 1 && i + 1 < diffs.length && diffs[i + 1].added && diffs[i + 1].count == 1) {
        var nextChange = diffs[i + 1];
        var wordDiffs = diffWords(change.value, nextChange.value);
        result.push({ lineChange: change, wordChanges: wordDiffs.filter(c => !c.added) });
        result.push({ lineChange: nextChange, wordChanges: wordDiffs.filter(c => !c.removed) });
        i++;
      } else {
        var lines = change.value.replaceAll("\r", "").split("\n");
        if (lines.last() == "")
          lines.removeAt(lines.length - 1);

        var lineChanges = lines.map(v => softCast<LineOrWordsChange>({
          lineChange: { value: v + "\n", count: 1, added: change.added, removed: change.removed }
        }));
        result.push(...lineChanges);
      }
    }

    return [...result]
  }, [p.first, p.second]);


  var indices = p.margin == null ? Array.range(0, linesDiff.length) :
    expandNumbers(linesDiff.map((a, i) => a.lineChange.added || a.lineChange.removed ? i : null).filter(n => n != null) as number[], linesDiff.length, p.margin);

  return (
    <pre className="m-0">{indices.map((ix, i) => {
      if (typeof ix == "object")
        return [<span key={i} style={{ backgroundColor: "#DDD" }}><span> ----- {ix.numLines} Lines Removed ----- </span><br /></span>];

      var line = linesDiff[ix];

      var color = line.lineChange.added ? "#CEF3CE" : line.lineChange.removed ? "#FFD1D1" : undefined;

      if (line.wordChanges) {
        return (<span key={i} style={{ backgroundColor: color }}>
          {line.wordChanges.map((c, j) => {
            var changeColor = c.added ? "#72F272" : c.removed ? "#FF8B8B" : undefined;
            return <span key={j} style={{ backgroundColor: changeColor }}>{c.value}</span>;
          })}
        </span>);
      }
      else
        return <span key={i} style={{ backgroundColor: color }}>{line.lineChange.value}</span>
    })}
    </pre >
  );
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
