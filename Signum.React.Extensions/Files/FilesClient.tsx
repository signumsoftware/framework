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
import { ImageModal } from './ImageModal';
import { IconName } from '@fortawesome/fontawesome-svg-core';

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
    const tr = ctx.propertyRoute!.typeReference();
    if (tr.isCollection)
      return <MultiFileLine ctx={ctx} />;

    var m = ctx.propertyRoute!.member;
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
        <FileThumbnail file={cell as IFile & ModifiableEntity} />)
  });
}

export const extensionInfo: { [ext: string]: { icon: IconName, color: string, mimeType?: string } } = {

  ["jpg"]: { icon: "file-image", color: "#5DADE2", mimeType: "image/jpeg" },
  ["jpeg"]: { icon: "file-image", color: "#5DADE2", mimeType: "image/jpeg" },
  ["gif"]: { icon: "file-image", color: "#5DADE2", mimeType: "image/gif" },
  ["png"]: { icon: "file-image", color: "#5DADE2", mimeType: "image/png" },
  ["bmp"]: { icon: "file-image", color: "#5DADE2", mimeType: "image/bmp" },
  ["tiff"]: { icon: "file-image", color: "#5DADE2", mimeType: "image/tiff" },
  ["svg"]: { icon: "file-image", color: "#21618C", mimeType: "image/svg+xml" },
  ["psd"]: { icon: "file-image", color: "#21618C" },
  ["ai"]: { icon: "file-image", color: "#21618C"},

  ["doc"]: { icon: "file-word", color: "#2a5699", mimeType: "application/msword" },
  ["docx"]: { icon: "file-word", color: "#2a5699", mimeType: "application/vnd.openxmlformats-officedocument.wordprocessingml.document" },

  ["xls"]: { icon: "file-excel", color: "#02723b", mimeType: "application/vnd.ms-excel" },
  ["xlsx"]: { icon: "file-excel", color: "#02723b", mimeType: "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" },

  ["ppt"]: { icon: "file-powerpoint", color: "rgb(207 66 36)", mimeType: "application/vnd.ms-powerpoint" },
  ["pptx"]: { icon: "file-powerpoint", color: "rgb(207 66 36)", mimeType: "application/vnd.openxmlformats-officedocument.presentationml.presentation" },

  ["pdf"]: { icon: "file-pdf", color: "#b30b00", mimeType: "application/pdf" },

  ["html"]: { icon: "file-code", color: "#373377", mimeType: "text/html" },
  ["xml"]: { icon: "file-code", color: "#373377", mimeType: "text/xml" },
  ["css"]: { icon: "file-code", color: "#373377", mimeType: "text/css" },
  ["js"]: { icon: "file-code", color: "#373377", mimeType: "text/javascript" },
  ["jsx"]: { icon: "file-code", color: "#373377", mimeType: "text/plain" },
  ["ts"]: { icon: "file-code", color: "#373377", mimeType: "text/plain" },
  ["tsx"]: { icon: "file-code", color: "#373377", mimeType: "text/plain" },
  ["cs"]: { icon: "file-code", color: "#373377", mimeType: "text/plain" },
  ["csproj"]: { icon: "file-code", color: "#373377", mimeType: "text/plain" },
  ["sln"]: { icon: "file-code", color: "#373377", mimeType: "text/plain" },
  ["py"]: { icon: "file-code", color: "#373377", mimeType: "text/plain" },
  ["c"]: { icon: "file-code", color: "#373377", mimeType: "text/plain" },
  ["cpp"]: { icon: "file-code", color: "#373377", mimeType: "text/plain" },
  ["vb"]: { icon: "file-code", color: "#373377", mimeType: "text/plain" },
  ["fs"]: { icon: "file-code", color: "#373377", mimeType: "text/plain" },

  ["zip"]: { icon: "file-archive", color: "#F5B041", mimeType: "application/zip" },
  ["rar"]: { icon: "file-archive", color: "#F5B041", mimeType: "application/vnd.rar" },
  ["7z"]: { icon: "file-archive", color: "#F5B041", mimeType: "application/x-7z-compressed" },

  ["txt"]: { icon: "file-alt", color: "#566573", mimeType: "text/plain"  },
  ["rtf"]: { icon: "file-alt", color: "#566573", mimeType: "text/plain"  },
  ["info"]: { icon: "file-alt", color: "#566573", mimeType: "text/plain"  },
  ["log"]: { icon: "file-alt", color: "#566573", mimeType: "text/plain"  },

  ["csv"]: { icon: "file-csv", color: "#566573", mimeType: "text/plain"  },

  ["avi"]: { icon: "file-video", color: "red", mimeType: "video/x-msvideo" },
  ["mkv"]: { icon: "file-video", color: "red", mimeType: "video/x-matroska" },
  ["mpeg"]: { icon: "file-video", color: "red", mimeType: "video/mpeg" },
  ["mpg"]: { icon: "file-video", color: "red", mimeType: "video/mpeg" },
  ["mp4"]: { icon: "file-video", color: "red", mimeType: "video/mpeg"  },
  ["ogg"]: { icon: "file-video", color: "red", mimeType: "video/ogg" },
  ["ogv"]: { icon: "file-video", color: "red", mimeType: "video/ogg" },
  ["mov"]: { icon: "file-video", color: "red", mimeType: "video/quicktime" },
  ["webm"]: { icon: "file-video", color: "red", mimeType: "video/webm" },
  ["wmv"]: { icon: "file-video", color: "red", mimeType: "video/x-ms-asf" },

  ["mp3"]: { icon: "file-audio", color: "#BA4A00", mimeType: "audio/mpeg" },
  ["weba"]: { icon: "file-audio", color: "#BA4A00", mimeType: "audio/webm" },
  ["wav"]: { icon: "file-audio", color: "#BA4A00", mimeType: "audio/wav" },
  ["wma"]: { icon: "file-audio", color: "#BA4A00", mimeType: "audio/x-ms-wma" },
};


interface FileThumbnailProps extends React.ImgHTMLAttributes<HTMLImageElement> {
  file: IFile & ModifiableEntity;
}

function FileThumbnail({ file, ...attrs }: FileThumbnailProps) {
  return <FileImage file={file} onClick={() => ImageModal.show(file)} {...attrs} />
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
