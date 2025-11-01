import { $applyNodeReplacement, DecoratorNode, DOMExportOutput, NodeKey } from "lexical";
import { ImageConverter, ImageInfo } from "./ImageConverter";
import { ReactElement, JSXElementConstructor } from "react";

export class ImageNode extends DecoratorNode<React.ReactElement> {
  constructor(private imageInfo: ImageInfo, private imageConverter: ImageConverter, key?: NodeKey) {
    super(key);
    this.imageInfo = imageInfo;
    this.imageConverter = imageConverter;
  }

  static getType(): string {
    return "image";
  }

  static clone(node: ImageNode): ImageNode {
    return new ImageNode(node.imageInfo, node.imageConverter, node.__key);
  }

  createDOM(): HTMLElement {
    return document.createElement("div");
  }

  updateDOM(): boolean {
    return false;
  }

  decorate(): ReactElement {
    return this.imageConverter.renderImage(this.imageInfo);
  }

  exportJSON(): any {
    return {
      type: "image",
      uploadedFile: this.imageInfo,
      imageConverter: this.imageConverter,
      version: 1
    }
  }

  exportDOM(): DOMExportOutput {
    const element =  this.imageConverter.toElement(this.imageInfo) ?? null;
    return { element: element };
  }
}

export function $createImageNode(uploadedFile: ImageInfo, imageConverter: ImageConverter): ImageNode {
  return $applyNodeReplacement(new ImageNode(uploadedFile, imageConverter));
}
