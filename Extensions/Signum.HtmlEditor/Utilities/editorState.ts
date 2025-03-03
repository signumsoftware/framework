import { $getRoot, EditorState } from "lexical";

export function isEmpty(editorState: EditorState | undefined): boolean {
  let isEmpty = false;

  editorState?.read(() => {
    isEmpty = $getRoot().getTextContentSize() === 0;
  });

  return isEmpty
}
