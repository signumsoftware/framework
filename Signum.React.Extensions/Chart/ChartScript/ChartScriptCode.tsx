import * as React from 'react'
import { UserQueryEntity, UserQueryMessage, QueryFilterEmbedded, QueryOrderEmbedded, QueryColumnEmbedded } from '../../UserQueries/Signum.Entities.UserQueries'
import ChartBuilder from '../Templates/ChartBuilder'
import { ChartScriptEntity, ChartScriptColumnEmbedded, ChartScriptParameterEmbedded } from '../Signum.Entities.Chart'
import { FormGroup, FormControlReadonly, ValueLine, ValueLineType, EntityLine, EntityCombo, EntityList, EntityRepeater} from '../../../../Framework/Signum.React/Scripts/Lines'
import * as Finder from '../../../../Framework/Signum.React/Scripts/Finder'
import { QueryDescription, SubTokensOptions } from '../../../../Framework/Signum.React/Scripts/FindOptions'
import { getQueryNiceName } from '../../../../Framework/Signum.React/Scripts/Reflection'
import * as Navigator from '../../../../Framework/Signum.React/Scripts/Navigator'
import { TypeContext, FormGroupStyle } from '../../../../Framework/Signum.React/Scripts/TypeContext'
import QueryTokenEntityBuilder from '../../UserAssets/Templates/QueryTokenEntityBuilder'
import JavascriptCodeMirror from '../../Codemirror/JavascriptCodeMirror'

import "../Chart.css"

export default class ChartScriptCode extends React.Component<{ ctx: TypeContext<ChartScriptEntity> }> {

    get entity() {
        return this.props.ctx.value;
    }

    changedHandler!: number;
    exceptionHandler!: number;

    handleOnChange = (newValue: string) => {
        this.props.ctx.value.script = newValue;
        this.props.ctx.value.modified = true;
        this.forceUpdate();

        if (opener != undefined) {
            clearTimeout(this.changedHandler);
            this.changedHandler = setTimeout(this.updatePreview, 150);
        }
    };

    updatePreview = () => {
        if (opener.changeScript) { //was rendered
            opener.changeScript(this.props.ctx.value);
            this.exceptionHandler = setTimeout(this.getException, 100);
        }
    }

    jsCodeMirror!: JavascriptCodeMirror;
    lineHandle?: CodeMirror.LineHandle;

    getException = () => {
        const number = opener.getExceptionNumber();
        clearTimeout(this.exceptionHandler);
        if (this.lineHandle != undefined)
            this.jsCodeMirror.codeMirrorComponent.codeMirror.removeLineClass(this.lineHandle, undefined, undefined);
        if (number != -1)
            this.lineHandle = this.jsCodeMirror.codeMirrorComponent.codeMirror.addLineClass(number - 1, undefined, "exceptionLine");

    }

    render() {

        const ctx = this.props.ctx;

      

        const css = ".exceptionLine { background: 'pink' }";


        return (
            <div className="code-container">
                <pre style={{ color: "Green", overflowStyle: "inherit" }}>{ChartScriptCode.example}</pre>
                <style>{css}</style>
                <JavascriptCodeMirror code={this.props.ctx.value.script || ""} ref={jscm => this.jsCodeMirror = jscm!}
                    onChange={this.handleOnChange}/>
            </div>
        );
    }

    static example = `//const chart = d3.select('#sfChartControl .sf-chart-container').append('svg:svg').attr('width', width).attr('height', height))
//const data = { 
//              "columns": { "c0": { "title":"Product", "token":"Product", "isGroupKey":true, ... }, 
                             "c1": { "title":"Count", "token":"Count", "isGroupKey":true, ...} 
                          },
//              "rows": [ { "c0": { "key": "Product;1", "toStr": "Apple", "color": undefined }, "c1": { "key": "140", "toStr": "140" } },
//                        { "c0": { "key": "Product;2", "toStr": "Orange", "color": undefined }, "c1": { "key": "179", "toStr": "179" } }, ...
//                      ]
//           }
// DrawChart(chart, data);
// 
// Visit: http://d3js.org/
// Other functions defined in: \Chart\Scripts\ChartUtils.js
// use 'debugger' keyword or just throw JSON.stringify(myVariable)
// All yours!...`
}