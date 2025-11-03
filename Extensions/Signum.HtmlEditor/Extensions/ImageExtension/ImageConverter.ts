
export interface ImageConverter {
  //converterKey: string; converter class name used as key
  dataImageIdAttribute?: string;
  uploadData(blob: Blob): Promise<ImageInfo>;
  renderImage(val: ImageInfo): React.ReactElement;
  toElement(val: ImageInfo): HTMLElement | undefined;
  fromElement(val: HTMLElement): ImageInfo | undefined;
}

export interface ImageInfo {
  converterKey: string;
  imageId?: string;
  binaryFile?: string;
  fileName?: string;
}
