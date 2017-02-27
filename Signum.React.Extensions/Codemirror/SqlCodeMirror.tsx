import * as React from 'react'
import CodeMirrorComponent from './CodeMirrorComponent'
import * as CodeMirror from 'codemirror'

require("codemirror/lib/codemirror.css");
require("codemirror/addon/dialog/dialog.css");
require("codemirror/addon/display/fullscreen.css");
require("codemirror/addon/hint/show-hint.css");


require("codemirror/lib/codemirror");
require("codemirror/mode/sql/sql");
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

interface SqlCodeMirrorProps {
    script: string;
    onChange?: (newScript: string) => void;
    isReadOnly?: boolean;
}

export default class SqlCodeMirror extends React.Component<SqlCodeMirrorProps, void> {

    codeMirrorComponent: CodeMirrorComponent;

    render() {

        const options = {
            lineNumbers: true,
            mode: "text/x-mssql",
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
                onChange={this.props.onChange} />
        );
    }
}