import * as React from 'react';
import { CodeMirrorComponentHandler } from './CodeMirrorComponent';
import "codemirror/lib/codemirror.css";
import "codemirror/addon/dialog/dialog.css";
import "codemirror/addon/display/fullscreen.css";
import "codemirror/addon/hint/show-hint.css";
import "codemirror/lib/codemirror";
import "codemirror/mode/javascript/javascript";
import "codemirror/addon/comment/comment";
import "codemirror/addon/comment/continuecomment";
import "codemirror/addon/dialog/dialog";
import "codemirror/addon/display/fullscreen";
import "codemirror/addon/edit/closebrackets";
import "codemirror/addon/edit/matchbrackets";
import "codemirror/addon/hint/show-hint";
import "codemirror/addon/hint/javascript-hint";
import "codemirror/addon/search/match-highlighter";
import "codemirror/addon/search/search";
import "codemirror/addon/search/searchcursor";
interface JavascriptCodeMirrorProps {
    code: string;
    onChange?: (code: string) => void;
    innerRef?: React.Ref<CodeMirrorComponentHandler>;
}
export default function JavascriptCodeMirror(p: JavascriptCodeMirrorProps): React.JSX.Element;
export {};
//# sourceMappingURL=JavascriptCodeMirror.d.ts.map