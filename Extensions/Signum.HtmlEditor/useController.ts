import { IBinding } from "@framework/Reflection";
import { LexicalEditor } from "lexical";
import React from "react";
import { BasicCommandsExtensions } from "./Extensions/BasicCommandsExtension";
import { CodeBlockExtension } from "./Extensions/CodeBlockExtension";
import { ListExtension } from "./Extensions/ListExtension";
import { OnChangeExtension } from "./Extensions/OnChangeExtension";
import {
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
  extensions?: HtmlEditorExtension[];
  initiallyFocused?: boolean | number;
  handleKeybindings?: HtmlEditorProps["handleKeybindings"];
};

type ControllerReturnType = {
  controller: HtmlEditorController;
  nodes: LexicalConfigNode;
  builtinPlugins: React.ReactElement[];
};

export const useController = ({
  binding,
  readOnly,
  small,
  converter,
  innerRef,
  extensions,
  initiallyFocused,
  handleKeybindings,
  editableId,
}: ControllerProps): ControllerReturnType => {
  const controller = React.useMemo(() => new HtmlEditorController(), []);
  const textConverter = converter ?? new HtmlContentStateConverter();

  const finalExtension: HtmlEditorExtension[] = React.useMemo(() => {
    const defaultExtensions = [
      new BasicCommandsExtensions(),
      new ListExtension(),
      new OnChangeExtension(),
      new CodeBlockExtension(),
    ];

    if (!extensions) {
      return defaultExtensions;
    }

    const result = [...defaultExtensions, ...extensions];
    result.toObject((a) => a.name); // To throw if there are duplicates

    return result;
  }, [extensions, controller]);

  React.useEffect(() => {
    if (!controller.editor) return;

    controller.editor.setEditable(!readOnly);
  }, [controller.editor, readOnly]);

  //useRegisterExtensions(controller, extensions);
  useRegisterExtensions(controller, finalExtension);

  useRegisterKeybindings(controller, handleKeybindings);

  controller.init({
    binding,
    readOnly,
    small,
    converter: textConverter,
    innerRef,
    initiallyFocused,
    //extensions,
    extensions: finalExtension,
    editableId,
  });

  const nodes = React.useMemo(() => {
    return finalExtension.flatMap((e) => e.getNodes?.() ?? []);
  }, [extensions]);

  const builtinPlugins = React.useMemo(() => {
    return finalExtension.map((e) => e.getBuiltPlugin?.()).notNull();
  }, [extensions]);

  return { controller, nodes, builtinPlugins };
};
