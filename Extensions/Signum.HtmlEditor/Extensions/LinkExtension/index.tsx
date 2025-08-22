import { $isLinkNode, LinkNode } from "@lexical/link";
import { LinkPlugin } from "@lexical/react/LexicalLinkPlugin";
import {
  $getSelection,
  $isRangeSelection,
  CLICK_COMMAND,
  COMMAND_PRIORITY_EDITOR,
} from "lexical";
import React from "react";
import { HtmlEditorController } from "../../HtmlEditorController";
import { $findMatchingParent } from "../../Utils/node";
import {
  ComponentAndProps,
  HtmlEditorExtension,
  LexicalConfigNode,
  OptionalCallback,
} from "../types";
import { AutoLinkExtension } from "./AutoLinkExtension";
import ToolbarLinkButton from "./ToolbarLinkButton";
import { validateUrl } from "./helper";

export class LinkExtension implements HtmlEditorExtension {
  name = "LinkExtension";

  getToolbarButtons(controller: HtmlEditorController): React.ReactNode {
    return <ToolbarLinkButton controller={controller} />;
  }

  getBuiltInComponent(): ComponentAndProps<typeof LinkPlugin> {
    return {
      component: LinkPlugin,
      props: { attributes: { target: "_blank" }, validateUrl: validateUrl },
    };
  }

  getNodes(): LexicalConfigNode {
    return [LinkNode];
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

export { AutoLinkExtension };
