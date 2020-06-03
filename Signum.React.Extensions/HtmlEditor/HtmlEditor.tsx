import * as React from 'react'
import * as draftjs from 'draft-js';
import { IBinding } from '@framework/Reflection';
import { HtmlContentStateConverter } from './HtmlContentStateConverter';
import './HtmlEditor.css'
import { InlineStyleButton, Separator, BlockStyleButton, SubMenuButton } from './HtmlEditorButtons';

export interface IContentStateConverter {
  contentStateToText(content: draftjs.ContentState): string;
  textToContentState(html: string): draftjs.ContentState;
}

export interface ImageConverter<T extends object> {
  uploadData(blob: Blob): Promise<T>;
  renderImage(val: T): React.ReactElement;
  toHtml(val: T): string;
  fromHtml(val: T): string;
}

export interface HtmlEditorProps {
  binding: IBinding<string | null | undefined>;
  readOnly?: boolean;
  images?: ImageConverter<Object>
  converter?: IContentStateConverter;
  innerRef?: React.Ref<draftjs.Editor>;
  toolbarButtons?: (c: HtmlEditorController) => React.ReactElement | React.ReactFragment;
}

export interface HtmlEditorControllerProps {
  binding: IBinding<string | null | undefined>;
  readOnly?: boolean;
  converter: IContentStateConverter,
  innerRef?: React.Ref<draftjs.Editor>;
  images?: ImageConverter<object>;
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
  images?: ImageConverter<object>;

  init(p: HtmlEditorControllerProps) {

    this.binding = p.binding;
    this.readOnly = p.readOnly;
    this.converter = p.converter;
    this.images = p.images;

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

export default React.forwardRef(function HtmlEditor({ readOnly, binding, converter, innerRef, toolbarButtons, images, ...props }: HtmlEditorProps & Partial<draftjs.EditorProps>, ref?: React.Ref<HtmlEditorController>) {

  var c = React.useMemo(() => new HtmlEditorController(), []);
  React.useImperativeHandle(ref, () => c, []);
  c.init({ binding, readOnly, converter: converter ?? HtmlContentStateConverter.default, innerRef, images });

  const editorProps = props as draftjs.EditorProps;
  basicCommandsPlugin(editorProps, c);
  imagePlugin(editorProps, c);

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


export function basicCommandsPlugin(props: draftjs.EditorProps, controller: HtmlEditorController) {

  var prevKeyCommand = props.handleKeyCommand;
  props.handleKeyCommand = (command, state, timeStamp) => {

    if (prevKeyCommand) {
      var result = prevKeyCommand(command, state, timeStamp);
      if (result == "handled")
        return result;
    }

    const inlineStyle =
      command == "bold" ? "BOLD" :
        command == "italic" ? "ITALIC" :
          command == "underline" ? "UNDERLINE" :
            undefined;

    if (inlineStyle) {
      controller.setEditorState(draftjs.RichUtils.toggleInlineStyle(controller.editorState, inlineStyle));
      return "handled";
    }

    return "not-handled"; 
  }

  return props
}

export function imagePlugin(props: draftjs.EditorProps, controller: HtmlEditorController) {

  var oldRenderer = props.blockRendererFn;
  props.blockRendererFn = (block) => {

    if (oldRenderer) {
      const result = oldRenderer(block);
      if (result)
        return result;
    }

    if (block.getType() === 'atomic') {
      const contentState = controller.editorState.getCurrentContent();
      const entity = block.getEntityAt(0);
      if (!entity) return null;
      const type = contentState.getEntity(entity).getType();
      if (type === 'IMAGE' || type === 'image') {
        return {
          component: ImageComponent,
          editable: false,
          props: { controller }
        };
      }
    }

    return null;
  };

  function addImage(editorState: draftjs.EditorState, data: Object): draftjs.EditorState {
    const contentState = editorState.getCurrentContent();
    const contentStateWithEntity = contentState.createEntity(
      'IMAGE',
      'IMMUTABLE',
      data
    );
    const entityKey = contentStateWithEntity.getLastCreatedEntityKey();
    const newEditorState = draftjs.AtomicBlockUtils.insertAtomicBlock(
      editorState,
      entityKey,
      ' '
    );

    return draftjs.EditorState.forceSelection(
      newEditorState,
      newEditorState.getCurrentContent().getSelectionAfter()
    );
  }

  props.handlePastedFiles = files => {
    const imageFiles = files.filter(a => a.type.startsWith("image/"));
    if (imageFiles.length == 0)
      return "not-handled";

    Promise.all(imageFiles.map(blob => controller.images!.uploadData(blob)))
      .then(datas => {
        var newState = datas.reduce<draftjs.EditorState>((state, data) => addImage(state, data), controller.editorState);
        controller.setEditorState(newState);
      }).done();

    return "handled"
  }

  props.handleDroppedFiles = (selection, files) => {
    const imageFiles = files.filter(a => a.type.startsWith("image/"));
    if (imageFiles.length == 0)
      return "not-handled";

    const editorStateWithSelection = draftjs.EditorState.acceptSelection(controller.editorState, selection);
    Promise.all(imageFiles.map(blob => controller.images!.uploadData(blob)))
      .then(datas => {
        var newState = datas.reduce<draftjs.EditorState>((state, data) => addImage(state, data), editorStateWithSelection);
        controller.setEditorState(newState);
      }).done();

    return "handled"
  }

  return { addImage };
}

export function ImageComponent(p: { contentState: draftjs.ContentState, block: draftjs.ContentBlock, controller: HtmlEditorController }) {
  const data = p.contentState.getEntity(p.block.getEntityAt(0)).getData();
  return p.controller.images!.renderImage(data);
}
