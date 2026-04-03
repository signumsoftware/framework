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

export class CodeBlockExtension extends HtmlEditorExtension {
  override name = "CodeBlockExtension";

  override registerExtension(controller: HtmlEditorController): OptionalCallback {
    return registerCodeHighlighting(controller.editor);
  }

  override getNodes(): LexicalConfigNode {
    return [CodeNode, CodeHighlightNode];
  }
}
