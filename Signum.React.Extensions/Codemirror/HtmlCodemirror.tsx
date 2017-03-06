import * as React from 'react'
import { getQueryNiceName } from '../../../Framework/Signum.React/Scripts/Reflection'
import * as Navigator from '../../../Framework/Signum.React/Scripts/Navigator'
import { TypeContext, FormGroupStyle } from '../../../Framework/Signum.React/Scripts/TypeContext'
import CodeMirrorComponent from '../Codemirror/CodeMirrorComponent'
import * as CodeMirror from 'codemirror'

require("!style!css!codemirror/lib/codemirror.css");
require("!style!css!codemirror/addon/dialog/dialog.css");
require("!style!css!codemirror/addon/display/fullscreen.css");
require("!style!css!codemirror/addon/hint/show-hint.css");


require("codemirror/lib/codemirror");
require("codemirror/mode/htmlmixed/htmlmixed");
require("codemirror/addon/comment/comment");
require("codemirror/addon/comment/continuecomment");
require("codemirror/addon/dialog/dialog");
require("codemirror/addon/display/fullscreen");
require("codemirror/addon/hint/show-hint");
require("codemirror/addon/search/match-highlighter");
require("codemirror/addon/search/search");
require("codemirror/addon/search/searchcursor");

export default class HtmlCodemirror extends React.Component<{ ctx: TypeContext<string | null | undefined>, onChange?: (newValue: string) => void }, void> {

    get entity() {
        return this.props.ctx.value;
    }

    changedHandler: number;
    exceptionHandler: number;

    handleOnChange = (newValue: string) => {
        const { ctx, onChange } = this.props;

        ctx.value = newValue;
        if (onChange != undefined)
            onChange(ctx.value);
    };
    
    codeMirrorComponent: CodeMirrorComponent;
    
    render() {

        const ctx = this.props.ctx;

        const options = {
            lineNumbers: true,
            mode: "htmlmixed",
            extraKeys: {
                "Ctrl-K": (cm : any) => cm.lineComment(cm.getCursor(true), cm.getCursor(false)),
                "Ctrl-U": (cm: any) => cm.uncomment(cm.getCursor(true), cm.getCursor(false)),
                "F11": (cm: any) => cm.setOption("fullScreen", !cm.getOption("fullScreen")),
                "Esc": (cm: any) => {
                    if (cm.getOption("fullScreen"))
                        cm.setOption("fullScreen", false);
                }
            }
        } as CodeMirror.EditorConfiguration;

        (options as any).highlightSelectionMatches = true;
        (options as any).matchBrackets = true;
        
        return (
            <div>
                <CodeMirrorComponent value={this.props.ctx.value} ref={cm => this.codeMirrorComponent = cm}
                    options={options}
                    onChange={this.handleOnChange}/>
            </div>
        );
    }
}