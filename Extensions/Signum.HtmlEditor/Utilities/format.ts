import { $createCodeNode, $isCodeNode } from "@lexical/code";
import {
  INSERT_UNORDERED_LIST_COMMAND,
  INSERT_ORDERED_LIST_COMMAND,
  REMOVE_LIST_COMMAND,
  $createListNode,
  ListType,
  $isListNode,
  $isListItemNode,
} from "@lexical/list";
import {
  $createHeadingNode,
  $createQuoteNode,
  $isHeadingNode,
  $isQuoteNode,
  HeadingTagType,
} from "@lexical/rich-text";
import { $setBlocksType } from "@lexical/selection";
import {
  $createParagraphNode,
  $getSelection,
  $isRangeSelection,
  LexicalCommand,
  LexicalEditor,
} from "lexical";
import { isNodeType } from "./node";

export function formatList(editor: LexicalEditor, listTag: "ul" | "ol"): void {
  editor.update(() => {
    const selection = $getSelection();

    if (!$isRangeSelection(selection)) return;

    const listType = listTag === "ul" ? "bullet" : "number";
    const revert = isNodeType(selection, (node) => {
      if ($isListItemNode(node)) {
        const parentNode = node.getParent();
        return $isListNode(parentNode) && parentNode.getTag() === listTag;
      }

      return $isListNode(node) && node.getTag() === listTag;
    });

    $setBlocksType(selection, () =>
      revert ? $createParagraphNode() : $createListNode(listType)
    );
  });
}

export function formatHeading(
  editor: LexicalEditor,
  headingTagType: HeadingTagType
): void {
  editor.update(() => {
    const selection = $getSelection();

    if (!$isRangeSelection(selection)) return;

    const revert = isNodeType(
      selection,
      (node) => $isHeadingNode(node) && node.getTag() === headingTagType
    );

    $setBlocksType(selection, () =>
      revert ? $createParagraphNode() : $createHeadingNode(headingTagType)
    );
  });
}

export function formatQuote(editor: LexicalEditor): void {
  editor.update(() => {
    const selection = $getSelection();

    if (!$isRangeSelection(selection)) return;

    const revert = isNodeType(selection, (node) => $isQuoteNode(node));

    $setBlocksType(selection, () =>
      revert ? $createParagraphNode() : $createQuoteNode()
    );
  });
}
