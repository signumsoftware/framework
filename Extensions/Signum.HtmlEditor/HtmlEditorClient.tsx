import { ImageConverter, ImageInfoBase } from "./Extensions/ImageExtension/ImageConverter";
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


