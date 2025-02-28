import React from "react";
import { BlockStyleButton, HtmlEditorButton, InlineStyleButton } from "../HtmlEditorButtons";
import { HtmlEditorController } from "../HtmlEditorController";
import { validateUrl } from "../Utilities/url";
import { ComponentAndProps, HtmlEditorExtension, LexicalConfigNode } from "./types";
import { LinkPlugin } from "@lexical/react/LexicalLinkPlugin"
import { $getSelection, $isRangeSelection, RangeSelection } from "lexical";
import { isNodeType } from "../Utilities/node";
import { $isLinkNode, LinkNode } from "@lexical/link";
import { formatLink } from "../Utilities/format";

export class LinkExtension implements HtmlEditorExtension {
  getToolbarButtons(controller: HtmlEditorController): React.ReactNode {
      return <LinkButton controller={controller} />
  }

  getBuiltInComponent(): ComponentAndProps<typeof LinkPlugin> {
      return { component: LinkPlugin, props: { attributes: { rel: "noopener noreferrer", target: "_blank" }, validateUrl: validateUrl } }
  }

  getNodes(): LexicalConfigNode {
      return [LinkNode]
  }
}

type LinkButtonProps = {
  controller: HtmlEditorController;
  // onClick: () => void;
  // getIsActive: (selection: RangeSelection) => boolean;
}

export function LinkButton({ controller }: LinkButtonProps): React.JSX.Element {
  const { editorState } = controller;
 const isActive = React.useMemo(() => {
    let active = false;

    editorState?.read(() => {
      const selection = $getSelection();
      if(!$isRangeSelection(selection)) return;
      
      active = isNodeType(selection, node => $isLinkNode(node))
    })

    return active;
 }, []);

 

 return <HtmlEditorButton isActive={isActive} onClick={() => formatLink(controller.editor)} icon="link" title="Insert link" />
}
