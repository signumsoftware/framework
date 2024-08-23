import * as React from 'react'
import * as Services from '@framework/Services'
import * as AppContext from '@framework/AppContext'
import { Navigator } from '@framework/Navigator'
import { ModifiableEntity, Lite, Entity, isModifiableEntity, getToString, EntityControlMessage } from '@framework/Signum.Entities'
import { IFile, FileEntity, FilePathEntity, FileEmbedded, FilePathEmbedded, IFilePath } from '../Signum.Files'
import { FilesClient } from '../FilesClient'
import { Type } from '@framework/Reflection';
import "./Files.css"
import { QueryString } from '@framework/QueryString'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { TypeContext } from '@framework/Lines'

export type DownloadBehaviour = "SaveAs" | "View" | "ViewOrSave" | "None";

export interface FileDownloaderProps {
  entityOrLite: ModifiableEntity & IFile | Lite<IFile & Entity>;
  containerEntity?: ModifiableEntity;
  download?: DownloadBehaviour;
  configuration?: FileDownloaderConfiguration<IFile>;
  htmlAttributes?: React.HTMLAttributes<HTMLSpanElement | HTMLAnchorElement>;
  children?: React.ReactNode | ((info: FilesClient.ExtensionInfo | undefined) => React.ReactNode)
  showFileIcon?: boolean;
}

