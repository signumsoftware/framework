import * as React from 'react'
import RichTextEditor, { EditorValue } from 'react-rte';
import { IBinding } from '@framework/Reflection';

export interface HtmlEditorProps {
  binding: IBinding<string | null | undefined>;
  readonly?: boolean;
  format?: "html" | "markdown";
  rootStyle?: React.CSSProperties;
  editorStyle?: React.CSSProperties;
  autoFocus?: boolean;
  innerRef?: React.Ref<RichTextEditor>;
}

export default function HtmlEditor(p: HtmlEditorProps) {

  const [editorValue, setEditorValue] = React.useState<EditorValue>(() => RichTextEditor.createValueFromString(p.binding.getValue() ?? "", p.format ||"html"));

  React.useEffect(() => {

    return () => { saveHtml() };
  });

  function saveHtml() {
    if (!p.readonly)
      p.binding.setValue(editorValue.toString(p.format || "html") ?? "");
  }

  return (
    <RichTextEditor
      ref={p.innerRef}
      value={editorValue}
      readOnly={p.readonly}
      autoFocus={p.autoFocus}
      onChange={ev => setEditorValue(ev)}
      rootStyle={p.rootStyle}
      editorStyle={p.editorStyle}
      {...({ onBlur: () => saveHtml() }) as any}
    />
  );
}

export function MarkdownViewer(p: { markdown: string }) {

  var html = React.useMemo(() => RichTextEditor.createValueFromString(p.markdown, "markdown").toString("html"), [p.markdown]);

  return (
    <div dangerouslySetInnerHTML={{ __html: html }} />
  );
}
