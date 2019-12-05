import * as React from 'react'
import RichTextEditor, { EditorValue } from 'react-rte';
import { IBinding } from '@framework/Reflection';

export interface HtmlEditorProps {
  binding: IBinding<string | null | undefined>;
  readonly?: boolean;
  rootStyle?: React.CSSProperties;
  editorStyle?: React.CSSProperties;
}

export default function HtmlEditor(p: HtmlEditorProps) {


  const [editorValue, setEditorValue] = React.useState<EditorValue>(() => RichTextEditor.createValueFromString(p.binding.getValue() ?? "", "html"));

  React.useEffect(() => {


    return () => { saveHtml() };
  });

  function componentWillUnmount() {
    saveHtml();
  }

  function saveHtml() {
    if (!p.readonly)
      p.binding.setValue(editorValue.toString("html") ?? "");
  }

  return (
    <RichTextEditor
      value={editorValue}
      readOnly={p.readonly}
      onChange={ev => setEditorValue(editorValue)}
      rootStyle={p.rootStyle}
      editorStyle={p.editorStyle}
      {...({ onBlur: () => saveHtml() }) as any}
    />
  );
}

