import { $getRoot, EditorState } from "lexical";

export function isEmpty(editorState: EditorState | undefined): boolean {
  return editorState?.read(() => {
   return $getRoot().getTextContentSize() === 0;
  }) ?? false;

}
