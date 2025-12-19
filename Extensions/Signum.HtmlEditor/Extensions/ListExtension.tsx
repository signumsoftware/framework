import {
  $isListItemNode,
  INSERT_ORDERED_LIST_COMMAND,
  INSERT_UNORDERED_LIST_COMMAND,
  ListItemNode,
  ListNode,
} from "@lexical/list";
import { ListPlugin } from "@lexical/react/LexicalListPlugin";
import {
  $getSelection,
  $isRangeSelection,
  COMMAND_PRIORITY_LOW,
  INDENT_CONTENT_COMMAND,
  KEY_SPACE_COMMAND,
  KEY_TAB_COMMAND,
  OUTDENT_CONTENT_COMMAND,
} from "lexical";
import { HtmlEditorController } from "../HtmlEditorController";
import { $findMatchingParent, isListActive } from "../Utils/node";
import {
  HtmlEditorExtension,
  LexicalConfigNode,
  OptionalCallback,
} from "./types";

const MAX_INDENT_LEVEL = 6;

export class ListExtension implements HtmlEditorExtension {
  name = "ListExtension";

  getBuiltPlugin(): React.ReactElement {
    return <ListPlugin />;
  }

  getNodes(): LexicalConfigNode {
    return [ListNode, ListItemNode];
  }

  registerExtension(controller: HtmlEditorController): OptionalCallback {
    const unsubscribeSpaceCommand = controller.editor.registerCommand(
      KEY_SPACE_COMMAND,
      () => {
        const selection = $getSelection();

        if (!$isRangeSelection(selection) || !selection.isCollapsed())
          return false;
        const anchorNode = selection.anchor.getNode();
        const text = anchorNode.getTextContent();

        let command = null;

        if (text === "*" || text === "-") {
          command = INSERT_UNORDERED_LIST_COMMAND;
        } else if (text === "1.") {
          command = INSERT_ORDERED_LIST_COMMAND;
        }

        if (!command) return false;

        controller.editor.update(() => {
          anchorNode.remove();
          controller.editor.dispatchCommand(command, undefined);
        });
        return true;
      },
      COMMAND_PRIORITY_LOW
    );

    const unsubscribeTabCommand = controller.editor.registerCommand(
      KEY_TAB_COMMAND,
      (event) => {
        let handled = false;
        const selection = $getSelection();
        if (!$isRangeSelection(selection)) return handled;

        if (isListActive(selection)) {
          event.preventDefault();

          const anchorNode = selection.anchor.getNode();
          const listItemNode = $findMatchingParent(anchorNode, (node) =>
            $isListItemNode(node)
          ) as ListItemNode | undefined;
          const depth = listItemNode?.getIndent() || 0;
          if (!event.shiftKey) {
            if (depth >= MAX_INDENT_LEVEL) return false;
            controller.editor.dispatchCommand(
              INDENT_CONTENT_COMMAND,
              undefined
            );
          } else {
            controller.editor.dispatchCommand(
              OUTDENT_CONTENT_COMMAND,
              undefined
            );
          }

          handled = true;
        }

        return handled;
      },
      COMMAND_PRIORITY_LOW
    );

    return () => {
      unsubscribeTabCommand();
      unsubscribeSpaceCommand();
    };
  }
}
