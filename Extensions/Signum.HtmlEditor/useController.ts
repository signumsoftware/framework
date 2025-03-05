import { IBinding } from "@framework/Reflection";
import { LexicalEditor } from "lexical";
import React from "react";
import { BasicCommandsExtensions } from "./Extensions/BasicCommandsExtension";
import { CodeBlockExtension } from "./Extensions/CodeBlockExtension";
import { ImageExtension } from "./Extensions/ImageExtension";
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
import { HtmlEditorController } from "./HtmlEditorController";
import { useRegisterExtensions } from "./useRegisterExtensions";

type ControllerProps = {
  binding: IBinding<string | null | undefined>;
  readOnly?: boolean;
  small?: boolean;
  converter?: ITextConverter;
  innerRef?: React.Ref<LexicalEditor>;
  plugins?: HtmlEditorExtension[];
  initiallyFocused?: boolean | number;
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
}: ControllerProps): ControllerReturnType => {
  const controller = React.useMemo(() => new HtmlEditorController(), []);
  const textConverter = converter ?? new HtmlContentStateConverter();

  const extensions: HtmlEditorExtension[] = React.useMemo(() => {
    const defaultPlugins = [
      new BasicCommandsExtensions(),
      new ListExtension(),
      new OnChangeExtension(),
      new CodeBlockExtension(),
      new ImageExtension()
    ];

    if (!plugins) {
      return defaultPlugins;
    }

    return [...defaultPlugins, ...plugins];
  }, [plugins, controller]);

  useRegisterExtensions(controller, extensions);

  controller.init({
    binding,
    readOnly,
    small,
    converter: textConverter,
    innerRef,
    initiallyFocused,
    plugins: extensions,
  });

  const nodes = React.useMemo(() => {
    return extensions.flatMap((plugin) => plugin.getNodes?.() ?? []);
  }, [extensions]);

  const builtinComponents = React.useMemo(() => {
    return extensions.map((plugin) => plugin.getBuiltInComponent?.()).notNull();
  }, [extensions]);

  return { controller, nodes, builtinComponents };
};
