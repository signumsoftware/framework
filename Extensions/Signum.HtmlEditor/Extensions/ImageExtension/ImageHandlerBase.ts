import { LexicalEditor, SerializedLexicalNode } from "lexical";
import { ImageNode } from "./ImageNode";

export interface ImageHandlerBase {
  uploadData(blob: Blob): Promise<ImageInfo>;
  renderImage(val: ImageInfo): React.ReactElement;
  toElement(val: ImageInfo): HTMLElement | undefined;
  fromElement(val: HTMLElement): ImageInfo | undefined;
}

export interface ImageInfo extends Partial<SerializedLexicalNode>{
  imageId?: string;
  binaryFile?: string;
  fileName?: string;
}


declare module "lexical" {
  export interface LexicalEditor {
    imageHandler?: ImageHandlerBase;
  }
}
