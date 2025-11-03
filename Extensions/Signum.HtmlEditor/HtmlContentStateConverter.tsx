import { $generateHtmlFromNodes, $generateNodesFromDOM } from "@lexical/html";
import { $getRoot, $getSelection, $setSelection, EditorState, LexicalEditor } from "lexical";

export interface ITextConverter {
  $convertToText(editor: LexicalEditor): string;
  $convertFromText(editor: LexicalEditor, html: string): EditorState;
}

export class HtmlContentStateConverter implements ITextConverter {

  constructor(public dataImageIdAttribute: string | undefined) /*"data-attachment-id"*/ {

  }

  $convertToText(editor: LexicalEditor): string {
    return editor.read(() => fixListHTML($generateHtmlFromNodes(editor)));
  }

  $convertFromText(editor: LexicalEditor, html: string): EditorState {

    editor.update(() => {
      const parser = new DOMParser();
      const doc = parser.parseFromString(html, "text/html");

      const nodes = $generateNodesFromDOM(editor, doc);
      $getRoot().clear().select();
      $getSelection()?.insertNodes(nodes);
      $setSelection(null);
    }, { discrete: true })

    return editor.getEditorState();
  }
}

function fixListHTML(html: string): string {
  // Remove `value="..."` from OL elements
  html = html.replace(/\svalue="\d+"/g, '');

  const container = document.createElement('div');
  container.innerHTML = html;

  function fixBrokenNestedLists(element: HTMLElement): void {
    const listTags = ['ol', 'ul'];

    for (const tag of listTags) {
      const listElements = Array.from(element.querySelectorAll(tag));

      for (const list of listElements) {
        const children = Array.from(list.children);

        for (let i = 0; i < children.length; i++) {
          const child = children[i];

          if (
            child.tagName === 'LI' &&
            child.children.length === 1 &&
            (child.firstElementChild?.tagName === 'OL' || child.firstElementChild?.tagName === 'UL')
          ) {
            const prevLi = child.previousElementSibling;

            if (prevLi?.tagName === 'LI') {
              const nestedList = child.firstElementChild as HTMLOListElement | HTMLUListElement;
              prevLi.appendChild(nestedList);
              child.remove();
              i--;
            }
          }
        }
      }
    }
  }

  fixBrokenNestedLists(container);

  return container.innerHTML;
}


