import * as React from 'react'
import PluginEditor, { PluginEditorProps, EditorPlugin } from 'draft-js-plugins-editor';
import * as draftjs from 'draft-js';
import { IBinding } from '@framework/Reflection';
import { HtmlContentStateConverter } from './HtmlContentStateConverter';
import './EditorStyle.css'
import { useUpdatedRef } from '../../../Framework/Signum.React/Scripts/Hooks';

export interface IContentStateConverter {
  contentStateToText(content: draftjs.ContentState): string;
  textToContentState(html: string): draftjs.ContentState;
}

export interface HtmlEditorProps extends Partial<PluginEditorProps> {
  binding: IBinding<string | null | undefined>;
  readOnly?: boolean;
  converter?: IContentStateConverter;
  innerRef?: React.Ref<PluginEditor>;
  beforeEditor?: (editor: React.RefObject<PluginEditor>) => React.ReactNode | null | undefined;
  afterEditor?: (editor: React.RefObject<PluginEditor>) => React.ReactNode | null | undefined;
  plugins?: EditorPlugin[];
  decorators?: draftjs.DraftDecorator[]
}

export default function HtmlEditor({ readOnly, binding, converter, innerRef, beforeEditor, afterEditor, ...props }: HtmlEditorProps) {

  const [editorState, setEditorState] = React.useState<draftjs.EditorState>(() => draftjs.EditorState.createWithContent(converter!.textToContentState(binding.getValue() ?? "")));
  var ref = React.useRef<PluginEditor | null>(null);

  React.useEffect(() => {
    return () => { saveHtmlRef.current() };
  }, []);

  React.useEffect(() => {
    setEditorState(draftjs.EditorState.createWithContent(converter!.textToContentState(binding.getValue() ?? "")));
  }, [binding.getValue()]);

  function saveHtml() {
    if (!readOnly) {
      var value = converter!.contentStateToText(editorState.getCurrentContent());
      if (value ?? "" != binding.getValue() ?? "")
        binding.setValue(value);
    }
  }

  const saveHtmlRef = useUpdatedRef(saveHtml);

  var setRefs = React.useCallback((editor: PluginEditor | null) => {
    ref.current = editor;
    if (innerRef) {
      if (typeof innerRef == "function")
        innerRef(editor);
      else
        (innerRef as React.MutableRefObject<PluginEditor | null>).current = editor;
    }
  }, [innerRef]);

  return (
    <div className="html-editor" onClick={() => ref.current!.focus()}>
      {(beforeEditor ?? HtmlEditor.defaultBeforeEditor)(ref)}
      <PluginEditor
        ref={setRefs}
        editorState={editorState}
        readOnly={readOnly}
        defaultKeyBindings
        defaultKeyCommands
        onBlur={() => saveHtml()}
        onChange={ev => setEditorState(ev)}
        plugins={HtmlEditor.defaultPlugins}
        decorators={HtmlEditor.defaultDecorators}
        {...props}
      />
      {(afterEditor ?? HtmlEditor.defaultAfterEditor)(ref)}
    </div>
  );
}

HtmlEditor.defaultProps = {
  converter: new HtmlContentStateConverter(),
};



HtmlEditor.defaultBeforeEditor = (editor: React.RefObject<PluginEditor>) : React.ReactNode | null | undefined => null;
HtmlEditor.defaultAfterEditor = (editor: React.RefObject<PluginEditor>): React.ReactNode | null | undefined => null;
HtmlEditor.defaultPlugins = [] as EditorPlugin[];
HtmlEditor.defaultDecorators = [] as draftjs.DraftDecorator[]; 
