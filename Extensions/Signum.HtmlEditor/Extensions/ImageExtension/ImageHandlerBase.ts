import { SerializedLexicalNode } from "lexical";
import { ImageNodeBase } from "./ImageNodeBase";

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
