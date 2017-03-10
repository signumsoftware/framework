import * as React from 'react'
import CodeMirrorComponent from '../../../Extensions/Signum.React.Extensions/Codemirror/CodeMirrorComponent'
import * as CodeMirror from 'codemirror'

require("!style!css!codemirror/lib/codemirror.css");
require("!style!css!codemirror/addon/dialog/dialog.css");
require("!style!css!codemirror/addon/display/fullscreen.css");
require("!style!css!codemirror/addon/hint/show-hint.css");


require("codemirror/lib/codemirror");
require("codemirror/mode/clike/clike");
require("codemirror/addon/comment/comment");
require("codemirror/addon/comment/continuecomment");
require("codemirror/addon/dialog/dialog");
require("codemirror/addon/display/fullscreen");
require("codemirror/addon/edit/closebrackets");
require("codemirror/addon/edit/matchbrackets");
require("codemirror/addon/hint/show-hint");
require("codemirror/addon/search/match-highlighter");
require("codemirror/addon/search/search");
require("codemirror/addon/search/searchcursor");

interface CSharpCodeMirrorProps {
    script: string;
    onChange?: (newScript: string) => void;
    isReadOnly?: boolean;
    errorLineNumber?: number;
}

export default class CSharpCodeMirror extends React.Component<CSharpCodeMirrorProps, void> { 

    codeMirrorComponent: CodeMirrorComponent;

    render() {        

        const options = {
            lineNumbers: true,
            mode: "text/x-csharp",
            extraKeys: {
                "Ctrl-Space": "autocomplete",
                "Ctrl-K": (cm: any) => cm.lineComment(cm.getCursor(true), cm.getCursor(false)),
                "Ctrl-U": (cm: any) => cm.uncomment(cm.getCursor(true), cm.getCursor(false)),
                "Ctrl-I": (cm: any) => cm.autoFormatRange(cm.getCursor(true), cm.getCursor(false)),
                "F11": (cm: any) => cm.setOption("fullScreen", !cm.getOption("fullScreen")),
                "Esc": (cm: any) => {
                    if (cm.getOption("fullScreen"))
                        cm.setOption("fullScreen", false);
                }
            },
            readOnly: this.props.isReadOnly,
        } as CodeMirror.EditorConfiguration;

        (options as any).highlightSelectionMatches = true;
        (options as any).matchBrackets = true;
        
        return (
            <CodeMirrorComponent value={this.props.script} ref={cm => this.codeMirrorComponent = cm}
                options={options}
                onChange={this.props.isReadOnly ? undefined : this.props.onChange}
                errorLineNumber={this.props.errorLineNumber}
                />
        );
    }
}