import { AutoFocusPlugin } from "@lexical/react/LexicalAutoFocusPlugin";
import React from "react";
import {
  ComponentAndProps,
  HtmlEditorExtension
} from "./types";

export class AutoFocusExtension implements HtmlEditorExtension {
  getBuiltInComponent(): ComponentAndProps {
    return { component: AutoFocusPlugin };
  }
}
