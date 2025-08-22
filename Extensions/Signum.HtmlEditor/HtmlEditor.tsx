import { classes } from "@framework/Globals";
import { IBinding } from "@framework/Reflection";
import { $isCodeNode } from "@lexical/code";
import { InitialConfigType, LexicalComposer } from "@lexical/react/LexicalComposer";
import { ContentEditable } from "@lexical/react/LexicalContentEditable";
import { EditorRefPlugin } from "@lexical/react/LexicalEditorRefPlugin";
import { LexicalErrorBoundary } from "@lexical/react/LexicalErrorBoundary";
import { RichTextPlugin } from "@lexical/react/LexicalRichTextPlugin";
import { HistoryPlugin } from "@lexical/react/LexicalHistoryPlugin";
import { HeadingNode, QuoteNode } from "@lexical/rich-text";
import { LexicalEditor } from "lexical";
import * as React from "react";
import { HtmlEditorExtension } from "./Extensions/types";
import {
  ITextConverter
} from "./HtmlContentStateConverter";
import "./HtmlEditor.css";
import {
  BlockStyleButton,
  InlineStyleButton,
  Separator,
  SubMenuButton,
} from "./HtmlEditorButtons";
import { HtmlEditorController } from "./HtmlEditorController";
import LexicalTheme from "./LexicalTheme";
import { useController } from "./useController";
import { isEmpty } from "./Utils/editorState";
import { formatCode, formatHeading, formatList, formatQuote } from "./Utils/format";
import { $findMatchingParent, isHeadingActive, isListActive, isQuoteActive } from "./Utils/node";

export interface HtmlEditorProps {
  binding: IBinding<string | null | undefined>;
  readOnly?: boolean;
  small?: boolean;
  mandatory?: boolean | "warning";
  converter?: ITextConverter;
  innerRef?: React.Ref<LexicalEditor>;
  plugins?: HtmlEditorExtension[];
  handleKeybindings?: (event: KeyboardEvent) => boolean;
  toolbarButtons?: (c: HtmlEditorController) => React.ReactNode;
  placeholder?: React.ReactNode;
  htmlAttributes?: React.HTMLAttributes<HTMLDivElement>;
  initiallyFocused?: boolean | number;
  onEditorFocus?: (e: React.FocusEvent, controller: HtmlEditorController) => void;
  onEditorBlur?: (e: React.FocusEvent, controller: HtmlEditorController) => void;
}

const createUid = () => Math.random().toString(36).substring(2, 9);

const HtmlEditor: React.ForwardRefExoticComponent<HtmlEditorProps & React.RefAttributes<HtmlEditorController>> = React.forwardRef(function HtmlEditor(
  {
    readOnly,
    small,
    binding,
    converter,
    innerRef,
    toolbarButtons,
    plugins,
    htmlAttributes,
    mandatory,
    initiallyFocused,
    handleKeybindings,
    placeholder,
    ...props }: HtmlEditorProps,
  ref?: React.Ref<HtmlEditorController>
) {
  const id = React.useMemo(() => createUid(), []);
  const editableId = "editable_" + id;
  const { controller, nodes, builtinComponents } = useController({
    binding,
    readOnly,
    small,
    converter,
    innerRef,
    initiallyFocused,
    plugins,
    handleKeybindings,
    editableId
  });

  React.useImperativeHandle(ref, () => controller, [controller]);

  const error = binding.getError();

  return (
    <div
      title={error}
      onClick={() => controller.editor?.focus()}
      {...htmlAttributes}
      className={classes(
        "sf-html-editor",
        mandatory &&
        isEmpty(controller.editorState) &&
        (mandatory == "warning" ? "sf-mandatory-warning" : "sf-mandatory"),
        error && "has-error",
        controller.small ? "small-mode" : "",
        htmlAttributes?.className
      )}
    >
      <LexicalComposer
        initialConfig={{
          namespace: "HtmlEditor_" + id,
          nodes: [HeadingNode, QuoteNode, ...nodes!],
          theme: LexicalTheme,
          onError: (error) => console.error(error),
          editable: !readOnly
        }}
      >
        {
          controller.overrideToolbar ? <div className="sf-draft-toolbar">{controller.overrideToolbar}</div> :
            toolbarButtons ? toolbarButtons(controller) :
              controller.readOnly || controller.small ? null :
                defaultToolbarButtons(controller)
        }
        <RichTextPlugin
          contentEditable={
            <ContentEditable
              id={editableId}
              className="public-DraftEditor-content"
              onFocus={(event: React.FocusEvent) => {
                props.onEditorFocus?.(event, controller);
              }}
              onBlur={(event: React.FocusEvent) => {
                props.onEditorBlur?.(event, controller);
                controller.saveHtml();
              }}
            />
          }
          placeholder={Boolean(placeholder) ? <div className="sf-html-editor-placeholder">{placeholder}</div> : undefined}
          ErrorBoundary={LexicalErrorBoundary}
        />
        <EditorRefPlugin editorRef={controller.setRefs} />
        <HistoryPlugin />
        {builtinComponents.map(({ component: Component, props }) => <Component key={Component.name} {...props} />)}
      </LexicalComposer>
    </div>
  );
});

export default HtmlEditor;

const defaultToolbarButtons = (c: HtmlEditorController) => (
  <div className="sf-draft-toolbar">
    <InlineStyleButton controller={c} style="bold" icon="bold" title="Bold (Ctrl + B)" />
    <InlineStyleButton controller={c} style="italic" icon="italic" title="Italic (Ctrl + I)" />
    <InlineStyleButton controller={c} style="underline" icon="underline" title="Underline (Ctrl + U)" />
    <InlineStyleButton controller={c} style="code" icon="code" title="Code" />
    <Separator />
    <SubMenuButton controller={c} title="Headings..." icon="heading">
      <BlockStyleButton controller={c} blockType="h1" content="H1" isActiveFn={isHeadingActive} onClick={(editor) => formatHeading(editor, "h1")} />
      <BlockStyleButton controller={c} blockType="h2" content="H2" isActiveFn={isHeadingActive} onClick={(editor) => formatHeading(editor, "h2")} />
      <BlockStyleButton controller={c} blockType="h3" content="H3" isActiveFn={isHeadingActive} onClick={(editor) => formatHeading(editor, "h3")} />
    </SubMenuButton>
    <BlockStyleButton controller={c} blockType="ul" icon="list-ul" title="Unordered list" isActiveFn={isListActive} onClick={(editor) => formatList(editor, "ul")} />
    <BlockStyleButton controller={c} blockType="ol" icon="list-ol" title="Ordered list" isActiveFn={isListActive} onClick={(editor) => formatList(editor, "ol")} />
    <BlockStyleButton controller={c} blockType="blockquote" icon="quote-right" title="Quote" isActiveFn={isQuoteActive} onClick={formatQuote} />
    <BlockStyleButton controller={c} blockType="code-block" icon="file-code" title="Code Block" isActiveFn={(selection) => !!$findMatchingParent(selection.anchor.getNode(), node => $isCodeNode(node))} onClick={formatCode} />
    {c.extraButtons()}
  </div>
);
