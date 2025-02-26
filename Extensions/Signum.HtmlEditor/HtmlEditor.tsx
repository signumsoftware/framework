import { IBinding } from "@framework/Reflection";
import { LexicalComposer } from "@lexical/react/LexicalComposer";
import * as React from "react";
import {
  ITextConverter
} from "./HtmlContentStateConverter";
import "./HtmlEditor.css";
import { classes } from "@framework/Globals";
import { ContentEditable } from "@lexical/react/LexicalContentEditable";
import { EditorRefPlugin } from "@lexical/react/LexicalEditorRefPlugin";
import { LexicalErrorBoundary } from "@lexical/react/LexicalErrorBoundary";
import { RichTextPlugin } from "@lexical/react/LexicalRichTextPlugin";
import { HeadingNode, QuoteNode } from "@lexical/rich-text";
import { $getRoot, LexicalEditor } from "lexical";
import { HtmlEditorExtension } from "./Extensions/types";
import {
  BlockStyleButton,
  InlineStyleButton,
  Separator,
  SubMenuButton,
} from "./HtmlEditorButtons";
import { HtmlEditorController } from "./HtmlEditorController";
import { useController } from "./useController";

export interface HtmlEditorProps {
  binding: IBinding<string | null | undefined>;
  readOnly?: boolean;
  small?: boolean;
  mandatory?: boolean | "warning";
  converter?: ITextConverter;
  innerRef?: React.Ref<LexicalEditor>;
  //   decorators?: draftjs.DraftDecorator[];
  plugins?: HtmlEditorExtension[];
  toolbarButtons?: (
    c: HtmlEditorController
  ) => React.ReactElement | React.ReactFragment | null;
  htmlAttributes?: React.HTMLAttributes<HTMLDivElement>;
  initiallyFocused?: boolean | number;
  onEditorFocus?: (
    e: React.FocusEvent,
    controller: HtmlEditorController
  ) => void;
  onEditorBlur?: (
    e: React.FocusEvent,
    controller: HtmlEditorController
  ) => void;
}

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
    ...props }: HtmlEditorProps,
  ref?: React.Ref<HtmlEditorController>
) {
  const { controller, nodes, builtinComponents } = useController({
    binding,
    readOnly,
    small,
    converter,
    innerRef,
    initiallyFocused,
    plugins,
  });

  React.useImperativeHandle(ref, () => controller, [controller]);

  const hasText = React.useMemo(() => {
    let hasText = false;
    controller.editorState?.read(() => {
      hasText = $getRoot().getTextContentSize() > 0;
    })
    return hasText;
  }, [controller.editorState]);
  
  const error = binding.getError();

  return (
    <>
      <div
        title={error}
        onClick={() => controller.editor?.focus()}
        {...htmlAttributes}
        className={classes(
          "sf-html-editor",
          mandatory &&
            !hasText &&
            (mandatory == "warning" ? "sf-mandatory-warning" : "sf-mandatory"),
          error && "has-error",
          controller.small ? "small-mode" : "",
          htmlAttributes?.className
        )}
      >
        <LexicalComposer
          initialConfig={{
            namespace: "HtmlEditor",
            nodes: [HeadingNode, QuoteNode, ...nodes!],
            theme: {
                text: {
                    underline: 'text-underline',
                    code: 'code-block'
                }
            },
            onError: (error) => console.error('fwo#error', error),
          }}
        >
            {controller.overrideToolbar ? (
              <div className="sf-draft-toolbar">{controller.overrideToolbar}</div>
            ) : toolbarButtons ? (
              toolbarButtons(controller)
            ) : controller.readOnly || controller.small ? null : (
              defaultToolbarButtons(controller)
            )}
              <RichTextPlugin
                contentEditable={
                  <ContentEditable
                    className="public-DraftEditor-content"
                    readOnly={controller.readOnly}
                    onFocus={(event: React.FocusEvent) => {
                      props.onEditorFocus?.(event, controller);
                    }}
                    onBlur={(event: React.FocusEvent) => {
                      props.onEditorBlur?.(event, controller);
                      controller.saveHtml();
                    }}
                  />
                }
                ErrorBoundary={LexicalErrorBoundary}
              />
              <EditorRefPlugin editorRef={controller.setRefs} />
              {builtinComponents.map(({component: Component, props }) => <Component key={Component.name} {...props} />)}
        </LexicalComposer>
      </div>
    </>
  );
});

export default HtmlEditor;

const defaultToolbarButtons = (c: HtmlEditorController) => (
  <div className="sf-draft-toolbar">
    <InlineStyleButton
      controller={c}
      style="bold"
      icon="bold"
      title="Bold (Ctrl + B)"
    />
    <InlineStyleButton
      controller={c}
      style="italic"
      icon="italic"
      title="Italic (Ctrl + I)"
    />
    <InlineStyleButton
      controller={c}
      style="underline"
      icon="underline"
      title="Underline (Ctrl + U)"
    />
    <InlineStyleButton controller={c} style="code" icon="code" title="Code" />
    <Separator />
    <SubMenuButton controller={c} title="Headings..." icon="heading">
      <BlockStyleButton controller={c} blockType="h1" content="H1" />
      <BlockStyleButton controller={c} blockType="h2" content="H2" />
      <BlockStyleButton controller={c} blockType="h3" content="H3" />
    </SubMenuButton>
    <BlockStyleButton
      controller={c}
      blockType="ul"
      icon="list-ul"
      title="Unordered list"
    />
    <BlockStyleButton
      controller={c}
      blockType="ol"
      icon="list-ol"
      title="Ordered list"
    />
    <BlockStyleButton
      controller={c}
      blockType="blockquote"
      icon="quote-right"
      title="Quote"
    />
    {/* <BlockStyleButton
      controller={c}
      blockType="code-block"
      icon="file-code"
      title="Code Block"
    /> */}
    {c.extraButtons()}
  </div>
);

// export interface HtmlEditorPlugin {
//   //   getDecorators?(controller: HtmlEditorController): draftjs.DraftDecorator[];
//   getToolbarButtons?(controller: HtmlEditorController): React.ReactChild;
//   expandConverter?(converter: ITextConverter): void;
//   expandEditorProps?(
//     props: any, //draftjs.EditorProps,
//     controller: HtmlEditorController
//   ): void;
// }
