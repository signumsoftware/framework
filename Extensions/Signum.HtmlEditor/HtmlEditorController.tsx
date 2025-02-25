import { IBinding } from "@framework/Reflection";
import { $getRoot, EditorState } from "lexical";
import { LexicalEditor } from "lexical/LexicalEditor";
import React, { useEffect, useRef, useState } from "react";
import { HtmlEditorExtension } from "./Extensions/types";
import { ITextConverter } from "./HtmlContentStateConverter";
import { Separator } from "./HtmlEditorButtons";

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

  editorState!: EditorState;
  setEditorState!: (newState: EditorState) => void;

  overrideToolbar!: React.ReactElement | undefined;
  setOverrideToolbar!: (newState: React.ReactElement | undefined) => void;

  converter!: ITextConverter;
  plugins!: HtmlEditorExtension[];
  binding!: IBinding<string | null | undefined>;
  readOnly?: boolean;
  small?: boolean;
  initialContentState?: EditorState;

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

  init(p: HtmlEditorControllerProps): void {
    this.binding = p.binding;
    this.readOnly = p.readOnly;
    this.small = p.small;
    this.converter = p.converter;
    this.plugins = p.plugins ?? [];

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

    var newValue = this.binding.getValue();
    React.useEffect(() => {
      if (this.lastSavedString && this.lastSavedString.str == newValue) {
        this.lastSavedString = undefined;
        return;
      }

      this.editorState?.read(() => {
        this.initialContentState = this.converter.$convertFromText(
          this.editor,
          newValue ?? ""
        );
      });

      this.setEditorState(this.createWithContentAndDecorators());
    }, [newValue]);

    React.useEffect(() => {
      return () => {
        this.saveHtml();
      };
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

    let newContentString = JSON.stringify(this.editorState);
    let initialContentString = JSON.stringify(this.initialContentState);

    if (newContentString !== initialContentString) {
      var value = this.editorState.isEmpty()
        ? null
        : this.converter.$convertToText(this.editor);
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
