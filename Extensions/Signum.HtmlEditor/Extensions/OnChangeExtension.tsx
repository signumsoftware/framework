import { EditorState } from "lexical";
import { HtmlEditorController } from "../HtmlEditorController";
import { HtmlEditorExtension, OptionalCallback } from "./types";

type OnChangeCallback = (editorState?: EditorState) => void;
type OnChangeExtensionProps = { onChange?: OnChangeCallback };

export class OnChangeExtension extends HtmlEditorExtension {
override name = "OnChangeExtension";

props: OnChangeExtensionProps;

constructor(onChange?: OnChangeCallback) {
  super();
  this.props = { onChange };
}

override registerExtension(controller: HtmlEditorController): OptionalCallback {
    if (!controller.editor) return;

    return controller.editor.registerUpdateListener(({ editorState }) => {
      this.props.onChange?.();
    });
  }
}
