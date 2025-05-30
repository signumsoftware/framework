import * as React from 'react'
import { TypeContext } from '@framework/TypeContext'
import { CodeMirrorComponent, CodeMirrorComponentHandler } from './CodeMirrorComponent'
import * as CodeMirror from 'codemirror'

import "codemirror/lib/codemirror.css"
import "codemirror/addon/dialog/dialog.css"
import "codemirror/addon/display/fullscreen.css"
import "codemirror/addon/hint/show-hint.css"
import "codemirror/lib/codemirror"
import "codemirror/mode/htmlmixed/htmlmixed"
import "codemirror/addon/comment/comment"
import "codemirror/addon/comment/continuecomment"
import "codemirror/addon/dialog/dialog"
import "codemirror/addon/display/fullscreen"
import "codemirror/addon/hint/show-hint"
import "codemirror/addon/search/match-highlighter"
import "codemirror/addon/search/search"
import "codemirror/addon/search/searchcursor"

export default function HtmlCodeMirror(p: {
  ctx: TypeContext<string | null | undefined>,
  onChange?: (newValue: string) => void;
  innerRef?: React.Ref<CodeMirrorComponentHandler>;
  options?: Partial<CodeMirror.EditorConfiguration>;
}): React.JSX.Element {

  const { ctx, onChange, innerRef } = p;

  function handleOnChange(newValue: string) {
    if (!ctx.readOnly) {
      ctx.value = newValue;
      if (onChange != undefined)
        onChange(ctx.value);
    }
  };


  const options = {
    lineNumbers: true,
    mode: "htmlmixed",
    extraKeys: {
      "Ctrl-K": (cm: any) => cm.lineComment(cm.getCursor(true), cm.getCursor(false)),
      "Ctrl-U": (cm: any) => cm.uncomment(cm.getCursor(true), cm.getCursor(false)),
      "F11": (cm: any) => cm.setOption("fullScreen", !cm.getOption("fullScreen")),
      "Esc": (cm: any) => {
        if (cm.getOption("fullScreen"))
          cm.setOption("fullScreen", false);
      }
    },
    readOnly: ctx.readOnly,
    ...p.options
  } as CodeMirror.EditorConfiguration;

  (options as any).highlightSelectionMatches = true;
  (options as any).matchBrackets = true;

  return (
    <div>
      <CodeMirrorComponent value={p.ctx.value} ref={innerRef}
        options={options}
        onChange={handleOnChange} />
    </div>
  );
}
