import { ImageConverter } from "./Extensions/ImageExtension/ImageConverter";

export namespace HtmlEditorClient {

  class UniqueMap<K, V> extends Map<K, V> {
    set(key: K, value: V): this {
      if (this.has(key))
        throw new Error(`Duplicate key registration: ${String(key)}`);
      return super.set(key, value);
    }
  }

  type ConverterClass = { new(): ImageConverter };
  const ImageConverterRegistry = new UniqueMap<string, new () => ImageConverter>();

  export function registerImageConverter(cls: ConverterClass): void {
    ImageConverterRegistry.set(cls.name.toLowerCase(), cls);
  }

  export function getImageConverter(key: string): ImageConverter {
    const ctor = ImageConverterRegistry.get(key.toLowerCase());
    if (!ctor) throw new Error(`Converter not registered: ${key}`);
    return new ctor();
  }

}
