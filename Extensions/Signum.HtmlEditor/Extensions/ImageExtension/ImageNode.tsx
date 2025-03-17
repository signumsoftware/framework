import { $applyNodeReplacement, DecoratorNode, DOMExportOutput, NodeKey } from "lexical";
import { ImageConverter } from "./ImageConverter";

export class ImageNode<T extends object = {}> extends DecoratorNode<JSX.Element> {
  constructor(private uploadedFile: T, private imageConverter: ImageConverter<T>, key?: NodeKey) {
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

export function $createImageNode<T extends object = {}>(uploadedFile: T, imageConverter: ImageConverter<T>): ImageNode<T>{
  return $applyNodeReplacement(new ImageNode(uploadedFile, imageConverter));
}
