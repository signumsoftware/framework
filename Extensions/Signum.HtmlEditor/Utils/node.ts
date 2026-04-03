import { $isListNode, ListNode } from "@lexical/list";
import { $isHeadingNode, $isQuoteNode } from "@lexical/rich-text";
import { LexicalNode, RangeSelection } from "lexical";

/**
 * Can only be used within register/read/update callback of the editor.
 */
export function $findMatchingParent(node: LexicalNode, selector: (node: LexicalNode) => boolean): LexicalNode | undefined {
  if(selector(node)) return node;

  const parentNode = node.getParent();
  if(!parentNode) return;

  return $findMatchingParent(parentNode, selector);
}

export function isListActive(
  selection: RangeSelection,
  listTag?: string
): boolean {
  const verifyListTag = (node: ListNode) => !listTag ? true : node.getTag() === listTag
  return !!$findMatchingParent(selection.anchor.getNode(), node => $isListNode(node) && verifyListTag(node))
}

export function isQuoteActive(
  selection: RangeSelection,
  blockType: string
): boolean {
  return !!$findMatchingParent(selection.anchor.getNode(), node => $isQuoteNode(node) && blockType === "blockquote");
}

export function isHeadingActive(
  selection: RangeSelection,
  headingTag: string
): boolean {
  return !!$findMatchingParent(selection.anchor.getNode(), node => $isHeadingNode(node) && node.getTag() === headingTag);
}


