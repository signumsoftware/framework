import { InitialConfigType } from "@lexical/react/LexicalComposer";
import { HtmlEditorController } from "../HtmlEditorController";

export interface HtmlEditorExtension {
  name: string;
  getToolbarButtons?(controller: HtmlEditorController): React.ReactNode;
  registerExtension?(controller: HtmlEditorController): OptionalCallback;
  getNodes?(): LexicalConfigNode;
  getBuiltInComponent?(): ComponentAndProps;
}

export type ComponentAndProps<
  T extends React.FC<P> = React.FC<any>,
  P extends {} = React.ComponentProps<T>
> = {
  component: T;
  props?: P;
};

export type OptionalCallback = (() => void) | null | undefined;
export type LexicalConfigNode = InitialConfigType["nodes"];
