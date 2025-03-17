import { $applyNodeReplacement, DecoratorNode, DOMExportOutput, NodeKey } from "lexical";
import { ImageConverter } from "./ImageConverter";

export class ImageNode extends DecoratorNode<JSX.Element> {
  private uploadedFile: any;
  private imageConverter: ImageConverter<any>;

  constructor(uploadedFile: any, imageConverter: ImageConverter<any>, key?: NodeKey) {
    super(key);
    this.uploadedFile = uploadedFile;
    this.imageConverter = imageConverter;
  }

  static getType(): string {
    return "image";
  }

  static clone(node: ImageNode): ImageNode {
    return new ImageNode(node.uploadedFile, node.imageConverter, node.__key);
  }

  createDOM(): HTMLElement {
    return document.createElement("div");;
  }

  updateDOM(): boolean {
    return false;
  }

  decorate(): JSX.Element {
    return this.imageConverter.renderImage(this.uploadedFile);
  }

  static importJSON(serializedNode: any): ImageNode {
    return new ImageNode(serializedNode.src, serializedNode.altText);
  }

  exportJSON(): any {
    return {
      type: "image",
      uploadedFile: this.uploadedFile,
      imageConverter: this.imageConverter,
      version: 1
    }
  }

  exportDOM(): DOMExportOutput {
    const element =  this.imageConverter.toElement(this.uploadedFile) ?? null;
    return { element: element };
  }
}

export function $createImageNode(uploadedFile: any, imageConverter: ImageConverter<any>): ImageNode {
  return $applyNodeReplacement(new ImageNode(uploadedFile, imageConverter));
}
