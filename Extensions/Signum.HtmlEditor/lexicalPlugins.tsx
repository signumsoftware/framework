import { InitialConfigType } from "@lexical/react/LexicalComposer";
import React, { ComponentProps } from "react";
import { LexicalErrorBoundary } from "@lexical/react/LexicalErrorBoundary";
import { ContentEditable } from "@lexical/react/LexicalContentEditable";
import { RichTextPlugin } from "@lexical/react/LexicalRichTextPlugin";
import { AutoFocusPlugin } from "@lexical/react/LexicalAutoFocusPlugin";
import { HeadingNode } from "@lexical/rich-text";
import { ListItemNode, ListNode } from "@lexical/list";

export interface CustomLexicalExt<T = any> {
  nodes: InitialConfigType["nodes"];
  component?: React.FC<T>;
  config?: ComponentProps<React.FC<T>>;
}

export const defaultPlugins: CustomLexicalExt[] = [
  {
    component: RichTextPlugin,
    config: {
      contentEditable: <ContentEditable className="lex-editor-input" />,
      ErrorBoundary: LexicalErrorBoundary,
    },
    nodes: [],
  },
  { component: AutoFocusPlugin, nodes: [] },
  { nodes: [HeadingNode] },
  { nodes: [ListNode, ListItemNode] },
];

export enum ExtraPluginKey {
  Link = "custom",
}

export type ExtraPluginsMapType = Map<ExtraPluginKey, CustomLexicalExt>;

export const extraPlugins: ExtraPluginsMapType = new Map([]);

export function getNodesFromPlugins(
  plugins: CustomLexicalExt[]
): NonNullable<CustomLexicalExt["nodes"]> {
  return plugins
    .flatMap((plugin) => plugin.nodes)
    .filter((v) => v !== undefined);
}
