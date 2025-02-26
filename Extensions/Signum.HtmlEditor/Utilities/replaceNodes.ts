import {
  LexicalEditor,
  ElementNode,
  $getSelection,
  $isRangeSelection,
} from "lexical";

/**
 * Replaces the editor's node of the selection.
 * @param editor Instance of the active editor.
 * @param replaceWith Callback that returns an element to replace the selection's node with.
 */
export function replaceNodes(
  editor: LexicalEditor,
  replaceWith: (node: ElementNode) => ElementNode | undefined
): void {
  editor.update(() => {
    const selection = $getSelection();
    if (!$isRangeSelection(selection)) return;
    const nodes = selection.getNodes();
    for (const node of nodes) {
      const parent = node.getParent();
      if (!parent) continue;
      const replacementNode = replaceWith(parent);

      if (replacementNode) {
        parent.getChildren().forEach((child) => replacementNode.append(child));
        parent.replace(replacementNode);
      }
    }
  });
}
