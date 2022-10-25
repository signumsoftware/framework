import * as React from 'react'
import * as Services from '@framework/Services'
import * as AppContext from '@framework/AppContext'
import * as Navigator from '@framework/Navigator'
import { ModifiableEntity, Lite, Entity, isModifiableEntity, getToString } from '@framework/Signum.Entities'
import { IFile, FileEntity, FilePathEntity, FileEmbedded, FilePathEmbedded, IFilePath } from './Signum.Entities.Files'
import { ExtensionInfo, extensionInfo } from './FilesClient'
import { Type } from '@framework/Reflection';
import "./Files.css"
import { QueryString } from '@framework/QueryString'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'

export type DownloadBehaviour = "SaveAs" | "View" | "ViewOrSave" | "None";

export interface FileDownloaderProps {
  entityOrLite: ModifiableEntity & IFile | Lite<IFile & Entity>;
  download?: DownloadBehaviour;
  configuration?: FileDownloaderConfiguration<IFile>;
  htmlAttributes?: React.HTMLAttributes<HTMLSpanElement | HTMLAnchorElement>;
  children?: React.ReactNode | ((info: ExtensionInfo | undefined) => React.ReactNode)
  showFileIcon?: boolean;
}

const units = ["Bytes", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB"];

export function getFileName(toStr: string) {
  if (units.some(u => toStr.endsWith(" " + u)))
    return toStr.beforeLast(" - ");

  return toStr;
}

export function FileDownloader(p: FileDownloaderProps) {

  function handleOnClick(e: React.MouseEvent, save: boolean) {
    const entityOrLite = p.entityOrLite;
    const promise = isModifiableEntity(entityOrLite) ? Promise.resolve(entityOrLite) :
      Navigator.API.fetchAndRemember(entityOrLite as Lite<IFile & Entity>);

    promise.then(entity => {

      const configuration = p.configuration ?? configurations[entity.Type];
      if (!configuration)
        throw new Error("No configuration registered in FileDownloader.configurations for ");

      if (save) {
        if (entity.binaryFile)
          downloadBase64(e, entity.binaryFile, entity.fileName!);
        else
          configuration.downloadClick ? configuration.downloadClick(e, entity) : downloadUrl(e, configuration.fileUrl!(entity));
      } else {
        if (entity.binaryFile) {
          viewBase64(e, entity.binaryFile, entity.fileName!); //view without mime type is problematic
        }
        else
          configuration.viewClick ? configuration.viewClick(e, entity) : viewUrl(e, configuration.fileUrl!(entity));
      }

    });
  }

  const entityOrLite = p.entityOrLite;

  const toStr = getToString(entityOrLite);

  const fileName = getFileName(toStr); //Hacky

  const info: ExtensionInfo | undefined = extensionInfo[fileName.tryAfterLast(".")?.toLowerCase()!]

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
            {p.showFileIcon && <FontAwesomeIcon className="me-1" icon={["far", info?.icon ?? "file"]} color={info?.color ?? "grey"} />}
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
          <FontAwesomeIcon className="ms-1 sf-pointer" icon={["fas", "download"]} />
        </a>
      }
    </div>
  );
}

FileDownloader.defaultProps = {
  download: "ViewOrSave",
  showFileIcon: true,
}

export const configurations: { [typeName: string]: FileDownloaderConfiguration<IFile> } = {};

export function registerConfiguration<T extends IFile & ModifiableEntity>(type: Type<T>, configuration: FileDownloaderConfiguration<T>) {
  configurations[type.typeName] = configuration as FileDownloaderConfiguration<IFile>;
}

export function getConfiguration<T extends IFile & ModifiableEntity>(type: Type<T>): FileDownloaderConfiguration<T> | undefined {
  return configurations[type.typeName] as FileDownloaderConfiguration<T> | undefined
}

export interface FileDownloaderConfiguration<T extends IFile> {
  fileUrl?: (file: T) => string;
  fileLiteUrl?: (file: Lite<T & Entity>) => string;
  downloadClick?: (event: React.MouseEvent<any>, file: T) => void;
  viewClick?: (event: React.MouseEvent<any>, file: T) => void;
}

registerConfiguration(FileEntity, {
  fileUrl: file => AppContext.toAbsoluteUrl("~/api/files/downloadFile/" + file.id),
  fileLiteUrl: file => AppContext.toAbsoluteUrl("~/api/files/downloadFile/" + file.id),
});

registerConfiguration(FilePathEntity, {
  fileUrl: file => AppContext.toAbsoluteUrl("~/api/files/downloadFilePath/" + file.id),
  fileLiteUrl: file => AppContext.toAbsoluteUrl("~/api/files/downloadFilePath/" + file.id),
});

registerConfiguration(FileEmbedded, {
  downloadClick: (event, file) => downloadBase64(event, file.binaryFile!, file.fileName!),
  viewClick: (event, file) => viewBase64(event, file.binaryFile!, file.fileName!)
});

registerConfiguration(FilePathEmbedded, {
  fileUrl: file => AppContext.toAbsoluteUrl(`~/api/files/downloadEmbeddedFilePath/${file.rootType}/${file.entityId}?${QueryString.stringify({ route: file.propertyRoute, rowId: file.mListRowId })}`)
});

export function downloadFile(file: IFilePath & ModifiableEntity): Promise<Response> {
  var fileUrl = configurations[file.Type].fileUrl!(file);
  return Services.ajaxGetRaw({ url: fileUrl });
}

export function downloadUrl(e: React.MouseEvent<any>, url: string) {

  e.preventDefault();
  Services.ajaxGetRaw({ url: url })
    .then(resp => Services.saveFile(resp));
};

export function viewUrl(e: React.MouseEvent<any>, url: string) {

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

  const info = extensionInfo[fileName.tryAfterLast(".")?.toLocaleLowerCase()!];

  const blob = Services.b64toBlob(binaryFile, info?.mimeType);

  const url = URL.createObjectURL(blob);

  window.open(url);
};

