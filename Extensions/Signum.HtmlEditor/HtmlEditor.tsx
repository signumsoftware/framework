import { classes, softCast } from "@framework/Globals";
import { IBinding } from "@framework/Reflection";
import { $isCodeNode } from "@lexical/code";
import { LexicalComposer } from "@lexical/react/LexicalComposer";
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
import { useForceUpdate } from "../../Signum/React/Hooks";
import { HtmlEditorMessage } from "../../Signum/React/Signum.Entities";
import { ImageExtension } from "./Extensions/ImageExtension";

export interface HtmlEditorProps {
  ref?: React.Ref<HtmlEditorController>;
  binding: IBinding<string | null | undefined>;
  readOnly?: boolean;
  small?: boolean;
  mandatory?: boolean | "warning";
  converter?: ITextConverter;
  innerRef?: React.Ref<LexicalEditor>;
  extensionsMemo?: HtmlEditorExtension[];
  handleKeybindings?: (event: KeyboardEvent) => boolean;
  toolbarButtons?: (c: HtmlEditorController) => React.ReactNode;
  placeholder?: React.ReactNode;
  htmlAttributes?: React.HTMLAttributes<HTMLDivElement>;
  initiallyFocused?: boolean | number;
  /** Names the editable area. A label element cannot do it: `for` only binds to form controls, and this is
   * a contenteditable div, so without this the text box reaches a screen reader unnamed. */
  ariaLabel?: string;
  onEditorFocus?: (e: React.FocusEvent, controller: HtmlEditorController) => void;
  onEditorBlur?: (e: React.FocusEvent, controller: HtmlEditorController) => void;
}

const createUid = () => Math.random().toString(36).substring(2, 9);

function HtmlEditor(
  {
    ref,
    readOnly,
    small,
    binding,
    converter,
    innerRef,
    toolbarButtons,
    extensionsMemo,
    htmlAttributes,
    mandatory,
    initiallyFocused,
    handleKeybindings,
    placeholder,
    ...props }: HtmlEditorProps
): React.JSX.Element {
  const forceUpdate = useForceUpdate();
  const id = React.useMemo(() => createUid(), []);
  const editableId = "editable_" + id;
  const { controller, nodes, builtinPlugins } = useController({
    binding,
    readOnly,
    small,
    converter,
    innerRef,
    initiallyFocused,
    extensionsMemo,
    handleKeybindings,
    editableId
  });

  React.useImperativeHandle(ref, () => controller, [controller]);

  const error = binding.getError();

  const imageHandler = extensionsMemo?.filter(a => a instanceof ImageExtension ? a.imageHandler : null).notNull().singleOrNull() == null;

  return (
    <div
      title={error}
      onClick={() => controller.editor?.focus()}
      {...htmlAttributes}
      className={classes(
        "sf-html-editor",
        controller.readOnly && "read-only",
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
          editable: !readOnly,
          // @ts-ignore
          imageHandler: imageHandler
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
              ref={controller.setContentEditableRef}
              id={editableId}
              className="public-DraftEditor-content"
              // Lexical gives its div role="textbox" whether or not the editor is editable, and adds
              // aria-readonly when it is not. Read-only, that div has contentEditable={false} and no
              // tabindex, so the text box it announces can be neither reached nor edited by anyone: a
              // dashboard greeting was read out as an empty, unnamed edit field (axe aria-input-field-name,
              // WCAG 4.1.2), and giving it a name would only have made the phantom field easier to find.
              // It is static text, so it is left as a plain div and read as text. aria-readonly goes with
              // the role it belongs to, and so does aria-autocomplete: Lexical hard-codes it to "none" on
              // the non-editable branch, where it was left behind by the role it used to belong to, and
              // aria-autocomplete is not allowed on role="presentation" (axe aria-allowed-attr). The
              // kebab-case keys are the ones the element spreads last, so these win over what Lexical sets;
              // the camelCase props it destructures would not.
              {...(readOnly ? { role: "presentation", "aria-readonly": undefined, "aria-autocomplete": undefined } : { ariaLabel: props.ariaLabel })}
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
        <EditorRefPlugin editorRef={comp => { controller.setEditorRef(comp); if (comp) forceUpdate(); }} />
        <HistoryPlugin />
        {...builtinPlugins.map((a, i) => React.cloneElement(a, { key: i }))}
      </LexicalComposer>
    </div>
  );
}

export default HtmlEditor;

const defaultToolbarButtons = (c: HtmlEditorController) => (
  <div className="sf-draft-toolbar">
    <InlineStyleButton controller={c} style="bold" icon="bold" title={HtmlEditorMessage.Bold.niceToString()} aria-label={HtmlEditorMessage.Bold.niceToString()} />
    <InlineStyleButton controller={c} style="italic" icon="italic" title={HtmlEditorMessage.Italic.niceToString()} aria-label={HtmlEditorMessage.Italic.niceToString()} />
    <InlineStyleButton controller={c} style="underline" icon="underline" title={HtmlEditorMessage.Underline.niceToString()} aria-label={HtmlEditorMessage.Underline.niceToString()} />
    <InlineStyleButton controller={c} style="code" icon="code" title={HtmlEditorMessage.Code.niceToString()} aria-label={HtmlEditorMessage.Code.niceToString()} />
    <Separator />
    <SubMenuButton controller={c} title={HtmlEditorMessage.Headings.niceToString()} aria-label={HtmlEditorMessage.Headings.niceToString()} icon="heading">
      <BlockStyleButton controller={c} blockType="h1" content="H1" isActiveFn={isHeadingActive} onClick={(editor) => formatHeading(editor, "h1")} />
      <BlockStyleButton controller={c} blockType="h2" content="H2" isActiveFn={isHeadingActive} onClick={(editor) => formatHeading(editor, "h2")} />
      <BlockStyleButton controller={c} blockType="h3" content="H3" isActiveFn={isHeadingActive} onClick={(editor) => formatHeading(editor, "h3")} />
    </SubMenuButton>
    <BlockStyleButton controller={c} blockType="ul" icon="list-ul" title={HtmlEditorMessage.UnorderedList.niceToString()} aria-label={HtmlEditorMessage.UnorderedList.niceToString()} isActiveFn={isListActive} onClick={(editor) => formatList(editor, "ul")} />
    <BlockStyleButton controller={c} blockType="ol" icon="list-ol" title={HtmlEditorMessage.OrderedList.niceToString()} aria-label={HtmlEditorMessage.OrderedList.niceToString()} isActiveFn={isListActive} onClick={(editor) => formatList(editor, "ol")} />
    <BlockStyleButton controller={c} blockType="blockquote" icon="quote-right" title={HtmlEditorMessage.Quote.niceToString()} aria-label={HtmlEditorMessage.Quote.niceToString()} isActiveFn={isQuoteActive} onClick={formatQuote} />
    <BlockStyleButton controller={c} blockType="code-block" icon="file-code" title={HtmlEditorMessage.CodeBlock.niceToString()} aria-label={HtmlEditorMessage.CodeBlock.niceToString()} isActiveFn={(selection) => !!$findMatchingParent(selection.anchor.getNode(), node => $isCodeNode(node))} onClick={formatCode} />
    {c.extraButtons()}
  </div>
);

