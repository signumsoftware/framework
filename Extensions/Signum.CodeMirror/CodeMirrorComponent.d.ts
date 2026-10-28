import * as React from 'react';
import * as CodeMirror from 'codemirror';
import "codemirror/lib/codemirror.css";
export interface CodeMirrorProps {
    ref?: React.Ref<CodeMirrorComponentHandler>;
    onChange?: (value: string) => void;
    onFocusChange?: (focused: boolean) => void;
    options?: CodeMirror.EditorConfiguration;
    path?: string;
    value?: string | null;
    className?: string;
    errorLineNumber?: number;
    onInit?: (cm: CodeMirror.EditorFromTextArea) => void;
}
export interface CodeMirrorComponentHandler {
    codeMirror?: CodeMirror.EditorFromTextArea;
}
export declare function CodeMirrorComponent(p: CodeMirrorProps): React.JSX.Element;
//# sourceMappingURL=CodeMirrorComponent.d.ts.map