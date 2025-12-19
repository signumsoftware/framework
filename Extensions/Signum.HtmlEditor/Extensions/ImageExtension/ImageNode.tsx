import { $applyNodeReplacement, DecoratorNode, DOMConversion, DOMConversionMap, DOMConversionOutput, DOMExportOutput, EditorConfig, LexicalEditor, NodeKey } from "lexical";
import { ImageHandlerBase, ImageInfo } from "./ImageHandlerBase";
import { ReactElement } from "react";

// Pseudo-abstract base class.
// Should be subclassed by concrete nodes that implement getType, clone, and importJSON.
export class ImageNode extends DecoratorNode<React.ReactElement> {

  static getType(): string {
    return "image";
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

  decorate(editor: LexicalEditor, config: EditorConfig): ReactElement {
    return editor.imageHandler!.renderImage(this.imageInfo);
  }

  exportJSON(): any {
    return {
      type: "image",
      uploadedFile: this.imageInfo,
      version: 1
    }
  }

  exportDOM(editor: LexicalEditor): DOMExportOutput {
    const element = editor.imageHandler!.toElement(this.imageInfo) ?? null;
    return { element: element };
  }

  static currentHandler: ImageHandlerBase | undefined; //Hack but the is no way to acces the editor in importDOM, at least is sync.

  static importDOM(): DOMConversionMap | null {
    return {
      img: (domNode: HTMLElement) => {
        return {
          priority: 1,
          conversion: (element: HTMLElement) => {
            try {
              if (this.currentHandler == null)
                throw new Error("currentHandler not set");

              const info = this.currentHandler!.fromElement(element);
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
  
  static clone(node: ImageNode): ImageNode {
    return new ImageNode(node.imageInfo, node.__key);
  }

  static importJSON(serializedNode: ImageInfo): ImageNode {
    return new ImageNode(serializedNode);
  }
}

export function $createImageNode(file: ImageInfo, nodeType: typeof ImageNode): ImageNode {
  const node = new nodeType(file);
  return $applyNodeReplacement(node);
}
