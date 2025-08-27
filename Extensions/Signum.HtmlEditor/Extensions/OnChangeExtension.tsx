import { EditorState } from "lexical";
import { HtmlEditorController } from "../HtmlEditorController";
import { HtmlEditorExtension, OptionalCallback } from "./types";

type OnChangeCallback = (editorState?: EditorState) => void;
type OnChangeExtensionProps = { onChange?: OnChangeCallback };

export class OnChangeExtension implements HtmlEditorExtension {
  name = "OnChangeExtension";

  props: OnChangeExtensionProps;

  constructor(onChange?: OnChangeCallback) {
    this.props = { onChange };
  }

  registerExtension(controller: HtmlEditorController): OptionalCallback {
    if (!controller.editor) return;

    return controller.editor.registerUpdateListener(({ editorState }) => {
      this.props.onChange?.();
    });
  }
}
