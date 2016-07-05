import * as React from 'react'
import { UserQueryEntity, UserQueryMessage, QueryFilterEntity, QueryOrderEntity, QueryColumnEntity } from '../../UserQueries/Signum.Entities.UserQueries'
import ChartBuilder from '../Templates/ChartBuilder'
import { ChartScriptEntity, ChartScriptColumnEntity, ChartScriptParameterEntity } from '../Signum.Entities.Chart'
import { FormGroup, FormControlStatic, ValueLine, ValueLineType, EntityLine, EntityCombo, EntityList, EntityRepeater} from '../../../../Framework/Signum.React/Scripts/Lines'
import * as Finder from '../../../../Framework/Signum.React/Scripts/Finder'
import { QueryDescription, SubTokensOptions } from '../../../../Framework/Signum.React/Scripts/FindOptions'
import { getQueryNiceName } from '../../../../Framework/Signum.React/Scripts/Reflection'
import * as Navigator from '../../../../Framework/Signum.React/Scripts/Navigator'
import { TypeContext, FormGroupStyle } from '../../../../Framework/Signum.React/Scripts/TypeContext'
import QueryTokenEntityBuilder from '../../UserAssets/Templates/QueryTokenEntityBuilder'
import CodeMirrorComponent from '../../Codemirror/CodeMirrorComponent'
import * as CodeMirror from 'codemirror'

require("!style!css!../Chart.css");

require("!style!css!codemirror/lib/codemirror.css");
require("!style!css!codemirror/addon/dialog/dialog.css");
require("!style!css!codemirror/addon/display/fullscreen.css");
require("!style!css!codemirror/addon/hint/show-hint.css");


require("codemirror/lib/codemirror");
require("codemirror/mode/javascript/javascript");
require("codemirror/addon/comment/comment");
require("codemirror/addon/comment/continuecomment");
require("codemirror/addon/dialog/dialog");
require("codemirror/addon/display/fullscreen");
require("codemirror/addon/edit/closebrackets");
require("codemirror/addon/edit/matchbrackets");
require("codemirror/addon/hint/show-hint");
require("codemirror/addon/hint/javascript-hint");
require("codemirror/addon/search/match-highlighter");
require("codemirror/addon/search/search");
require("codemirror/addon/search/searchcursor");

export default class ChartScriptCode extends React.Component<{ ctx: TypeContext<ChartScriptEntity> }, void> {

    get entity() {
        return this.props.ctx.value;
    }

    changedHandler: number;
    exceptionHandler: number;

    handleOnChange = (newValue: string) => {
        this.props.ctx.value.script = newValue;
        this.props.ctx.value.modified = true;

        if (opener != null && opener != undefined) {
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

    codeMirrorComponent: CodeMirrorComponent;
    lineHandle: CodeMirror.LineHandle;
    getException = () => {
        var number = (opener as any).getExceptionNumber();
        clearTimeout(this.exceptionHandler);
        if (this.lineHandle != null)
            this.codeMirrorComponent.codeMirror.removeLineClass(this.lineHandle, null, null);
        if (number != -1)
            this.lineHandle = this.codeMirrorComponent.codeMirror.addLineClass(number - 1, null, "exceptionLine");

    }

    render() {

        var ctx = this.props.ctx;

        var options = {
            lineNumbers: true,
            mode: "javascript",
            extraKeys: {
                "Ctrl-Space": "autocomplete",
                "Ctrl-K": cm => cm.lineComment(cm.getCursor(true), cm.getCursor(false)),
                "Ctrl-U": cm => cm.uncomment(cm.getCursor(true), cm.getCursor(false)),
                "Ctrl-I": cm => cm.autoFormatRange(cm.getCursor(true), cm.getCursor(false)),
                "F11": cm => cm.setOption("fullScreen", !cm.getOption("fullScreen")),
                "Esc": cm => {
                    if (cm.getOption("fullScreen"))
                        cm.setOption("fullScreen", false);
                }
            }
        } as CodeMirror.EditorConfiguration;

        (options as any).highlightSelectionMatches = true;
        (options as any).matchBrackets = true;

        var css = ".exceptionLine { background: 'pink' }";


        return (
            <div>
                <pre style={{ color: "Green", overflowStyle: "inherit" }}>{ChartScriptCode.example}</pre>
                <style>{css}</style>
                <CodeMirrorComponent value={this.props.ctx.value.script} ref={cm => this.codeMirrorComponent = cm}
                    options={options}
                    onChange={this.handleOnChange}/>
            </div>
        );
    }

    static example = `//var chart = d3.select('#sfChartControl .sf-chart-container').append('svg:svg').attr('width', width).attr('height', height))
//var data = { 
//              "columns": { "c0": { "title":"Product", "token":"Product", "isGroupKey":true, ... }, 
                             "c1": { "title":"Count", "token":"Count", "isGroupKey":true, ...} 
                          },
//              "rows": [ { "c0": { "key": "Product;1", "toStr": "Apple", "color": null }, "c1": { "key": "140", "toStr": "140" } },
//                        { "c0": { "key": "Product;2", "toStr": "Orange", "color": null }, "c1": { "key": "179", "toStr": "179" } }, ...
//                      ]
//           }
// DrawChart(chart, data);
// 
// Visit: http://d3js.org/
// Other functions defined in: \Chart\Scripts\ChartUtils.js
// use 'debugger' keyword or just throw JSON.stringify(myVariable)
// All yours!...`
}