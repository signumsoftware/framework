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
