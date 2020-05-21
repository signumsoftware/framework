
declare module "draft-js-static-toolbar-plugin" {
  import { EditorPlugin } from "draft-js-plugins-editor";
  import { ComponentType, ReactNode, HTMLAttributes } from "react";
  import { EditorState } from "draft-js";

  export interface StaticToolbarPluginTheme {
    buttonStyles?: {
      buttonWrapper?: string;
      button?: string;
      active?: string;
    };
    toolbarStyles?: {
      toolbar?: string;
    };
    separatorStyles?: {
      separator?: string;
    };
  }

  export interface StaticToolbarPluginConfig {
    theme: StaticToolbarPluginTheme;
  }

  export interface ToolbarChildrenProps {
    theme: StaticToolbarPluginTheme["buttonStyles"];
    getEditorState: () => EditorState;
    setEditorState: (editorState: EditorState) => void;
    onOverrideContent: (content: ComponentType<ToolbarChildrenProps>) => void;
  }

  export interface ToolbarProps {
    children?(externalProps: ToolbarChildrenProps): ReactNode;
  }

  export type StaticToolbarPlugin = EditorPlugin & {
    Toolbar: ComponentType<ToolbarProps>;
  };

  const createStaticToolbarPlugin: (
    config?: StaticToolbarPluginConfig
  ) => StaticToolbarPlugin;

  export const Separator: ComponentType<HTMLAttributes<HTMLDivElement>>;
  export default createStaticToolbarPlugin;
}
