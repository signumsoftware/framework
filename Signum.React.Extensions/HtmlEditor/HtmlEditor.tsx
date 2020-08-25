import * as React from 'react'
import * as draftjs from 'draft-js';
import { IBinding } from '@framework/Reflection';
import { HtmlContentStateConverter } from './HtmlContentStateConverter';
import './HtmlEditor.css'
import { InlineStyleButton, Separator, BlockStyleButton, SubMenuButton } from './HtmlEditorButtons';
import BasicCommandsPlugin from './Plugins/BasicCommandsPlugin';

export interface IContentStateConverter {
  contentStateToText(content: draftjs.ContentState): string;
  textToContentState(html: string): draftjs.ContentState;
}

export interface HtmlEditorProps {
  binding: IBinding<string | null | undefined>;
  readOnly?: boolean;
  converter?: IContentStateConverter;
  innerRef?: React.Ref<draftjs.Editor>;
  decorators?: draftjs.DraftDecorator[],
  plugins?: HtmlEditorPlugin[],
  toolbarButtons?: (c: HtmlEditorController) => React.ReactElement | React.ReactFragment | null;
}

export interface HtmlEditorControllerProps {
  binding: IBinding<string | null | undefined>;
  readOnly?: boolean;
  converter: IContentStateConverter,
  decorators?: draftjs.DraftDecorator[],
  plugins?: HtmlEditorPlugin[];
  innerRef?: React.Ref<draftjs.Editor>;
}

export class HtmlEditorController {
  editor!: draftjs.Editor;

  editorState !: draftjs.EditorState;
  setEditorState !: (newState: draftjs.EditorState) => void;

  overrideToolbar !: React.ReactFragment | React.ReactElement | undefined;
  setOverrideToolbar!: (newState: React.ReactFragment | React.ReactElement | undefined) => void;

  converter!: IContentStateConverter;
  decorators!: draftjs.DraftDecorator[];
  plugins!: HtmlEditorPlugin[]; 
  binding!: IBinding<string | null | undefined>;
  readOnly?: boolean;

  init(p: HtmlEditorControllerProps) {

    this.binding = p.binding;
    this.readOnly = p.readOnly;
    this.converter = p.converter;
    this.plugins = p.plugins ?? [];
    this.decorators = [...p.decorators ?? [], ...this.plugins.flatMap(p => p.getDecorators == null ? [] : p.getDecorators(this))];

    [this.editorState, this.setEditorState] = React.useState<draftjs.EditorState>(() => draftjs.EditorState.createWithContent(
      p.converter!.textToContentState(this.binding.getValue() ?? ""),
      this.decorators.length == 0 ? undefined : new draftjs.CompositeDecorator(this.decorators)));

    [this.overrideToolbar, this.setOverrideToolbar] = React.useState<React.ReactFragment | React.ReactElement | undefined>(undefined);

    React.useEffect(() => {
      if (this.binding.getValue() ?? "" == this.lastSave ?? "")
        this.lastSave = undefined;
      else
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
      if (value ?? "" != this.binding.getValue() ?? "") {
        this.lastSave = value;
        this.binding.setValue(value);
      }
    }
  }

  extraButtons(): React.ReactFragment | null {

    var buttons = this.plugins.map(p => p.getToolbarButtons && p.getToolbarButtons(this)).notNull();

    if (buttons.length == 0)
      return null;

    return React.createElement(React.Fragment, undefined, <Separator />, ...buttons);
  }

  lastSave: string | undefined;

  setRefs!: (editor: draftjs.Editor | null) => void;
}


export default React.forwardRef(function HtmlEditor({
  readOnly,
  binding,
  converter,
  innerRef,
  toolbarButtons,
  decorators,
  plugins,
  ...props }: HtmlEditorProps & Partial<draftjs.EditorProps>, ref?: React.Ref<HtmlEditorController>) {

  const textConverter = converter ?? new HtmlContentStateConverter({}, {});

  plugins = plugins ?? [new BasicCommandsPlugin()];

  plugins.forEach(p => p.expandConverter && p.expandConverter(textConverter));

  var c = React.useMemo(() => new HtmlEditorController(), []);
  React.useImperativeHandle(ref, () => c, []);
  c.init({
    binding,
    readOnly,
    converter: textConverter,
    innerRef,
    decorators,
    plugins,
  });

  const editorProps = props as draftjs.EditorProps;

  if (editorProps.keyBindingFn == undefined)
    editorProps.keyBindingFn = draftjs.getDefaultKeyBinding;

  if (editorProps.handleKeyCommand == undefined)
    editorProps.handleKeyCommand = command => {
      const newState = draftjs.RichUtils.handleKeyCommand(
        c.editorState,
        command
      );
      if (newState) {
        c.setEditorState(newState);
        return "handled";
      }
      return "not-handled";
    };

  plugins.forEach(p => p.expandEditorProps && p.expandEditorProps(editorProps, c));

  return (
    <>
      <div className="sf-html-editor" onClick={() => c.editor.focus()}>
        {c.overrideToolbar ?? (toolbarButtons ? toolbarButtons(c) : defaultToolbarButtons(c))}

        <draftjs.Editor
          ref={c.setRefs}
          editorState={c.editorState}
          readOnly={readOnly}
          onBlur={() => c.saveHtml()}
          onChange={ev => c.setEditorState(ev)}
          {...props}
        />
      </div>
      {/*<pre style={{ textAlign: "left" }}>
        {JSON.stringify(draftjs.convertToRaw(c.editorState.getCurrentContent()), undefined, 2)}
      </pre>*/}
    </>
  );
});

const defaultToolbarButtons = (c: HtmlEditorController) => <div className="sf-draft-toolbar">
  <InlineStyleButton controller={c} style="BOLD" icon="bold" title="Bold (Ctrl + B)" />
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
  {c.extraButtons()}
</div>;


export interface HtmlEditorPlugin {
  getDecorators?(controller: HtmlEditorController): [draftjs.DraftDecorator];
  getToolbarButtons?(controller: HtmlEditorController): React.ReactChild;
  expandConverter?(converter: IContentStateConverter): void;
  expandEditorProps?(props: draftjs.EditorProps, controller: HtmlEditorController): void;
}
