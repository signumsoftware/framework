import { $isListNode, ListNode, ListType } from "@lexical/list";
import { $isHeadingNode, HeadingNode, HeadingTagType } from "@lexical/rich-text";
import { ElementNode, RangeSelection, TextNode } from "lexical";

/**
 * Traverses the lexical editor state to determine the current selection's list type.
 */
export function getListType (selection: RangeSelection): ListType | null  {
    let anchorNode: ElementNode | TextNode | null = selection.anchor.getNode();
    let listType = null
    
    while(anchorNode) {
        if($isListNode(anchorNode)) {
            listType = (anchorNode as ListNode).getListType();
            break;
        }

        anchorNode = anchorNode.getParent();
    }

    return listType;
}

export function getHeadingTag(selection: RangeSelection): HeadingTagType | null {
    let anchorNode: ElementNode | TextNode | null = selection.anchor.getNode();
    let tagType = null;

    while(anchorNode) {
        if($isHeadingNode(anchorNode)) {
            tagType = (anchorNode as HeadingNode).getTag();
            break;
        }

        anchorNode = anchorNode.getParent();
    }

    return tagType;
}