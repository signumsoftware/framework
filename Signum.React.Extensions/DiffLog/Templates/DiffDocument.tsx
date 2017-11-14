import * as React from 'react'
import { DiffPair } from 'DiffLog/DiffLogClient';
export class DiffDocument extends React.Component<{diff:Array<DiffPair<Array<DiffPair<string>>>> }>
{


   render()
   {
       return this.renderDiffDocument(this.props.diff)
   }
   
   renderDiffDocument(diff: Array<DiffPair<Array<DiffPair<string>>>>): React.ReactElement<any> {
        
            const result = diff.flatMap(line => {
                if (line.Action == "Removed") {
                    return [<span style={{ backgroundColor: "#FFD1D1" }}>{this.renderDiffLine(line.Value) }</span>];
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
        
        
            return <pre>{result.map((e, i) => React.cloneElement(e, { key: i })) }</pre>;
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
        
            result.push(<br key={result.length}/>);
            return result;
        }

}