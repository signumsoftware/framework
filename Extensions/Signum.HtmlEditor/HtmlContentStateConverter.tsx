import { $generateHtmlFromNodes, $generateNodesFromDOM } from "@lexical/html";
import { $getRoot, $getSelection, $setSelection, EditorState, LexicalEditor } from "lexical";

export interface ITextConverter {
  $convertToText(editor: LexicalEditor): string;
  $convertFromText(editor: LexicalEditor, html: string): EditorState;
}

export class HtmlContentStateConverter implements ITextConverter {
  $convertToText(
    editor: LexicalEditor
  ): ReturnType<ITextConverter["$convertToText"]> {
    return editor.read(() => fixListHTML($generateHtmlFromNodes(editor)));
  }

  $convertFromText(
    editor: LexicalEditor,
    html: string
  ): ReturnType<ITextConverter["$convertFromText"]> {

    editor.update(() => {
      const parser = new DOMParser();
      const doc = parser.parseFromString(html, "text/html");

      createImagePlaceholders(doc);
      const nodes = $generateNodesFromDOM(editor, doc);
      $getRoot().clear().select();
      $getSelection()?.insertNodes(nodes);
      $setSelection(null);
    }, { discrete: true })

    return editor.getEditorState();
  }
}

function createImagePlaceholders(doc: Document) {
  const imgElements = doc.querySelectorAll("img");
  for (let i = 0; i < imgElements.length; i++) {
    const img = imgElements[i];
    const attachmentId = img.getAttribute("data-attachment-id");
    if (!attachmentId) continue;

    const placeholderText = `[IMAGE_${attachmentId}]`;
    img.replaceWith(document.createTextNode(placeholderText));
  }
}

function fixListHTML(html: string): string {
  html = html.replace(/\svalue="\d+"/g, '');

  const container = document.createElement('div');
  container.innerHTML = html;

  function fixBrokenNestedLists(element: HTMLElement): void {
    const olElements = Array.from(element.querySelectorAll('ol'));

    for (const ol of olElements) {
      const children = Array.from(ol.children);

      for (let i = 0; i < children.length; i++) {
        const child = children[i];

        if (
          child.tagName === 'LI' &&
          child.children.length === 1 &&
          child.firstElementChild?.tagName === 'OL'
        ) {
          const prevLi = child.previousElementSibling;

          if (prevLi?.tagName === 'LI') {
            const nestedOl = child.firstElementChild as HTMLOListElement;
            prevLi.appendChild(nestedOl);
            child.remove();
            i--;
          }
        }
      }
    }
  }

  fixBrokenNestedLists(container);

  return container.innerHTML;
}


