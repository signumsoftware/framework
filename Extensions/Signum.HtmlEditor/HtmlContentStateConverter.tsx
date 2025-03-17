import { $generateHtmlFromNodes, $generateNodesFromDOM } from "@lexical/html";
import { $getRoot, $getSelection, EditorState, LexicalEditor } from "lexical";

export interface ITextConverter {
  $convertToText(editor: LexicalEditor): string;
  $convertFromText(editor: LexicalEditor, html: string): EditorState;
}

export class HtmlContentStateConverter implements ITextConverter {
  $convertToText(
    editor: LexicalEditor
  ): ReturnType<ITextConverter["$convertToText"]> {
    return editor.read(() => $generateHtmlFromNodes(editor));
  }
  
  $convertFromText(
    editor: LexicalEditor,
    html: string
  ): ReturnType<ITextConverter["$convertFromText"]> {

    editor.update(() => {
      const parser = new DOMParser();
      const doc = parser.parseFromString(html, "text/html");

      createImagePlaceholders(doc);
      let nodes = $generateNodesFromDOM(editor, doc);

      $getRoot().clear().select();
      $getSelection()?.insertNodes(nodes);
    }, { discrete: true })

    return editor.getEditorState();
  }
}

function createImagePlaceholders(doc: Document) {
  const imgElements = doc.querySelectorAll("img");
  for(let i = 0; i < imgElements.length; i++) {
    const img = imgElements[i];
    const attachmentId = img.getAttribute("data-attachment-id");
    if(!attachmentId) continue;

    const placeholderText = `[IMAGE_${attachmentId}]`;
    img.replaceWith(document.createTextNode(placeholderText));
  }
}