const units = ["Bytes", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB"];

export function getFileName(toStr: string): string {
  if (units.some(u => toStr.endsWith(" " + u)))
    return toStr.beforeLast(" - ");

  return toStr;
}

export function FileDownloader(p: FileDownloaderProps): React.JSX.Element {

  function handleOnClick(e: React.MouseEvent, save: boolean) {
    const entityOrLite = p.entityOrLite;
    const promise = isModifiableEntity(entityOrLite) ? Promise.resolve(entityOrLite) :
      Navigator.API.fetchAndRemember(entityOrLite as Lite<IFile & Entity>);

    promise.then(entity => {

      const configuration = p.configuration ?? configurations[entity.Type];
      if (!configuration)
        throw new Error("No configuration registered in FileDownloader.configurations for ");

      const container = p.containerEntity;

      if (save) {
        if (entity.binaryFile)
          downloadBase64(e, entity.binaryFile, entity.fileName!);
        else
          configuration.downloadClick ? configuration.downloadClick(e, entity, container) : downloadUrl(e, configuration.fileUrl!(entity, container));
      } else {
        if (entity.binaryFile) {
          viewBase64(e, entity.binaryFile, entity.fileName!); //view without mime type is problematic
        }
        else
          configuration.viewClick ? configuration.viewClick(e, entity, container) : viewUrl(e, configuration.fileUrl!(entity, container));
      }

    });
  }

  const entityOrLite = p.entityOrLite;

  const toStr = getToString(entityOrLite);

  const fileName = getFileName(toStr); //Hacky

  const info: FilesClient.ExtensionInfo | undefined = FilesClient.extensionInfo[fileName.tryAfterLast(".")?.toLowerCase()!]

  function getChildren(){
    return !p.children ? null : (typeof p.children === 'function') ? p.children(info) : p.children
  }

  return (
    <div {...p.htmlAttributes}>
      <a
        href="#"
        onClick={e => {
          e.preventDefault();
          handleOnClick(e, p.download == "SaveAs" || p.download == "ViewOrSave" && !(info?.browserView));
        }}
        title={toStr ?? undefined}
        target="_blank"
      >
        {getChildren() ??
          <>
          {p.showFileIcon && <FontAwesomeIcon className="me-1"
            icon={info?.icon ?? "file"}
            color={info?.color ?? "grey"} />}
            {toStr}
          </>}
      </a>
      {p.download == "ViewOrSave" &&
        <a href="#"
          className="sf-view sf-line-button"          
          onClick={e => {
            e.preventDefault();
            handleOnClick(e, true);
          }}>
          <FontAwesomeIcon className="ms-1 sf-pointer" icon={"download"} title={EntityControlMessage.Download.niceToString()}/>
        </a>
      }
    </div>
  );
}
export declare namespace FileDownloader {
    export var defaultProps: {
        download: string
        showFileIcon: boolean
    }
}

FileDownloader.defaultProps = {
  download: "ViewOrSave",
  showFileIcon: true,
}

export const configurations: { [typeName: string]: FileDownloaderConfiguration<IFile> } = {};

export function registerConfiguration<T extends IFile & ModifiableEntity>(type: Type<T>, configuration: FileDownloaderConfiguration<T>): void {
  configurations[type.typeName] = configuration as FileDownloaderConfiguration<IFile>;
}

export function getConfiguration<T extends IFile & ModifiableEntity>(type: Type<T>): FileDownloaderConfiguration<T> | undefined {
  return configurations[type.typeName] as FileDownloaderConfiguration<T> | undefined
}

export interface FileDownloaderConfiguration<T extends IFile> {
  fileUrl?: (file: T, container?: ModifiableEntity) => string;
  fileLiteUrl?: (file: Lite<T & Entity>, container?: ModifiableEntity) => string;
  downloadClick?: (event: React.MouseEvent<any>, file: T, container?: ModifiableEntity) => void;
  viewClick?: (event: React.MouseEvent<any>, file: T, container?: ModifiableEntity) => void;
}

registerConfiguration(FileEntity, {
  fileUrl: file => AppContext.toAbsoluteUrl("/api/files/downloadFile/" + file.id),
  fileLiteUrl: file => AppContext.toAbsoluteUrl("/api/files/downloadFile/" + file.id),
});

registerConfiguration(FilePathEntity, {
  fileUrl: file => AppContext.toAbsoluteUrl(`/api/files/downloadFilePath/${file.id}?${QueryString.stringify({ hash: file.hash })}`),
  fileLiteUrl: file => AppContext.toAbsoluteUrl("/api/files/downloadFilePath/" + file.id),
});

registerConfiguration(FileEmbedded, {
  downloadClick: (event, file) => downloadBase64(event, file.binaryFile!, file.fileName!),
  viewClick: (event, file) => viewBase64(event, file.binaryFile!, file.fileName!)
});

registerConfiguration(FilePathEmbedded, {
  fileUrl: file => AppContext.toAbsoluteUrl(
    `/api/files/downloadEmbeddedFilePath/${file.rootType}/${file.entityId}?${QueryString.stringify({ route: file.propertyRoute, rowId: file.mListRowId, hash: file.hash })}`)
});

export function downloadFile(file: IFilePath & ModifiableEntity): Promise<Response> {
  var fileUrl = configurations[file.Type].fileUrl!(file);
  return Services.ajaxGetRaw({ url: fileUrl });
}

export function downloadUrl(e: React.MouseEvent<any>, url: string): void {

  e.preventDefault();
  Services.ajaxGetRaw({ url: url })
    .then(resp => Services.saveFile(resp));
};

export function viewUrl(e: React.MouseEvent<any>, url: string): void {

  e.preventDefault();
  const win = window.open();
  if (!win)
    return;

  Services.ajaxGetRaw({ url: url })
    .then(resp => resp.blob())
    .then(blob => {
      const url = URL.createObjectURL(blob);
      win.location.assign(url);
    });

}

function downloadBase64(e: React.MouseEvent<any>, binaryFile: string, fileName: string) {
  e.preventDefault();

  const blob = Services.b64toBlob(binaryFile);

  Services.saveFileBlob(blob, fileName);
};

function viewBase64(e: React.MouseEvent<any>, binaryFile: string, fileName: string) {
  e.preventDefault();

  const info = FilesClient.extensionInfo[fileName.tryAfterLast(".")?.toLocaleLowerCase()!];

  const blob = Services.b64toBlob(binaryFile, info?.mimeType);

  const url = URL.createObjectURL(blob);

  window.open(url);
};

