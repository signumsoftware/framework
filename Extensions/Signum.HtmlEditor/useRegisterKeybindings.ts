import { COMMAND_PRIORITY_NORMAL, KEY_DOWN_COMMAND } from "lexical";
import { HtmlEditorController } from "./HtmlEditorController";
import { useEffect } from 'react';

export const useRegisterKeybindings = (controller: HtmlEditorController, keybindingFn?: (event: KeyboardEvent) => boolean): void => {
  useEffect(() => {
    if (!controller?.editor || !keybindingFn) return;

    return controller.editor.registerCommand(KEY_DOWN_COMMAND, keybindingFn, COMMAND_PRIORITY_NORMAL);
  }, [controller.editor, keybindingFn])
}
