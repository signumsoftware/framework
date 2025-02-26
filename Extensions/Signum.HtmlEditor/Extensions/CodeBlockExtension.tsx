import { $createCodeNode, $isCodeNode, CodeHighlightNode, CodeNode, registerCodeHighlighting } from "@lexical/code";
import { $setBlocksType } from "@lexical/selection";
import { $createParagraphNode, $getSelection, $isRangeSelection, LexicalEditor, TextNode } from "lexical";
import React from "react";
import { BlockStyleButton } from "../HtmlEditorButtons";
import { HtmlEditorController } from "../HtmlEditorController";
import { isNodeType } from "../Utilities/node";
import {
  HtmlEditorExtension,
  LexicalConfigNode,
  OptionalCallback
} from "./types";

export class CodeBlockExtension implements HtmlEditorExtension {
  getToolbarButtons(controller: HtmlEditorController): React.ReactNode {
      return (
        <BlockStyleButton 
          controller={controller} 
          blockType="code-block" 
          icon="file-code"
          title="Code Block"
          checkActive={(selection) => isNodeType(selection, node => $isCodeNode(node))}
          onClick={formatCode}
        />
      );
  }

  registerExtension(controller: HtmlEditorController): OptionalCallback {
      const unsubscribe = registerCodeHighlighting(controller.editor);
      return unsubscribe;
  }

  getNodes(): LexicalConfigNode {
      return [CodeNode, CodeHighlightNode]
  }
}

function formatCode(editor: LexicalEditor): void {
  editor.update(() => {
    const selection = $getSelection();

    if (!$isRangeSelection(selection)) return;

    const revert = isNodeType(selection, (node) => $isCodeNode(node));

    $setBlocksType(selection, () =>
      revert ? $createParagraphNode() : $createCodeNode()
    );
  });
}
