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
    isApplicable: qt => qt.type.name == type.typeName && !isImage(qt.propertyRoute),
    formatter: qt => new CellFormatter(cell => cell ? <FileDownloader entityOrLite={cell} htmlAttributes={{ className: "try-no-wrap" }} /> : undefined, true)
  });

  Finder.formatRules.push({
    name: type.typeName + "_Image",
    isApplicable: qt => qt.type.name == type.typeName && isImage(qt.propertyRoute),
    formatter: c => new CellFormatter(cell => !cell ? undefined :
      isLite(cell) ? <FetchInState lite={cell as Lite<IFile & Entity>}>{e => <FileThumbnail file={e as IFile & ModifiableEntity} />}</FetchInState> :
        <FileThumbnail file={cell as IFile & ModifiableEntity} />, false)
  });
}

export interface ExtensionInfo {
  icon: IconName;
  color: string;
  mimeType?: string;
  browserView?: boolean;
}

export const extensionInfo: { [ext: string]: ExtensionInfo } = {

  ["jpg"]: { icon: "file-image", color: "#5DADE2", mimeType: "image/jpeg", browserView: true },
  ["jpeg"]: { icon: "file-image", color: "#5DADE2", mimeType: "image/jpeg", browserView: true },
  ["gif"]: { icon: "file-image", color: "#5DADE2", mimeType: "image/gif", browserView: true },
  ["png"]: { icon: "file-image", color: "#5DADE2", mimeType: "image/png", browserView: true },
  ["bmp"]: { icon: "file-image", color: "#5DADE2", mimeType: "image/bmp", browserView: true },
  ["tiff"]: { icon: "file-image", color: "#5DADE2", mimeType: "image/tiff", browserView: true },
  ["svg"]: { icon: "file-image", color: "#21618C", mimeType: "image/svg+xml", browserView: true },
  ["psd"]: { icon: "file-image", color: "#21618C" },
  ["ai"]: { icon: "file-image", color: "#21618C"},

  ["doc"]: { icon: "file-word", color: "#2a5699", mimeType: "application/msword" },
  ["docx"]: { icon: "file-word", color: "#2a5699", mimeType: "application/vnd.openxmlformats-officedocument.wordprocessingml.document" },

  ["xls"]: { icon: "file-excel", color: "#02723b", mimeType: "application/vnd.ms-excel" },
  ["xlsx"]: { icon: "file-excel", color: "#02723b", mimeType: "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" },

  ["ppt"]: { icon: "file-powerpoint", color: "rgb(207 66 36)", mimeType: "application/vnd.ms-powerpoint" },
  ["pptx"]: { icon: "file-powerpoint", color: "rgb(207 66 36)", mimeType: "application/vnd.openxmlformats-officedocument.presentationml.presentation" },

  ["msg"]: { icon: "envelope", color: "#2980B9", mimeType: "application/vnd.ms-outlook" },
  ["eml"]: { icon: "envelope", color: "#F39C12", mimeType: "message/rfc822" },


  ["pdf"]: { icon: "file-pdf", color: "#b30b00", mimeType: "application/pdf", browserView: true},

  ["html"]: { icon: "file-code", color: "#373377", mimeType: "text/html", browserView: true },
  ["xml"]: { icon: "file-code", color: "#373377", mimeType: "text/xml", browserView: true},
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

  ["zip"]: { icon: "file-zipper", color: "#F5B041", mimeType: "application/zip" },
  ["rar"]: { icon: "file-zipper", color: "#F5B041", mimeType: "application/vnd.rar" },
  ["7z"]: { icon: "file-zipper", color: "#F5B041", mimeType: "application/x-7z-compressed" },

  ["txt"]: { icon: "file-lines", color: "#566573", mimeType: "text/plain", browserView: true },
  ["rtf"]: { icon: "file-lines", color: "#566573", mimeType: "text/plain"  },
  ["info"]: { icon: "file-lines", color: "#566573", mimeType: "text/plain", browserView: true  },
  ["log"]: { icon: "file-lines", color: "#566573", mimeType: "text/plain", browserView: true},

  ["csv"]: { icon: "file-csv", color: "#566573", mimeType: "text/plain"  },

  ["avi"]: { icon: "file-video", color: "red", mimeType: "video/x-msvideo", browserView: true },
  ["mkv"]: { icon: "file-video", color: "red", mimeType: "video/x-matroska", browserView: true },
  ["mpeg"]: { icon: "file-video", color: "red", mimeType: "video/mpeg", browserView: true },
  ["mpg"]: { icon: "file-video", color: "red", mimeType: "video/mpeg", browserView: true},
  ["mp4"]: { icon: "file-video", color: "red", mimeType: "video/mpeg", browserView: true  },
  ["ogg"]: { icon: "file-video", color: "red", mimeType: "video/ogg" },
  ["ogv"]: { icon: "file-video", color: "red", mimeType: "video/ogg" },
  ["mov"]: { icon: "file-video", color: "red", mimeType: "video/quicktime" },
  ["webm"]: { icon: "file-video", color: "red", mimeType: "video/webm" },
  ["wmv"]: { icon: "file-video", color: "red", mimeType: "video/x-ms-asf" },

  ["mp3"]: { icon: "file-audio", color: "#BA4A00", mimeType: "audio/mpeg", browserView: true },
  ["weba"]: { icon: "file-audio", color: "#BA4A00", mimeType: "audio/webm", browserView: true},
  ["wav"]: { icon: "file-audio", color: "#BA4A00", mimeType: "audio/wav", browserView: true},
  ["wma"]: { icon: "file-audio", color: "#BA4A00", mimeType: "audio/x-ms-wma", browserView: true },
};


interface FileThumbnailProps extends React.ImgHTMLAttributes<HTMLImageElement> {
  file: IFile & ModifiableEntity;
}

export function FileThumbnail({ file, ...attrs }: FileThumbnailProps) {
  return <FileImage file={file} onClick={e => ImageModal.show(file, e)} {...attrs} />
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
