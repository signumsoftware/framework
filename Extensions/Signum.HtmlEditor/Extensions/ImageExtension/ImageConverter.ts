
export interface ImageConverter {
  dataImageIdAttribute?: string;
  uploadData(blob: Blob): Promise<ImageInfo>;
  renderImage(val: ImageInfo): React.ReactElement;
  toElement(val: ImageInfo): HTMLElement | undefined;
  fromElement(val: HTMLElement): ImageInfo | undefined;
}

export interface ImageInfo {
  imageId?: string;
  binaryFile?: string;
  fileName?: string;
}
