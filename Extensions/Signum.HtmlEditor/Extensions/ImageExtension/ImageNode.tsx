import { $applyNodeReplacement, DecoratorNode, DOMConversion, DOMConversionMap, DOMConversionOutput, DOMExportOutput, NodeKey } from "lexical";
import { ImageConverter, ImageInfoBase } from "./ImageConverter";
import { getImageConverter } from "../../HtmlEditorClient";

export class ImageNode<T extends ImageInfoBase> extends DecoratorNode < JSX.Element > {
  constructor(private fileInfo: T, private imageConverter: ImageConverter<T>, key?: NodeKey) {
    super(key);
    this.fileInfo = fileInfo;
    this.imageConverter = imageConverter;
  }

  static getType(): string {
    return "image";
  }

  static clone(node: ImageNode<any>): ImageNode<any> {
    return new ImageNode(node.fileInfo, node.imageConverter, node.__key);
  }

  createDOM(): HTMLElement {
    return document.createElement("div");
  }

  updateDOM(): boolean {
    return false;
  }

  decorate(): JSX.Element {
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

  static importDOM(): DOMConversionMap | null {
    return {
      img: (domNode: HTMLElement) => {
        const converterKey = domNode.dataset["converterKey"];
        if (!converterKey) return null;

        return {
          priority: 1,
          conversion: (element: HTMLElement) => {
            try {
              const converter = getImageConverter<any>(converterKey);
              const info = converter.fromElement(element);
              if (!info) return null;
              return { node: new ImageNode<any>(info, converter) };
            } catch {
              return null;
            }
          },
        };
      },
    };
  }

}

export function $createImageNode<T extends ImageInfoBase>(uploadedFile: T, imageConverter: ImageConverter<T>): ImageNode<T>{
  return $applyNodeReplacement(new ImageNode(uploadedFile, imageConverter));
}
