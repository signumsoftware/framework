import { $isLinkNode, AutoLinkNode, LinkNode } from "@lexical/link";
import {
  AutoLinkPlugin,
  LinkMatcher,
} from "@lexical/react/LexicalAutoLinkPlugin";
import {
  $getSelection,
  $isRangeSelection,
  CLICK_COMMAND,
  COMMAND_PRIORITY_EDITOR,
} from "lexical";
import { HtmlEditorController } from "../../HtmlEditorController";
import { $findMatchingParent } from "../../Utils/node";
import {
  HtmlEditorExtension,
  LexicalConfigNode,
  OptionalCallback,
} from "../types";
import { urlRegExp } from "./helper";

const MATCHERS: LinkMatcher[] = [
  (text: string) => {
    const match = urlRegExp.exec(text);

    if (match === null) return null;

    const [fullMatch] = match;

    return {
      index: match.index,
      length: fullMatch.length,
      text: fullMatch,
      url: fullMatch.startsWith("http") ? fullMatch : `https://${fullMatch}`,
    };
  },
];

export class AutoLinkExtension implements HtmlEditorExtension {
  name = "AutoLinkExtension";

  getBuiltPlugin(): JSX.Element {
    return <AutoLinkPlugin matchers={MATCHERS} />;
  }

  getNodes(): LexicalConfigNode {
    return [AutoLinkNode];
  }

  registerExtension(controller: HtmlEditorController): OptionalCallback {
    return controller.editor.registerCommand(
      CLICK_COMMAND,
      (event) => {
        if (!event.ctrlKey) return false;
        const selection = $getSelection();
        if (!$isRangeSelection(selection)) return false;
        const linkNode = $findMatchingParent(
          selection.anchor.getNode(),
          (node) => $isLinkNode(node)
        );

        if (linkNode) {
          window.open((linkNode as LinkNode).getURL(), "_blank");
          return true;
        }
        return false;
      },
      COMMAND_PRIORITY_EDITOR
    );
  }
}
