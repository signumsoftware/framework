import * as React from 'react'
import * as Finder from '@framework/Finder'
import * as Navigator from '@framework/Navigator'
import { Type, PropertyRoute } from '@framework/Reflection'
import DynamicComponent from '@framework/Lines/DynamicComponent'
import { FileEntity, FilePathEntity, FileEmbedded, FilePathEmbedded, IFile } from './Signum.Entities.Files'
import FileLine from './FileLine'
import CellFormatter = Finder.CellFormatter;
import { ModifiableEntity, Lite, Entity, isLite, registerToString } from "@framework/Signum.Entities";
import FileImageLine from './FileImageLine';
import { MultiFileLine } from './MultiFileLine';
import FileDownloader from './FileDownloader';
import { Retrieve } from '@framework/Retrieve';
import { FileImage } from './FileImage';

export function start(options: { routes: JSX.Element[] }) {

  registerAutoFileLine(FileEntity);
  registerAutoFileLine(FileEmbedded);

  registerAutoFileLine(FilePathEntity);
  registerAutoFileLine(FilePathEmbedded);

  registerToString(FileEntity, f => f.toStr || f.fileName);
  registerToString(FileEmbedded, f => f.toStr || f.fileName);
  registerToString(FilePathEntity, f => f.toStr || f.fileName);
  registerToString(FilePathEmbedded, f => f.toStr || f.fileName);
  
  Finder.formatRules.push({
    name: "WebDownload",
    isApplicable: col => col.token!.type.name === "WebDownload",
    formatter: col => new CellFormatter((cell: WebDownload) =>
      !cell ? undefined : <a href={Navigator.toAbsoluteUrl(cell.fullWebPath)} download={cell.fileName}>{cell.fileName}</a>)
  });

  Finder.formatRules.push({
    name: "WebImage",
    isApplicable: col => col.token!.type.name === "WebImage",
    formatter: col => new CellFormatter((cell: WebImage) =>
      !cell ? undefined : <img src={Navigator.toAbsoluteUrl(cell.fullWebPath)} />)
  });
}


function registerAutoFileLine(type: Type<IFile & ModifiableEntity>) {
  DynamicComponent.customTypeComponent[type.typeName] = ctx => {
    const tr = ctx.propertyRoute.typeReference();
    if (tr.isCollection)
      return <MultiFileLine ctx={ctx} />;

    var m = ctx.propertyRoute.member;
    if (m && m.defaultFileTypeInfo && m.defaultFileTypeInfo.onlyImages)
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
      isLite(cell) ? Retrieve.create(cell as Lite<IFile & Entity>, e => <FileImage file={e} />) :
        < FileImage file={cell as IFile & ModifiableEntity} />)
  });
}

function isImage(propertyRoute: string | undefined) {

  if (propertyRoute == null)
    return false;

  const pr = PropertyRoute.parseFull(propertyRoute)

  return Boolean(pr && pr.member && pr.member.defaultFileTypeInfo && pr.member.defaultFileTypeInfo.onlyImages);

}
  
  
export interface WebDownload {
  fileName: string;
  fullWebPath: string;
}

export interface WebImage {
  fullWebPath: string;
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
