export interface ImageInfoBase {
  converterKey: string;
  attachmentId?: string;
}

export abstract class ImageConverter<T extends object> {
  //static key: string; // must be overridden
  abstract uploadData(blob: Blob): Promise<T>;
  abstract renderImage(val: T): React.ReactElement;
  abstract toElement(val: T): HTMLElement | undefined;
  abstract fromElement(val: HTMLElement): T | undefined;
}
