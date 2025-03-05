import { $createCodeNode, $isCodeNode } from "@lexical/code";
import { $isLinkNode, TOGGLE_LINK_COMMAND } from "@lexical/link";
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
import { $findMatchingParent, isHeadingActive, isListActive } from "./node";

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

    const revert = !!$findMatchingParent(selection.anchor.getNode(), node => $isQuoteNode(node));

    $setBlocksType(selection, () =>
      revert ? $createParagraphNode() : $createQuoteNode()
    );
  });
}

export function formatCode(
  editor: LexicalEditor,
  language: string = "javascript"
): void {
  editor.update(() => {
    const selection = $getSelection();

    if (!$isRangeSelection(selection)) return;

    const revert = !!$findMatchingParent(selection.anchor.getNode(), node => $isCodeNode(node));

    $setBlocksType(selection, () =>
      revert ? $createParagraphNode() : $createCodeNode(language)
    );
  });
}

export function formatLink(editor: LexicalEditor, url?: string): void {
  editor.update(() => {
    const selection = $getSelection();

    if (!$isRangeSelection(selection)) return;

    const revert = !!$findMatchingParent(selection.anchor.getNode(), node => $isLinkNode(node));

    editor.dispatchCommand(TOGGLE_LINK_COMMAND, revert ? null : url || null);
  });
}
