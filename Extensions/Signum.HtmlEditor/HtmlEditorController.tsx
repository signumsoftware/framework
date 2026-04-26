import { IBinding } from "@framework/Reflection";
import { $getRoot, EditorState } from "lexical";
import { LexicalEditor } from "lexical";
import React from "react";
import { HtmlEditorExtension } from "./Extensions/types";
import { ITextConverter } from "./HtmlContentStateConverter";
import { Separator } from "./HtmlEditorButtons";
import { isEmpty } from "./Utils/editorState";
import { ImageExtension } from "./Extensions/ImageExtension";
import { ImageHandlerBase } from "./Extensions/ImageExtension/ImageHandlerBase";

export interface HtmlEditorControllerProps {
  binding: IBinding<string | null | undefined>;
  editableId: string;
  readOnly?: boolean;
  small?: boolean;
  converter: ITextConverter;
  extensions?: HtmlEditorExtension[];
  innerRef?: React.Ref<LexicalEditor>;
  initiallyFocused?: boolean | number;
}

export class HtmlEditorController {
  editor!: LexicalEditor;
  editableElement: HTMLDivElement | null = null;
  editorState!: EditorState;

  overrideToolbar!: React.ReactElement | undefined;
  setOverrideToolbar!: (newState: React.ReactElement | undefined) => void;

  converter!: ITextConverter;
  extensions!: HtmlEditorExtension[];
  binding!: IBinding<string | null | undefined>;
  readOnly?: boolean;
  small?: boolean;
  initialEditorContent?: string;
  imageHandler?: ImageHandlerBase;

  lastSavedString?: { str: string | null }

  init(p: HtmlEditorControllerProps): void {
    this.binding = p.binding;
    this.readOnly = p.readOnly;
    this.small = p.small;
    this.converter = p.converter;
    this.extensions = p.extensions ?? [];

    [this.overrideToolbar, this.setOverrideToolbar] = React.useState<
      React.ReactElement | undefined
      >(undefined);

    this.imageHandler = p.extensions?.map(a => a instanceof ImageExtension ? a.imageHandler : null).notNull().singleOrNull() ?? undefined;

    React.useEffect(() => {
      if (p.initiallyFocused) {
        window.setTimeout(
          () => {
            if (this.editor) this.editor.focus();
          },
          p.initiallyFocused == true ? 0 : (p.initiallyFocused as number)
        );
      }
    }, []);

    const newValue = this.binding.getValue();
    React.useEffect(() => {
      if(!this.editor) return;

      if (this.lastSavedString && this.lastSavedString.str === newValue) {
        this.lastSavedString = undefined;
        return;
      }
     
      queueMicrotask(() => {
        const newState = this.converter.$convertFromText(this.editor, newValue || '');

        if (newState.isEmpty()) {
          this.editor.update(() => {
            $getRoot().clear();
          });
        } else {
          this.editor.setEditorState(newState);
        }

        const htmlString = this.converter.$convertToText(this.editor);
        this.initialEditorContent = htmlString;
      });

    }, [newValue, this.editor]);

    React.useEffect(() => {
      return () => this.saveHtml();
    }, []);

    this.setEditorRef = React.useCallback(
      (editor: LexicalEditor | null) => {
        this.editor = editor!;
        if (this.editor)
          this.editor.imageHandler = this.imageHandler;

        if (p.innerRef) {
          if (typeof p.innerRef == "function")
            p.innerRef(editor);
          else
            (p.innerRef as React.RefObject<LexicalEditor | null>).current = editor;
        }
      },
      [p.innerRef]
    );

    this.setContentEditableRef = React.useCallback(
      (element: HTMLDivElement | null) => {
        this.editableElement = element!;
      },
      [p.innerRef]
    );
  }

  saveHtml(): void {
    if (this.readOnly) return;
    
    const newContentString = this.converter.$convertToText(this.editor);
    
    if (newContentString !== this.initialEditorContent) {
      const value = isEmpty(this.editorState) ? null : newContentString;
      this.lastSavedString = { str: value };
      this.binding.setValue(value);
    }
  }

  extraButtons(): React.ReactElement | null {
    const buttons = this.extensions
      .map((p) => p.getToolbarButtons?.(this))
      .notNull();

    if (buttons.length == 0) return null;

    return React.createElement(
      React.Fragment,
      undefined,
      <Separator />,
      ...buttons
    );
  }

  setEditorRef!: (editor: LexicalEditor | null) => void;
  setContentEditableRef!: (editor: HTMLDivElement | null) => void;
}
