import * as React from 'react'
import { TypeContext } from '@framework/TypeContext'
import { CodeMirrorComponent, CodeMirrorComponentHandler } from '../Codemirror/CodeMirrorComponent'
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

export default function HtmlCodemirror(p: {
  ctx: TypeContext<string | null | undefined>,
  onChange?: (newValue: string) => void;
  innerRef?: React.Ref<CodeMirrorComponentHandler>;
}) {
  function handleOnChange(newValue: string) {
    const { ctx, onChange } = p;

    ctx.value = newValue;
    if (onChange != undefined)
      onChange(ctx.value);
  };


  const ctx = p.ctx;

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
    }
  } as CodeMirror.EditorConfiguration;

  (options as any).highlightSelectionMatches = true;
  (options as any).matchBrackets = true;

  return (
    <div>
      <CodeMirrorComponent value={p.ctx.value} ref={p.innerRef}
        options={options}
        onChange={handleOnChange} />
    </div>
  );
}
