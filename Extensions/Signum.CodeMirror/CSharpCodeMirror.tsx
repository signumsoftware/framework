import * as React from 'react'
import { CodeMirrorComponent, CodeMirrorComponentHandler } from './CodeMirrorComponent'
import * as CodeMirror from 'codemirror'

import "codemirror/lib/codemirror.css"
import "codemirror/addon/dialog/dialog.css"
import "codemirror/addon/display/fullscreen.css"
import "codemirror/addon/hint/show-hint.css"
import "codemirror/lib/codemirror"
import "codemirror/mode/clike/clike"
import "codemirror/addon/comment/comment"
import "codemirror/addon/comment/continuecomment"
import "codemirror/addon/dialog/dialog"
import "codemirror/addon/display/fullscreen"
import "codemirror/addon/edit/closebrackets"
import "codemirror/addon/edit/matchbrackets"
import "codemirror/addon/hint/show-hint"
import "codemirror/addon/search/match-highlighter"
import "codemirror/addon/search/search"
import "codemirror/addon/search/searchcursor"

interface CSharpCodeMirrorProps {
  script: string;
  onChange?: (newScript: string) => void;
  isReadOnly?: boolean;
  errorLineNumber?: number;
  innerRef?: React.Ref<CodeMirrorComponentHandler>;
  onInit?: (cm: CodeMirror.EditorFromTextArea) => void;
}

export default function CSharpCodeMirror(p: CSharpCodeMirrorProps) {

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
    readOnly: p.isReadOnly,
  } as CodeMirror.EditorConfiguration;

  (options as any).highlightSelectionMatches = true;
  (options as any).matchBrackets = true;

  return (
    <CodeMirrorComponent value={p.script} ref={p.innerRef}
      options={options}
      onChange={p.isReadOnly ? undefined : p.onChange}
      errorLineNumber={p.errorLineNumber}
      onInit={p.onInit}
    />
  );
}
