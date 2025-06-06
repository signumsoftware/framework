
export interface ImageConverter<T extends object> {
  uploadData(blob: Blob): Promise<T>;
  renderImage(val: T): React.ReactElement;
  toElement(val: T): HTMLElement | undefined;
  fromElement(val: HTMLElement): T | undefined;
}
