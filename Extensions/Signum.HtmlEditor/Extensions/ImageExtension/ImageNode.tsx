import { $applyNodeReplacement, DecoratorNode, DOMExportOutput, NodeKey } from "lexical";
import { ImageConverter } from "./ImageConverter";
import { ReactElement, JSXElementConstructor } from "react";

export class ImageNode<T extends object = {}> extends DecoratorNode<React.ReactElement> {
  constructor(private fileInfo: T, private imageConverter: ImageConverter<T>, key?: NodeKey) {
    super(key);
    this.fileInfo = fileInfo;
    this.imageConverter = imageConverter;
  }

  static getType(): string {
    return "image";
  }

  static clone(node: ImageNode): ImageNode {
    return new ImageNode(node.fileInfo, node.imageConverter, node.__key);
  }

  createDOM(): HTMLElement {
    return document.createElement("div");
  }

  updateDOM(): boolean {
    return false;
  }

  decorate(): ReactElement {
    return this.imageConverter.renderImage(this.fileInfo);
  }

  exportJSON(): any {
    return {
      type: "image",
      uploadedFile: this.fileInfo,
      imageConverter: this.imageConverter,
      version: 1
    }
  }

  exportDOM(): DOMExportOutput {
    const element =  this.imageConverter.toElement(this.fileInfo) ?? null;
    return { element: element };
  }
}

export function $createImageNode<T extends object = {}>(uploadedFile: T, imageConverter: ImageConverter<T>): ImageNode<T>{
  return $applyNodeReplacement(new ImageNode(uploadedFile, imageConverter));
}
