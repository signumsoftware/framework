import * as draftjs from 'draft-js';
import { HtmlEditorPlugin, HtmlEditorController } from '../HtmlEditor';

export default class BasicCommandsPlugin implements HtmlEditorPlugin {

  expandEditorProps?(props: draftjs.EditorProps, controller: HtmlEditorController): draftjs.EditorProps {
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

}

