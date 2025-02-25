import { $generateHtmlFromNodes, $generateNodesFromDOM } from "@lexical/html";
import { $convertFromMarkdownString } from "@lexical/markdown";
import { $getEditor, EditorState, LexicalEditor, LexicalNode } from "lexical";

export interface ITextConverter {
  $convertToText(editor: LexicalEditor): string;
  $convertFromText(editor: LexicalEditor, html: string): EditorState;
}

export class HtmlContentStateConverter implements ITextConverter {
  $convertToText(
    editor: LexicalEditor
  ): ReturnType<ITextConverter["$convertToText"]> {
    let htmlString = "";
    editor.read(() => {
      htmlString = $generateHtmlFromNodes($getEditor());
    });
    return htmlString;
  }
  $convertFromText(
    editor: LexicalEditor,
    html: string
  ): ReturnType<ITextConverter["$convertFromText"]> {
    return editor.parseEditorState(html);
  }
}
