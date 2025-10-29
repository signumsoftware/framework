import { ImageConverter, ImageInfoBase } from "./Extensions/ImageExtension/ImageConverter";

const registry = new Map<string, new () => ImageConverter<any>>();

export function registerImageConverter(cls: ConverterClass<any>): void {
  registry.set(cls.key, cls);
}

export function getImageConverter<T extends ImageInfoBase>(key: string): ImageConverter<T> {
  const ctor = registry.get(key);
  if (!ctor) throw new Error(`Converter not registered: ${key}`);
  return new ctor();
}


