import { LexicalComposer } from "@lexical/react/LexicalComposer";
import React from "react";
import "./LexicalHtmlEditor.css";
import ToolbarPlugin from "./LexicalPlugins/ToolbarPlugin";
import LexicalTheme from "./LexicalTheme";
import {
  defaultPlugins,
  ExtraPluginKey,
  extraPlugins,
  getNodesFromPlugins,
} from "./lexicalPlugins";
import { TypeContext } from "@framework/Lines";

type LexicalHtmlEditorProps = {
  extraPluginKeys?: ExtraPluginKey[];
  readOnly?: boolean;
};

export default function LexicalHtmlEditor({
  extraPluginKeys = [],
}: LexicalHtmlEditorProps): React.JSX.Element {
  const extraPluginsToUse = extraPluginKeys
    .map((key) => extraPlugins.get(key))
    .filter((v) => v != undefined);

  return (
    <LexicalComposer
      initialConfig={{
        namespace: "LexicalHtmlEditor",
        theme: LexicalTheme,
        nodes: [
          ...getNodesFromPlugins(defaultPlugins),
          ...getNodesFromPlugins(extraPluginsToUse),
        ],
        onError: (error) => console.error(error),
      }}
    >
      <div className="lex-editor-container">
        <ToolbarPlugin />
        <div className="lex-editor-inner">
          {[...defaultPlugins, ...extraPluginsToUse].map(
            ({ component: PluginComponent, config }) => {
              if (!PluginComponent) return;

              return <PluginComponent {...config} />;
            }
          )}
        </div>
      </div>
    </LexicalComposer>
  );
}
