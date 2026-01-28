import { COMMAND_PRIORITY_NORMAL, KEY_DOWN_COMMAND } from "lexical";
import { HtmlEditorController } from "../HtmlEditorController";
import { HtmlEditorExtension } from "./types";

export class BasicCommandsExtensions extends HtmlEditorExtension {
override name = "BasicCommandsExtensions";

override registerExtension(controller: HtmlEditorController): () => void {
    return controller.editor.registerCommand(
      KEY_DOWN_COMMAND,
      (event) => {
        if (event.ctrlKey && event.key === "s") {
          controller.saveHtml();
          return true;
        }

        return false;
      },
      COMMAND_PRIORITY_NORMAL
    );
  }
}
