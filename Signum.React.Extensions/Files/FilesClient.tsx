import * as React from 'react'
import * as Finder from '@framework/Finder'
import * as Navigator from '@framework/Navigator'
import { Type, PropertyRoute } from '@framework/Reflection'
import { customTypeComponent } from '@framework/Lines/DynamicComponent'
import { FileEntity, FilePathEntity, FileEmbedded, FilePathEmbedded, IFile } from './Signum.Entities.Files'
import { FileLine } from './FileLine'
import CellFormatter = Finder.CellFormatter;
import { ModifiableEntity, Lite, Entity, isLite, registerToString } from "@framework/Signum.Entities";
import { FileImageLine } from './FileImageLine';
import { MultiFileLine } from './MultiFileLine';
import { FileDownloader } from './FileDownloader';
import { FetchInState } from '@framework/Lines/Retrieve';
import { FileImage } from './FileImage';
import { ImageModal } from './ImageModal'

export function start(options: { routes: JSX.Element[] }) {

  registerAutoFileLine(FileEntity);
  registerAutoFileLine(FileEmbedded);

  registerAutoFileLine(FilePathEntity);
  registerAutoFileLine(FilePathEmbedded);

  registerToString(FileEntity, f => f.toStr ?? f.fileName);
  registerToString(FileEmbedded, f => f.toStr ?? f.fileName);
  registerToString(FilePathEntity, f => f.toStr ?? f.fileName);
  registerToString(FilePathEmbedded, f => f.toStr ?? f.fileName);
}


function registerAutoFileLine(type: Type<IFile & ModifiableEntity>) {
  customTypeComponent[type.typeName] = ctx => {
    const tr = ctx.propertyRoute.typeReference();
    if (tr.isCollection)
      return <MultiFileLine ctx={ctx} />;

    var m = ctx.propertyRoute.member;
    if (m?.defaultFileTypeInfo && m.defaultFileTypeInfo.onlyImages)
      return <FileImageLine ctx={ctx} imageHtmlAttributes={{ style: { maxWidth: '100%', maxHeight: '100%' } }} />;

    return <FileLine ctx={ctx} />;
  };

  Finder.formatRules.push({
    name: type.typeName + "_Download",
    isApplicable: c => c.token!.type.name == type.typeName && !isImage(c.token!.propertyRoute),
    formatter: c => new CellFormatter(cell => cell ? <FileDownloader entityOrLite={cell} /> : undefined)
  });

  Finder.formatRules.push({
    name: type.typeName + "_Image",
    isApplicable: c => c.token!.type.name == type.typeName && isImage(c.token!.propertyRoute),
    formatter: c => new CellFormatter(cell => !cell ? undefined :
      isLite(cell) ? <FetchInState lite={cell as Lite<IFile & Entity>}>{e => <FileThumbnail file={e as IFile & ModifiableEntity} />}</FetchInState> :
        <FileThumbnail file={cell as IFile & ModifiableEntity } />)
  });
}

interface FileThumbnailProps extends React.ImgHTMLAttributes<HTMLImageElement> {
  file: IFile & ModifiableEntity;
}

function FileThumbnail({ file, ...attrs }: FileThumbnailProps) {
  return <FileImage file={file} onClick={() => ImageModal.show(file)} {...attrs}/>
}

FileThumbnail.defaultProps = {
  style: { maxWidth: "150px" }
} as Partial<FileThumbnailProps>;

function isImage(propertyRoute: string | undefined) {

  if (propertyRoute == null)
    return false;

  let pr = PropertyRoute.parseFull(propertyRoute);

  if (pr.propertyRouteType == "MListItem")
    pr = pr.parent!;

  return Boolean(pr?.member?.defaultFileTypeInfo?.onlyImages);
}

declare module '@framework/Reflection' {

  export interface MemberInfo {
    defaultFileTypeInfo?: {
      key: string,
      onlyImages: boolean,
      maxSizeInBytes: number | null,
    };
  }
}
