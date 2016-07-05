import * as React from 'react'
import { UserQueryEntity, UserQueryMessage, QueryFilterEntity, QueryOrderEntity, QueryColumnEntity } from '../../UserQueries/Signum.Entities.UserQueries'
import { FormGroup, FormControlStatic, ValueLine, ValueLineType, EntityLine, EntityCombo, EntityList, EntityRepeater} from '../../../../Framework/Signum.React/Scripts/Lines'
import * as Finder from '../../../../Framework/Signum.React/Scripts/Finder'
import { QueryDescription, SubTokensOptions } from '../../../../Framework/Signum.React/Scripts/FindOptions'
import { getQueryNiceName } from '../../../../Framework/Signum.React/Scripts/Reflection'
import * as Navigator from '../../../../Framework/Signum.React/Scripts/Navigator'
import { TypeContext, FormGroupStyle } from '../../../../Framework/Signum.React/Scripts/TypeContext'
import QueryTokenEntityBuilder from '../../UserAssets/Templates/QueryTokenEntityBuilder'
import CodeMirrorComponent from '../../Codemirror/CodeMirrorComponent'
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

export default class HtmlCodemirror extends React.Component<{ ctx: TypeContext<string>, onChange?: (newValue: string) => void  }, void> {

    get entity() {
        return this.props.ctx.value;
    }

    changedHandler: number;
    exceptionHandler: number;

    handleOnChange = (newValue: string) => {
        var { ctx, onChange } = this.props;

        ctx.value = newValue;
        if (onChange != null)
            onChange(ctx.value);
    };
    
    codeMirrorComponent: CodeMirrorComponent;
    
    render() {

        var ctx = this.props.ctx;

        var options = {
            lineNumbers: true,
            mode: "htmlmixed",
            extraKeys: {
                "Ctrl-K": cm => cm.lineComment(cm.getCursor(true), cm.getCursor(false)),
                "Ctrl-U": cm => cm.uncomment(cm.getCursor(true), cm.getCursor(false)),
                "F11": cm => cm.setOption("fullScreen", !cm.getOption("fullScreen")),
                "Esc": cm => {
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