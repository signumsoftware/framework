import { EditorState } from "lexical";
import { HtmlEditorController } from "../HtmlEditorController";
import { HtmlEditorExtension, OptionalCallback } from "./types";

type OnChangeCallback = (editorState?: EditorState) => void
type OnChangeExtensionProps = { onChange?: OnChangeCallback }


export class OnChangeExtension implements HtmlEditorExtension {
    props: OnChangeExtensionProps;

    constructor(onChange?: OnChangeCallback) {
        this.props = { onChange };
    }

    registerExtension(controller: HtmlEditorController): OptionalCallback {
        if(!controller.editor) return;

        const unsubscribe = controller.editor.registerUpdateListener(({editorState}) => {
            controller.setEditorState(editorState);
            this.props.onChange?.();
        });
        
        return unsubscribe;
    }
}
