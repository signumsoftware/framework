import { $isListItemNode, $isListNode, ListNode } from "@lexical/list";
import { $isHeadingNode, $isQuoteNode } from "@lexical/rich-text";
import { ElementNode, RangeSelection } from "lexical";

export function isNodeType(
  selection: RangeSelection,
  checkNodeType: (node: ElementNode | null) => boolean
): boolean {
  return selection.getNodes().some((node) => {
    const parent = node.getParent();
    return checkNodeType(parent);
  });
}

export function isListActive(
  selection: RangeSelection,
  listTag?: string
): boolean {
  const verifyListTag = (node: ListNode) => !listTag ? true : node.getTag() === listTag

  return isNodeType(selection, (node) => {
    if ($isListItemNode(node)) {
      const parentNode = node.getParent();
      return $isListNode(parentNode) && verifyListTag(parentNode);
    }

    return $isListNode(node) && verifyListTag(node);
  });
}

export function isQuoteActive(
  selection: RangeSelection,
  blockType: string
): boolean {
  return isNodeType(
    selection,
    (node) => $isQuoteNode(node) && blockType === "blockquote"
  );
}

export function isHeadingActive(
  selection: RangeSelection,
  headingTag: string
): boolean {
  return isNodeType(
    selection,
    (node) => $isHeadingNode(node) && node.getTag() === headingTag
  );
}
