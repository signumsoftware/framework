import * as React from 'react'
import * as Finder from '@framework/Finder'
import { Type } from '@framework/Reflection'
import DynamicComponent from '@framework/Lines/DynamicComponent'
import { FileEntity, FilePathEntity, FileEmbedded, FilePathEmbedded, IFile } from './Signum.Entities.Files'
import FileLine from './FileLine'
import CellFormatter = Finder.CellFormatter;
import { ModifiableEntity } from "@framework/Signum.Entities";
import FileImageLine from './FileImageLine';
import { MultiFileLine } from './MultiFileLine';

export function start(options: { routes: JSX.Element[] }) {

  registerAutoFileLine(FileEntity);
  registerAutoFileLine(FileEmbedded);

  registerAutoFileLine(FilePathEntity);
  registerAutoFileLine(FilePathEmbedded);


  Finder.formatRules.push({
    name: "WebDownload",
    isApplicable: col => col.token!.type.name === "WebDownload",
    formatter: col => new CellFormatter((cell: WebDownload) =>
      !cell ? undefined : <a href={cell.fullWebPath} download={cell.fileName}>{cell.fileName}</a>)
  });

  Finder.formatRules.push({
    name: "WebImage",
    isApplicable: col => col.token!.type.name === "WebImage",
    formatter: col => new CellFormatter((cell: WebImage) =>
      !cell ? undefined : <img src={cell.fullWebPath} />)
  });
}


function registerAutoFileLine(type: Type<IFile & ModifiableEntity>) {
  DynamicComponent.customTypeComponent[type.typeName] = ctx => {
    const tr = ctx.propertyRoute.typeReference();
    if (tr.isCollection)
      return <MultiFileLine ctx={ctx} />;

    var m = ctx.propertyRoute.member;
    if (m && m.defaultFileTypeInfo && m.defaultFileTypeInfo.onlyImages)
      return <FileImageLine ctx={ctx} />;

    return <FileLine ctx={ctx} />;
  };
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
