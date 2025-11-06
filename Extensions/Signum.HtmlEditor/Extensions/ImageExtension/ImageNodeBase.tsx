import { $applyNodeReplacement, DecoratorNode, DOMConversion, DOMConversionMap, DOMConversionOutput, DOMExportOutput, NodeKey } from "lexical";
import { ImageHandlerBase, ImageInfo } from "./ImageHandlerBase";
import { ReactElement } from "react";

// Pseudo-abstract base class.
// Should be subclassed by concrete nodes that implement getType, clone, and importJSON.
export class ImageNodeBase extends DecoratorNode<React.ReactElement> {

  static dataImageIdAttribute: string;
   
  static converter: ImageHandlerBase;

  getHandler(): ImageHandlerBase {
    return (this.constructor as typeof ImageNodeBase).converter;
  }

  constructor(public imageInfo: ImageInfo, key?: NodeKey) {
    super(key);
  }

  createDOM(): HTMLElement {
    return document.createElement("div");
  }

  updateDOM(): boolean {
    return false;
  }

  decorate(): ReactElement {
    return this.getHandler().renderImage(this.imageInfo);
  }

  exportJSON(): any {
    return {
      type: "image",
      uploadedFile: this.imageInfo,
      version: 1
    }
  }

  exportDOM(): DOMExportOutput {
    const element =  this.getHandler().toElement(this.imageInfo) ?? null;
    return { element: element };
  }

  static importDOM(): DOMConversionMap | null {
    return {
      img: (domNode: HTMLElement) => {
        return {
          priority: 1,
          conversion: (element: HTMLElement) => {
            try {
              const info = this.converter.fromElement(element);
              if (!info) return null;
              return { node: new this(info) };
            } catch {
              return null;
            }
          },
        };
      },
    };
  }

}

export function $createImageNode(file: ImageInfo, nodeType: typeof ImageNodeBase): ImageNodeBase {
  const node = new nodeType(file);
  return $applyNodeReplacement(node);
}
