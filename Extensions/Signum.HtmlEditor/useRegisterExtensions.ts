import React from "react";
import { HtmlEditorExtension } from "./Extensions/types";
import { HtmlEditorController } from "./HtmlEditorController";

export const useRegisterExtensions = (
  controller: HtmlEditorController,
  extensions: HtmlEditorExtension[] = []
): void => {
  React.useEffect(() => {
    if (!controller?.editor)
      return;

    const unsubscribeFns = extensions
      .flatMap((e) => [e.registerExtension?.(controller)])
      .notNull();

    return () => unsubscribeFns.forEach((fn) => fn());
  }, [controller.editor, extensions]);
};
