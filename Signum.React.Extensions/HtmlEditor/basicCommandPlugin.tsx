import * as draftjs from 'draft-js';
import { IContentStateConverter, HtmlEditorController } from "./HtmlEditor"

export function basicCommandsPlugin(props: draftjs.EditorProps, controller: HtmlEditorController) {

  var prevKeyCommand = props.handleKeyCommand;
  props.handleKeyCommand = (command, state, timeStamp) => {

    if (prevKeyCommand) {
      var result = prevKeyCommand(command, state, timeStamp);
      if (result == "handled")
        return result;
    }

    const inlineStyle =
      command == "bold" ? "BOLD" :
        command == "italic" ? "ITALIC" :
          command == "underline" ? "UNDERLINE" :
            undefined;

    if (inlineStyle) {
      controller.setEditorState(draftjs.RichUtils.toggleInlineStyle(controller.editorState, inlineStyle));
      return "handled";
    }

    return "not-handled";
  }

  return props
}

