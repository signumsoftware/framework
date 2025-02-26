import { CodeNode } from "@lexical/code";
import React from "react";
import { BlockStyleButton } from "../HtmlEditorButtons";
import { HtmlEditorController } from "../HtmlEditorController";
import {
  HtmlEditorExtension,
  LexicalConfigNode
} from "./types";

export class CodeBlockExtension implements HtmlEditorExtension {
  getToolbarButtons(controller: HtmlEditorController): React.ReactNode {
      return (
        <BlockStyleButton 
          controller={controller} 
          blockType="code-block" 
          icon="file-code"
          title="Code Block"
        />
      );
  }

  getNodes(): LexicalConfigNode {
      return [CodeNode]
  }
}
