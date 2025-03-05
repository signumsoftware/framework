import { $generateHtmlFromNodes, $generateNodesFromDOM } from "@lexical/html";
import { $getEditor, $getRoot, $getSelection, EditorState, LexicalEditor } from "lexical";

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

    editor.update(() => {
      const parser = new DOMParser();
      const doc = parser.parseFromString(html, "text/html");
      const nodes = $generateNodesFromDOM(editor, doc)
      $getRoot().clear().select();
      $getSelection()?.insertNodes(nodes);
    }, { discrete: true })

    return editor.getEditorState();
  }
}
