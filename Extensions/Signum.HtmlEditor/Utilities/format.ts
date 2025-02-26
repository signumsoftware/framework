import { $createCodeNode, $isCodeNode } from "@lexical/code";
import { $createListNode } from "@lexical/list";
import { ListNodeTagType } from "@lexical/list/LexicalListNode";
import {
  $createHeadingNode,
  $createQuoteNode,
  $isQuoteNode,
  HeadingTagType,
} from "@lexical/rich-text";
import { $setBlocksType } from "@lexical/selection";
import {
  $createParagraphNode,
  $getSelection,
  $isRangeSelection,
  LexicalEditor,
} from "lexical";
import { isHeadingActive, isListActive, isNodeType } from "./node";

export function formatList(
  editor: LexicalEditor,
  listTag: ListNodeTagType
): void {
  editor.update(() => {
    const selection = $getSelection();

    if (!$isRangeSelection(selection)) return;

    const listType = listTag === "ul" ? "bullet" : "number";
    const revert = isListActive(selection, listTag);

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

    const revert = isHeadingActive(selection, headingTagType);

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

export function formatCode(editor: LexicalEditor): void {
  editor.update(() => {
    const selection = $getSelection();

    if (!$isRangeSelection(selection)) return;

    const revert = isNodeType(selection, (node) => $isCodeNode(node));

    $setBlocksType(selection, () =>
      revert ? $createParagraphNode() : $createCodeNode()
    );
  });
}
