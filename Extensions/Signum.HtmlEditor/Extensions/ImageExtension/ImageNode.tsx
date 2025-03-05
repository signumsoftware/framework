import { $applyNodeReplacement, DecoratorNode, EditorConfig, LexicalEditor, NodeKey } from "lexical";
import React from "react";

export class ImageNode extends DecoratorNode<JSX.Element> {
  private src: string;
  private altText: string;
  private onClick?: React.MouseEventHandler;

  constructor(src: string, altText = "", onClick?: React.MouseEventHandler, key?: NodeKey) {
    super(key);
    this.src = src;
    this.altText = altText;
    this.onClick = onClick;
  }

  static getType(): string {
    return "image";
  }

  static clone(node: ImageNode): ImageNode {
    return new ImageNode(node.src, node.altText, node.onClick, node.__key);
  }

  createDOM(): HTMLElement {
    return document.createElement("span");
  }

  updateDOM(): boolean {
    return false;
  }

  decorate(): JSX.Element {
    return <img src={this.src} alt={this.altText} onClick={this.onClick} className="rounded mw-100" />
  }

  static importJSON(serializedNode: any): ImageNode {
    return new ImageNode(serializedNode.src, serializedNode.altText);
  }

  exportJSON(): any {
    return {
      type: "image",
      src: this.src,
      altText: this.altText,
      version: 1
    }
  }
}

export function $createImageNode(src: string, altText = "", onClick?: React.MouseEventHandler): ImageNode {
  return $applyNodeReplacement(new ImageNode(src, altText, onClick));
}
