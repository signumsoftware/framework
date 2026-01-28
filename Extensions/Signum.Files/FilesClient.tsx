import * as React from 'react'
import { RouteObject } from 'react-router'
import { Finder } from '@framework/Finder'
import { Navigator } from '@framework/Navigator'
import { Type, PropertyRoute, getTypeName, GraphExplorer } from '@framework/Reflection'
import { AutoLine } from '@framework/Lines/AutoLine'
import { FileEntity, FilePathEntity, FileEmbedded, FilePathEmbedded, IFile, IFilePath, FileMessage } from './Signum.Files'
import { FileLine } from './Components/FileLine'
import { ModifiableEntity, Lite, Entity, isLite, registerToString, MList, isModifiableEntity } from "@framework/Signum.Entities";
import { FileImageLine } from './Components/FileImageLine';
import { MultiFileLine } from './Components/MultiFileLine';
import { FileDownloader } from './Components/FileDownloader';
import { FetchInState } from '@framework/Lines/Retrieve';
import { FileImage } from './Components/FileImage';
import { ImageModal } from './Components/ImageModal';
import { IconName, IconProp } from '@fortawesome/fontawesome-svg-core';
import { ChangeLogClient } from '@framework/Basics/ChangeLogClient'
import { TypeContext } from '@framework/Lines'
import { ajaxPost, ajaxPostRaw, ajaxPostUpload } from '@framework/Services'
import { QueryString } from '@framework/QueryString'
import { DeleteLogsTypeOverridesEmbedded } from '@framework/Signum.Basics'

export namespace FilesClient {
  
  export function start(options: { routes: RouteObject[] }): void {
  
    ChangeLogClient.registerChangeLogModule("Signum.Files", () => import("./Changelog"));
  
    registerAutoFileLine(FileEntity);
    registerAutoFileLine(FileEmbedded);
  
    registerAutoFileLine(FilePathEntity);
    registerAutoFileLine(FilePathEmbedded);
  
    registerToString(FileEntity, f => f.toStr ?? f.fileName);
    registerToString(FileEmbedded, f => f.toStr ?? f.fileName);
    registerToString(FilePathEntity, f => f.toStr ?? f.fileName);
    registerToString(FilePathEmbedded, f => f.toStr ?? f.fileName);

    GraphExplorer.onModifiable = (e, obj) => {
      if (e.mode == "clean") {
        const fp = uploadingInProgress(obj);
        if (fp != null)
          throw new Error(FileMessage.File0IsStillUploading.niceToString(fp.fileName));
      }
    };
  }

  export const fileEntityTypeNames: Record<string, boolean> = {};;
  function registerAutoFileLine(type: Type<IFile & ModifiableEntity>) {

    fileEntityTypeNames[getTypeName(type)] = true;

    AutoLine.registerComponent(type.typeName, (tr, pr) => { 
      if (tr.isCollection)
        return ({ ctx, ...rest }) => <MultiFileLine ctx={ctx as any} {...rest as any} />;
  
      var m = pr?.member;
      if (m?.defaultFileTypeInfo && m.defaultFileTypeInfo.onlyImages)
        return ({ ctx, ...rest }) => <FileImageLine ctx={ctx as any} imageHtmlAttributes={{ style: { maxWidth: '100%', maxHeight: '100%' } }} {...rest as any} />;
  
      return ({ ctx, ...rest }) => <FileLine ctx={ctx as any} {...rest as any} />; 
    });
  
    Finder.formatRules.push({
      name: type.typeName + "_Download",
      isApplicable: qt => qt.type.name == type.typeName && !isImage(qt.propertyRoute),
      formatter: qt => new Finder.CellFormatter(cell => cell ? <FileDownloader entityOrLite={cell} htmlAttributes={{ className: "try-no-wrap" }} /> : undefined, true)
    });
  
    Finder.formatRules.push({
      name: type.typeName + "_Image",
      isApplicable: qt => qt.type.name == type.typeName && isImage(qt.propertyRoute),
      formatter: c => new Finder.CellFormatter(cell => !cell ? undefined :
        isLite(cell) ? <FetchInState lite={cell as Lite<IFile & Entity>}>{e => <FileThumbnail file={e as IFile & ModifiableEntity} />}</FetchInState> :
          <FileThumbnail file={cell as IFile & ModifiableEntity} />, false)
    });
  }

  export function uploadingInProgress(obj: unknown): IFilePath | undefined {
    if (isLite(obj)) {
      return uploadingInProgress(obj.entity);
    }

    if (isModifiableEntity(obj) && (obj as unknown as IFilePath).__uploadingOffset != null)
      return obj as unknown as IFilePath;

    return undefined;
  }
  
  export interface ExtensionInfo {
    icon: IconProp;
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
  
  

  function isImage(propertyRoute: string | undefined) {
  
    if (propertyRoute == null)
      return false;
  
    let pr = PropertyRoute.parseFull(propertyRoute);
  
    if (pr.propertyRouteType == "MListItem")
      pr = pr.parent!;
  
    return Boolean(pr?.member?.defaultFileTypeInfo?.onlyImages);
  }


  export namespace API {
    export function startUpload(request: StartUploadRequest): Promise<StartUploadResponse> {
      return ajaxPost({ url: `/api/files/startUpload` }, request);
    }

    export function uploadChunk(blob: Blob, query: UploadChunkQuery, signal?: AbortSignal): Promise<ChunkInfo> {
      return ajaxPostUpload({ url: "/api/files/uploadChunk?" + QueryString.stringify({ ...query }), signal: signal }, blob);
    }

    export function finishUpload(request: FinishUploadRequest): Promise<FinishUploadResponse> {
      return ajaxPost({ url: "/api/files/finishUpload" }, request);
    }

    export function abortUpload(request: AbortUploadRequest): Promise<AbortUploadResponse> {
      return ajaxPost({ url: "/api/files/abortUpload" }, request);
    }
  }

  export interface ChunkInfo {
    partialHash: string;
    blockId: string;
  }

  export interface StartUploadRequest {
    fileTypeKey: string;
    fileName: string;
    type: string;
  }

  export interface StartUploadResponse {
    suffix: string;
    uploadId?: string;
  }

  export interface UploadChunkQuery {
    fileTypeKey: string;
    fileName: string;
    suffix: string;
    type: string;
    chunkIndex: number;
    uploadId?: string;
  }

  export interface FinishUploadRequest {
    fileTypeKey: string;
    fileName: string;
    suffix: string;
    type: string;
    chunks: ChunkInfo[];
    uploadId?: string;
  }

  export interface FinishUploadResponse {
    fileLength: number;
    hash: string;
    fullWebPath?: string;
  }

  export interface AbortUploadRequest {
    fileTypeKey: string;
    fileName: string;
    suffix: string;
    type: string;
    uploadId?: string;
  }

  export interface AbortUploadResponse {
    success: boolean;
    message?: string;
  }
  
}


interface FileThumbnailProps extends React.ImgHTMLAttributes<HTMLImageElement> {
  file: IFile & ModifiableEntity;
}

export function FileThumbnail({ file, ...attrs }: FileThumbnailProps): React.JSX.Element {

  const style = attrs?.style ?? { maxWidth: "150px" };

  return <FileImage file={file} onClick={e => ImageModal.show(file, e)} {...attrs} style={style} />
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
