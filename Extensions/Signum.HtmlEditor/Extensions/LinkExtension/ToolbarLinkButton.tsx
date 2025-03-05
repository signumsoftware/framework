import AutoLineModal from "@framework/AutoLineModal";
import { $isLinkNode, LinkNode } from "@lexical/link";
import { $getSelection, $isRangeSelection, RangeSelection } from "lexical";
import React, { useCallback } from "react";
import { HtmlEditorButton } from "../../HtmlEditorButtons";
import { HtmlEditorController } from "../../HtmlEditorController";
import { formatLink } from "../../Utils/format";
import { $findMatchingParent, isNodeType } from "../../Utils/node";
import EditLinkField from "./EditLinkField";
import { restoreSelection, sanitizeUrl, validateUrl } from "./helper";

type LinkButtonProps = { controller: HtmlEditorController; }

export default function LinkButton({ controller }: LinkButtonProps): React.ReactNode {
  const { editor, editorState } = controller;

  const isActive = React.useMemo(() => {
    let active = false;

    editorState?.read(() => {
      const selection = $getSelection();
      if(!$isRangeSelection(selection)) return;
      
      active = isNodeType(selection, node => $isLinkNode(node))
    })

    return active;
 }, [editorState]);

 const toggleLink = useCallback(async () => {
  let selection: RangeSelection | undefined
  let initialUrl = ""
  editor.read(()=> {
    const currentSelection = $getSelection();
    if(!$isRangeSelection(currentSelection)) return
    selection = currentSelection;
    const linkNode = $findMatchingParent(selection.anchor.getNode(), node => $isLinkNode(node)) as LinkNode | undefined;
    if(linkNode) {
      initialUrl = linkNode.getURL();
    }
  });

  if(!selection) return;
  
  const url = await AutoLineModal.show({ title: "Insert hyperlink", message: "", initialValue: initialUrl, allowEmptyValue: true, customComponent: p => <EditLinkField {...p} />})

  if(!url) {
    formatLink(editor);
    return;
  }

  const sanitizedUrl = sanitizeUrl(url)

  if(!validateUrl(sanitizedUrl)) {
    throw new Error("The entered URL is not valid.");
  }

  editor.update(() => {
    if(!selection) return
    restoreSelection(editor, selection);
    const linkNode = $findMatchingParent(selection.anchor.getNode(), node => $isLinkNode(node)) as LinkNode | undefined;
    if(linkNode && url) {
      linkNode.setURL(url);
    } else {
      formatLink(editor, sanitizedUrl)
    }
  })

 }, [editor])

 return <HtmlEditorButton isActive={isActive} onClick={toggleLink} icon="link" title="Insert hyperlink" />
}
