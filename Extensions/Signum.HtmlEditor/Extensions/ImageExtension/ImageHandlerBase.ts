import { SerializedLexicalNode } from "lexical";
import { ImageNodeBase } from "./ImageNodeBase";

export interface ImageHandlerBase {
  getNodeType(): typeof ImageNodeBase;
  uploadData(blob: Blob): Promise<ImageInfo>;
  renderImage(val: ImageInfo): React.ReactElement;
  toElement(val: ImageInfo): HTMLElement | undefined;
  fromElement(val: HTMLElement): ImageInfo | undefined;
}

export interface ImageInfo extends Partial<SerializedLexicalNode>{
  key?: string;
  binaryFile?: string;
  fileName?: string;
}
