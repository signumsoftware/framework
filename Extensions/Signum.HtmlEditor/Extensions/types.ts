import { InitialConfigType } from "@lexical/react/LexicalComposer";
import { HtmlEditorController } from "../HtmlEditorController";

export interface HtmlEditorExtension {
  name: string;
  getToolbarButtons?(controller: HtmlEditorController): React.ReactNode;
  registerExtension?(controller: HtmlEditorController): OptionalCallback;
  getNodes?(): LexicalConfigNode;
  getBuiltPlugin?(): React.ReactElement;
}

export type OptionalCallback = (() => void) | null | undefined;
export type LexicalConfigNode = InitialConfigType["nodes"];
