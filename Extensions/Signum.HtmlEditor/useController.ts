import { IBinding } from "@framework/Reflection";
import { LexicalEditor } from "lexical";
import React from "react";
import { BasicCommandsExtensions } from "./Extensions/BasicCommandsExtension";
import { CodeBlockExtension } from "./Extensions/CodeBlockExtension";
import { ListExtension } from "./Extensions/ListExtension";
import { OnChangeExtension } from "./Extensions/OnChangeExtension";
import {
  ComponentAndProps,
  HtmlEditorExtension,
  LexicalConfigNode,
} from "./Extensions/types";
import {
  HtmlContentStateConverter,
  ITextConverter,
} from "./HtmlContentStateConverter";
import { HtmlEditorProps } from "./HtmlEditor";
import { HtmlEditorController } from "./HtmlEditorController";
import { useRegisterExtensions } from "./useRegisterExtensions";
import { useRegisterKeybindings } from "./useRegisterKeybindings";
import { ImageExtension } from "./Extensions/ImageExtension";

type ControllerProps = {
  binding: IBinding<string | null | undefined>;
  editableId: string;
  readOnly?: boolean;
  small?: boolean;
  converter?: ITextConverter;
  innerRef?: React.Ref<LexicalEditor>;
  plugins?: HtmlEditorExtension[];
  initiallyFocused?: boolean | number;
  handleKeybindings?: HtmlEditorProps["handleKeybindings"];
};

type ControllerReturnType = {
  controller: HtmlEditorController;
  nodes: LexicalConfigNode;
  builtinComponents: ComponentAndProps[];
};

export const useController = ({
  binding,
  readOnly,
  small,
  converter,
  innerRef,
  plugins,
  initiallyFocused,
  handleKeybindings,
  editableId,
}: ControllerProps): ControllerReturnType => {
  const controller = React.useMemo(() => new HtmlEditorController(), []);
  const textConverter = converter ?? new HtmlContentStateConverter(plugins?.firstOrNull(a => a instanceof ImageExtension)?.imageConverter.dataImageIdAttribute);

  const extensions: HtmlEditorExtension[] = React.useMemo(() => {
    const defaultPlugins = [
      new BasicCommandsExtensions(),
      new ListExtension(),
      new OnChangeExtension(),
      new CodeBlockExtension(),
    ];

    if (!plugins) {
      return defaultPlugins;
    }

    const result = [...defaultPlugins, ...plugins];
    result.toObject((a) => a.name); // To throw if there are duplicates

    return result;
  }, [plugins, controller]);

  React.useEffect(() => {
    if (!controller.editor) return;

    controller.editor.setEditable(!readOnly);
  }, [controller.editor, readOnly]);

  useRegisterExtensions(controller, extensions);

  useRegisterKeybindings(controller, handleKeybindings);

  controller.init({
    binding,
    readOnly,
    small,
    converter: textConverter,
    innerRef,
    initiallyFocused,
    plugins: extensions,
    editableId,
  });

  const nodes = React.useMemo(() => {
    return extensions.flatMap((plugin) => plugin.getNodes?.() ?? []);
  }, [extensions]);

  const builtinComponents = React.useMemo(() => {
    return extensions.map((plugin) => plugin.getBuiltInComponent?.()).notNull();
  }, [extensions]);

  return { controller, nodes, builtinComponents };
};
