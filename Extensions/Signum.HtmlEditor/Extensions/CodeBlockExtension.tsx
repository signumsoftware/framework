import { CodeHighlightNode, CodeNode, registerCodeHighlighting } from "@lexical/code";
import { HtmlEditorController } from "../HtmlEditorController";
import {
  HtmlEditorExtension,
  LexicalConfigNode,
  OptionalCallback
} from "./types";

export class CodeBlockExtension implements HtmlEditorExtension {
  registerExtension(controller: HtmlEditorController): OptionalCallback {
      const unsubscribe = registerCodeHighlighting(controller.editor);
      return unsubscribe;
  }

  getNodes(): LexicalConfigNode {
      return [CodeNode, CodeHighlightNode]
  }
}
