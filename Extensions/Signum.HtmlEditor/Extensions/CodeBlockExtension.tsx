import {
  CodeHighlightNode,
  CodeNode,
  registerCodeHighlighting,
} from "@lexical/code";
import { HtmlEditorController } from "../HtmlEditorController";
import {
  HtmlEditorExtension,
  LexicalConfigNode,
  OptionalCallback,
} from "./types";

export class CodeBlockExtension implements HtmlEditorExtension {
  name = "CodeBlockExtension";

  registerExtension(controller: HtmlEditorController): OptionalCallback {
    return registerCodeHighlighting(controller.editor);
  }

  getNodes(): LexicalConfigNode {
    return [CodeNode, CodeHighlightNode];
  }
}
