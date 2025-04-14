import { IBinding } from "@framework/Reflection";
import { $getRoot, EditorState } from "lexical";
import { LexicalEditor } from "lexical/LexicalEditor";
import React from "react";
import { HtmlEditorExtension } from "./Extensions/types";
import { ITextConverter } from "./HtmlContentStateConverter";
import { Separator } from "./HtmlEditorButtons";
import { isEmpty } from "./Utils/editorState";

export interface HtmlEditorControllerProps {
  binding: IBinding<string | null | undefined>;
  readOnly?: boolean;
  small?: boolean;
  converter: ITextConverter;
  plugins?: HtmlEditorExtension[];
  innerRef?: React.Ref<LexicalEditor>;
  initiallyFocused?: boolean | number;
}

export class HtmlEditorController {
  editor!: LexicalEditor;
  editableElement: HTMLElement | null = null;
  editorState!: EditorState;
  setEditorState!: (newState: EditorState) => void;

  overrideToolbar!: React.ReactElement | undefined;
  setOverrideToolbar!: (newState: React.ReactElement | undefined) => void;

  converter!: ITextConverter;
  plugins!: HtmlEditorExtension[];
  binding!: IBinding<string | null | undefined>;
  readOnly?: boolean;
  small?: boolean;
  initialEditorState?: EditorState;

  lastSavedString?: { str: string | null };

  createWithContentAndDecorators(contentState?: EditorState): EditorState {
    this.editor?.update(() => {
      if (contentState) {
        this.editor.setEditorState(contentState);
      } else {
        $getRoot().clear();
      }
    });

    return this.editor?.getEditorState();
  }

  private initializeEditableElement() {
    return document.getElementById("editor-editable");
  }

  init(p: HtmlEditorControllerProps): void {
    this.binding = p.binding;
    this.readOnly = p.readOnly;
    this.small = p.small;
    this.converter = p.converter;
    this.plugins = p.plugins ?? [];
    this.editableElement = this.initializeEditableElement();

    [this.editorState, this.setEditorState] = React.useState<EditorState>(() =>
      this.createWithContentAndDecorators()
    );

    [this.overrideToolbar, this.setOverrideToolbar] = React.useState<
      React.ReactElement | undefined
    >(undefined);

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
      if(!this.editor || !newValue) return;

      if (this.lastSavedString?.str === newValue) {
        this.lastSavedString = undefined;
        return;
      }

      this.initialEditorState = this.editor.getEditorState();
      queueMicrotask(() => {
        this.setEditorState(this.converter.$convertFromText(this.editor, newValue));
      })
    }, [newValue, this.editor]);

    React.useEffect(() => {
      return () => this.saveHtml();
    }, []);

    this.setRefs = React.useCallback(
      (editor: LexicalEditor | null) => {
        this.editor = editor!;
        if (p.innerRef) {
          if (typeof p.innerRef == "function") p.innerRef(editor);
          else
            (
              p.innerRef as React.MutableRefObject<LexicalEditor | null>
            ).current = editor;
        }
      },
      [p.innerRef]
    );
  }

  saveHtml(): void {
    if (this.readOnly) return;

    const newContentString = JSON.stringify(this.editorState);
    const initialContentString = JSON.stringify(this.initialEditorState);
    if (newContentString !== initialContentString) {
      const value = isEmpty(this.editorState) ? null : this.converter.$convertToText(this.editor);
      this.lastSavedString = { str: value };
      this.binding.setValue(value);
    }
  }

  extraButtons(): React.ReactElement | null {
    const buttons = this.plugins
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

  setRefs!: (editor: LexicalEditor | null) => void;
}
