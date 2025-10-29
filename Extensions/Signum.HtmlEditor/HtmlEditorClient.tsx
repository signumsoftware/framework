export namespace HtmlEditorClient {

  type ConverterClass<T extends ImageInfoBase> = { new(): ImageConverter<T> }/* & { key: string }*/;
  const ImageConverterRegistry = new Map<string, new () => ImageConverter<any>>();

  export function registerImageConverter(cls: ConverterClass<any>): void {
    ImageConverterRegistry.set(cls.name.toLowerCase(), cls);
  }

  export function getImageConverter<T extends ImageInfoBase>(key: string): ImageConverter<T> {
    const ctor = ImageConverterRegistry.get(key.toLowerCase());
    if (!ctor) throw new Error(`Converter not registered: ${key}`);
    return new ctor();
  }

}

export interface ImageInfoBase {
  converterKey: string;
  attachmentId?: string;
}

export abstract class ImageConverter<T extends object> {
  //key removed and class name is used as key
  //static key: string; // must be overridden
  abstract uploadData(blob: Blob): Promise<T>;
  abstract renderImage(val: T): React.ReactElement;
  abstract toElement(val: T): HTMLElement | undefined;
  abstract fromElement(val: HTMLElement): T | undefined;
}
