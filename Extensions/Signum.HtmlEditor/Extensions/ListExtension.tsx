import {
  ListItemNode,
  ListNode
} from "@lexical/list";
import { ListPlugin } from "@lexical/react/LexicalListPlugin";
import {
  ComponentAndProps,
  LexicalConfigNode,
  HtmlEditorExtension
} from "./types";

export class ListExtension implements HtmlEditorExtension {
  getBuiltInComponent(): ComponentAndProps {
    return { component: ListPlugin };
  }

  getNodes(): LexicalConfigNode {
    return [ListNode, ListItemNode];
  }
}
