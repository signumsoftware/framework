import * as React from 'react'
import { DiffPair } from '../DiffLogClient';
import { NumericTextBox, ValueLine } from '../../../../Framework/Signum.React/Scripts/Lines/ValueLine';

export class DiffDocument extends React.Component<{ diff: Array<DiffPair<Array<DiffPair<string>>>> }>
{
    static marginLines: number | null = 4;
    handleSetMargin = (newMargin: number | null) => {
        DiffDocument.marginLines = newMargin;
        this.forceUpdate();
    }

    render() {

        var margin = DiffDocument.marginLines;

        return (
            <div>
                <div>
                    <label><input type="checkbox" checked={margin != null} onChange={() => this.handleSetMargin(margin == null ? 4 : null)} />
                        Show only <NumericTextBox value={margin == null ? 4 : margin} onChange={num => this.handleSetMargin(num == null ? 0 : Math.max(num, 0))}
                            validateKey={ValueLine.isNumber} /> lines arround each change</label>
                </div>
                <div>
                    {this.renderLines()}
                </div>
            </div>
        );
    }

    
    renderLines() {
        var diff = this.props.diff;
        
        var margin = DiffDocument.marginLines;

        var indices = margin == null ? Array.range(0, diff.length) :
            expandNumbers(diff.map((a, i) => a.Action != "Equal" || a.Value.length != 1 ? i : null).filter(n => n != null) as number[], diff.length);
        
        const result =
            indices
                .flatMap(ix => {
                    if (typeof ix == "object")
                        return [<span style={{ backgroundColor: "#DDD" }}><span> ----- {ix.numLines} Lines Removed ----- </span><br/></span>];

                    var line = diff[ix];

                    if (line.Action == "Removed") {
                        return [<span style={{ backgroundColor: "#FFD1D1" }}>{this.renderDiffLine(line.Value)}</span>];
                    }
                    if (line.Action == "Added") {
                        return [<span style={{ backgroundColor: "#CEF3CE" }}>{this.renderDiffLine(line.Value)}</span>];
                    }
                    else if (line.Action == "Equal") {
                        if (line.Value.length == 1) {
                            return [<span>{this.renderDiffLine(line.Value)}</span>];
                        }
                        else {
                            return [
                                <span style={{ backgroundColor: "#FFD1D1" }}>{this.renderDiffLine(line.Value.filter(a => a.Action == "Removed" || a.Action == "Equal"))}</span>,
                                <span style={{ backgroundColor: "#CEF3CE" }}>{this.renderDiffLine(line.Value.filter(a => a.Action == "Added" || a.Action == "Equal"))}</span>
                            ];
                        }
                    }
                    else
                        throw new Error("Unexpected");
                });

        return <pre>{result.map((e, i) => React.cloneElement(e, { key: i }))}</pre>;
    }


    renderDiffLine(list: Array<DiffPair<string>>): Array<React.ReactElement<any>> {
        const result = list.map((a, i) => {
            if (a.Action == "Equal")
                return <span key={i}>{a.Value}</span>;
            else if (a.Action == "Added")
                return <span key={i} style={{ backgroundColor: "#72F272" }}>{a.Value}</span>;
            else if (a.Action == "Removed")
                return <span key={i} style={{ backgroundColor: "#FF8B8B" }}>{a.Value}</span>;
            else
                throw Error("");
        });

        result.push(<br key={result.length} />);
        return result;
    }

}

interface LinesRemoved {
    numLines: number;
}

function expandNumbers(changes: number[], max: number): (number | LinesRemoved)[] {

    const margin = DiffDocument.marginLines!;

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

    const nextN = lastChange + margin ;
    if (nextN < max) {
        pushRange(lastChange + 1, nextN);
        result.push({ numLines: max - nextN });
    } else {
        pushRange(lastChange + 1, max - 1);
    }

    return result;
}
