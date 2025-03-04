import {
  ListItemNode,
  ListNode
} from "@lexical/list";
import { ListPlugin } from "@lexical/react/LexicalListPlugin";
import { $getSelection, $isRangeSelection, COMMAND_PRIORITY_LOW, INDENT_CONTENT_COMMAND, KEY_TAB_COMMAND, OUTDENT_CONTENT_COMMAND } from "lexical";
import { HtmlEditorController } from "../HtmlEditorController";
import { isListActive } from "../Utils/node";
import {
  ComponentAndProps,
  HtmlEditorExtension,
  LexicalConfigNode,
  OptionalCallback
} from "./types";

export class ListExtension implements HtmlEditorExtension {
  getBuiltInComponent(): ComponentAndProps {
    return { component: ListPlugin };
  }

  getNodes(): LexicalConfigNode {
    return [ListNode, ListItemNode];
  }

  registerExtension(controller: HtmlEditorController): OptionalCallback {
    return controller.editor.registerCommand(KEY_TAB_COMMAND, (event) => {
      let handled = false;
      const selection = $getSelection();
      if(!$isRangeSelection(selection)) return handled;

      if(isListActive(selection)) {
        event.preventDefault();

        if(!event.shiftKey) {
          controller.editor.dispatchCommand(INDENT_CONTENT_COMMAND, undefined);
        } else {
          controller.editor.dispatchCommand(OUTDENT_CONTENT_COMMAND, undefined);
        }

        handled = true;
      }

      return handled;
    }, COMMAND_PRIORITY_LOW);
  }
}
