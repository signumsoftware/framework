import * as React from 'react'
import * as CodeMirror from 'codemirror'
import { classes } from '@framework/Globals'

import "codemirror/lib/codemirror.css"

export interface CodeMirrorProps {
  onChange?: (value: string) => void,
  onFocusChange?: (focused: boolean) => void,
  options?: CodeMirror.EditorConfiguration,
  path?: string,
  value?: string | null,
  className?: string,
  errorLineNumber?: number;
}


export interface CodeMirrorComponentHandler {
  focus() : void;
}

export const CodeMirrorComponent = React.forwardRef(function CodeMirrorComponent(p: CodeMirrorProps, ref: React.Ref<CodeMirrorComponentHandler>) {

  const textAreaRef = React.useRef<HTMLTextAreaElement>(null);
  const codeMirrorRef = React.useRef<CodeMirror.EditorFromTextArea | undefined>(undefined);

  const [isFocused, setIsFocused] = React.useState(false);

  React.useEffect(() => {

    const codeMirror = codeMirrorRef.current = CodeMirror.fromTextArea(textAreaRef.current!, p.options);
    if (p.onChange)
      codeMirror.on('change', codemirrorValueChanged);
    codeMirror.on('focus', () => focusChanged(true));
    codeMirror.on('blur', () => focusChanged.bind(false));
    codeMirror.setValue(p.value ?? '');

    return () => {
      codeMirror.toTextArea();
    }
  }, []);


  React.useEffect(() => {
    const codeMirror = codeMirrorRef.current;
    if (codeMirror && p.value != codeMirror.getValue()) {
      codeMirror.off('change', codemirrorValueChanged);
      codeMirror.setValue(p.value ?? "");
      codeMirror.on('change', codemirrorValueChanged);
    }
  }, [p.value]);

  const lineHandleRef = React.useRef<CodeMirror.LineHandle | undefined>(undefined);

  React.useEffect(() => {
    if (lineHandleRef.current != undefined)
      codeMirrorRef.current!.removeLineClass(lineHandleRef.current, undefined as any, undefined);

    if (p.errorLineNumber != null)
      lineHandleRef.current = codeMirrorRef.current!.addLineClass(p.errorLineNumber - 1, undefined as any, "exceptionLine");
  }, [p.errorLineNumber]);

  React.useEffect(() => {
    if (typeof p.options === 'object') {
      for (let optionName in p.options) {
        const optName = optionName as keyof CodeMirror.EditorConfiguration;
        var newValue = p.options[optName];
        if (codeMirrorRef.current!.getOption(optName as keyof CodeMirror.EditorConfiguration) != newValue)
          codeMirrorRef.current!.setOption(optName, newValue);
      }
    }
  });

  function focus() {
    if (codeMirrorRef.current) {
      codeMirrorRef.current.focus();
    }
  }

  React.useImperativeHandle(ref, () => ({
    focus
  }));

  function focusChanged(focused: boolean) {
    setIsFocused(focused);
    p.onFocusChange && p.onFocusChange(focused);
  }

  function codemirrorValueChanged(doc: CodeMirror.Editor, change: CodeMirror.EditorChangeLinkedList) {
    const newValue = doc.getValue();
    if (newValue != p.value && p.onChange)
      p.onChange(newValue);
  }

  const editorClassName = classes(
    'ReactCodeMirror',
    isFocused ? 'ReactCodeMirror--focused' : undefined,
    p.className
  );

  const css = ".exceptionLine { background: pink }";
  return (
    <div className={editorClassName}>
      <style>{css}</style>
      <textarea ref={textAreaRef} name={p.path} defaultValue={p.value ?? undefined} autoComplete="off" />
    </div>
  );
});
