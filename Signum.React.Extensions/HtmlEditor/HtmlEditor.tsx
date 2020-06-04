import * as React from 'react'
import * as draftjs from 'draft-js';
import { IBinding } from '@framework/Reflection';
import { HtmlContentStateConverter } from './HtmlContentStateConverter';
import './HtmlEditor.css'
import { InlineStyleButton, Separator, BlockStyleButton, SubMenuButton } from './HtmlEditorButtons';
import { imagePlugin, ImageConverter } from './imagePlugin';
import { basicCommandsPlugin } from './basicCommandPlugin';

export interface IContentStateConverter {
  contentStateToText(content: draftjs.ContentState): string;
  textToContentState(html: string): draftjs.ContentState;
}

export interface HtmlEditorProps {
  binding: IBinding<string | null | undefined>;
  readOnly?: boolean;
  basicCommands?: boolean;
  imageConverter?: ImageConverter<Object>
  converter?: IContentStateConverter;
  innerRef?: React.Ref<draftjs.Editor>;
  toolbarButtons?: (c: HtmlEditorController) => React.ReactElement | React.ReactFragment;
}

export interface HtmlEditorControllerProps {
  binding: IBinding<string | null | undefined>;
  readOnly?: boolean;
  converter: IContentStateConverter,
  innerRef?: React.Ref<draftjs.Editor>;
}

export class HtmlEditorController {
  editor!: draftjs.Editor;

  editorState !: draftjs.EditorState;
  setEditorState !: (newState: draftjs.EditorState) => void;

  overrideToolbar !: React.ReactFragment | React.ReactElement | undefined;
  setOverrideToolbar!: (newState: React.ReactFragment | React.ReactElement | undefined) => void;

  converter!: IContentStateConverter;
  binding!: IBinding<string | null | undefined>;
  readOnly?: boolean;

  init(p: HtmlEditorControllerProps) {

    this.binding = p.binding;
    this.readOnly = p.readOnly;
    this.converter = p.converter;

    [this.editorState, this.setEditorState] = React.useState<draftjs.EditorState>(() => draftjs.EditorState.createWithContent(p.converter!.textToContentState(this.binding.getValue() ?? "")));
    [this.overrideToolbar, this.setOverrideToolbar] = React.useState<React.ReactFragment | React.ReactElement | undefined>(undefined);

    React.useEffect(() => {
      this.setEditorState(draftjs.EditorState.createWithContent(this.converter.textToContentState(this.binding.getValue() ?? "")));
    }, [this.binding.getValue()]);

    React.useEffect(() => {
      return () => { this.saveHtml() };
    }, []);

    this.setRefs = React.useCallback((editor: draftjs.Editor | null) => {
      this.editor = editor!;
      if (p.innerRef) {
        if (typeof p.innerRef == "function")
          p.innerRef(editor);
        else
          (p.innerRef as React.MutableRefObject<draftjs.Editor | null>).current = editor;
      }
    }, [p.innerRef]);
  }

  saveHtml() {
    if (!this.readOnly) {
      var value = this.converter.contentStateToText(this.editorState.getCurrentContent());
      if (value ?? "" != this.binding.getValue() ?? "")
        this.binding.setValue(value);
    }
  }

  setRefs!: (editor: draftjs.Editor | null) => void;
}



export default React.forwardRef(function HtmlEditor({ readOnly, binding, converter, innerRef, toolbarButtons, imageConverter, basicCommands, ...props }: HtmlEditorProps & Partial<draftjs.EditorProps>, ref?: React.Ref<HtmlEditorController>) {

  var c = React.useMemo(() => new HtmlEditorController(), []);
  React.useImperativeHandle(ref, () => c, []);
  c.init({
    binding,
    readOnly,
    converter: converter ?? new HtmlContentStateConverter({}, {}),
    innerRef,
  });

  const editorProps = props as draftjs.EditorProps;

  if (basicCommands != false)
    basicCommandsPlugin(editorProps, c);

  if (imageConverter != null)
    imagePlugin(editorProps, c, imageConverter);

  console.log("Rendering: " + JSON.stringify(draftjs.convertToRaw(c.editorState.getCurrentContent()), undefined, 2));

  return (
    <div className="sf-html-editor" onClick={() => c.editor.focus()}>
      <div className="sf-draft-toolbar">
        {c.overrideToolbar ?? (toolbarButtons ? toolbarButtons(c) : defaultToolbarButtons(c))}
      </div>
      <draftjs.Editor
        ref={c.setRefs}
        editorState={c.editorState}
        readOnly={readOnly}
        onBlur={() => c.saveHtml()}
        onChange={ev => c.setEditorState(ev)}
        {...props}
      />
    </div>
  );
});

const defaultToolbarButtons = (c: HtmlEditorController) => <>
  <InlineStyleButton controller={c} style="BOLD" icon="bold" title="Bold (Ctrl + B)"  />
  <InlineStyleButton controller={c} style="ITALIC" icon="italic" title="Italic (Ctrl + I)" />
  <InlineStyleButton controller={c} style="UNDERLINE" icon="underline" title="Underline (Ctrl + U)" />
  <InlineStyleButton controller={c} style="CODE" icon="code" title="Code" />
  <Separator />
  <SubMenuButton controller={c} title="Headings..." icon="heading">
    <BlockStyleButton controller={c} blockType="header-one" content="H1" />
    <BlockStyleButton controller={c} blockType="header-two" content="H2" />
    <BlockStyleButton controller={c} blockType="header-three" content="H3" />
  </SubMenuButton>
  <BlockStyleButton controller={c} blockType="unordered-list-item" icon="list-ul" title="Unordered list" />
  <BlockStyleButton controller={c} blockType="ordered-list-item" icon="list-ol" title="Ordered list" />
  <BlockStyleButton controller={c} blockType="blockquote" icon="quote-right" title="Quote" />
  <BlockStyleButton controller={c} blockType="code-block" icon={["far", "file-code"]} title="Quote" />
</>;
