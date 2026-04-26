import { $createCodeNode, $isCodeNode } from "@lexical/code";
import { $isLinkNode, TOGGLE_LINK_COMMAND } from "@lexical/link";
import { $createListNode } from "@lexical/list";
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
  listTag: string
): void {
  editor.update(() => {
    const selection = $getSelection();

    if (!$isRangeSelection(selection)) return;

    const listType = listTag === "ul" ? "bullet" : "number";

    const active = isListActive(selection, listTag);

    const anchorNode = selection.anchor.getNode();
    if(!anchorNode.getTextContent() && !active) return;

    $setBlocksType(selection, () =>
      active ? $createParagraphNode() : $createListNode(listType)
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

    const active = isHeadingActive(selection, headingTagType);

    $setBlocksType(selection, () =>
      active ? $createParagraphNode() : $createHeadingNode(headingTagType)
    );
  });
}

export function formatQuote(editor: LexicalEditor): void {
  editor.update(() => {
    const selection = $getSelection();

    if (!$isRangeSelection(selection)) return;

    const active = !!$findMatchingParent(selection.anchor.getNode(), node => $isQuoteNode(node));

    $setBlocksType(selection, () =>
      active ? $createParagraphNode() : $createQuoteNode()
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

    const active = !!$findMatchingParent(selection.anchor.getNode(), node => $isCodeNode(node));

    $setBlocksType(selection, () =>
      active ? $createParagraphNode() : $createCodeNode(language)
    );
  });
}

export function formatLink(editor: LexicalEditor, url?: string): void {
  editor.update(() => {
    const selection = $getSelection();

    if (!$isRangeSelection(selection)) return;
    
    const anchorNode = selection.anchor.getNode();
    const text = selection.getTextContent();

    const active = !!$findMatchingParent(anchorNode, node => $isLinkNode(node));

    if(!text && url) {
      selection.insertText(url);
    }

    editor.dispatchCommand(TOGGLE_LINK_COMMAND, active ? null : (url ?? null));
  });
}
