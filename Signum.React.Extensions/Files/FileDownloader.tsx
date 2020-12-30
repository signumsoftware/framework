import * as React from 'react'
import * as Services from '@framework/Services'
import * as AppContext from '@framework/AppContext'
import * as Navigator from '@framework/Navigator'
import { ModifiableEntity, Lite, Entity, isModifiableEntity, getToString } from '@framework/Signum.Entities'
import { IFile, FileEntity, FilePathEntity, FileEmbedded, FilePathEmbedded } from './Signum.Entities.Files'
import { extensionInfo } from './FilesClient'
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
  children?: React.ReactNode;
  showFileIcon?: boolean;
}

const units = ["Bytes", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB"];

function getFileName(toStr: string) {
  if (units.some(u => toStr.endsWith(" " + u)))
    return toStr.beforeLast(" - ");

  return toStr;
}

export function FileDownloader(p: FileDownloaderProps) {

  function handleOnClick(e: React.MouseEvent, save: boolean) {
    const entityOrLite = p.entityOrLite;
    var promise = isModifiableEntity(entityOrLite) ? Promise.resolve(entityOrLite) :
      Navigator.API.fetchAndRemember(entityOrLite as Lite<IFile & Entity>);

    promise.then(entity => {

      const configuration = p.configuration ?? configurtions[entity.Type];
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

    }).done();
  }

  const entityOrLite = p.entityOrLite;

  const toStr = getToString(entityOrLite);

  const fileName = getFileName(toStr); //Hacky

  var info = extensionInfo[fileName.tryAfterLast(".")?.toLowerCase()!]

  return (
    <div {...p.htmlAttributes}>
      <a
        href="#"
        onClick={e => {
          e.preventDefault();
          handleOnClick(e, p.download == "SaveAs");
        }}
        title={toStr ?? undefined}
        target="_blank"
      >
        {p.children ??
          <>
            {p.showFileIcon && <FontAwesomeIcon className="mr-1" icon={["far", info?.icon ?? "file"]} color={info?.color ?? "grey"} />}
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
          <FontAwesomeIcon className="ml-1 sf-pointer" icon={["fas", "download"]} />
        </a>
      }
    </div>
  );
}

FileDownloader.defaultProps = {
  download: "ViewOrSave",
  showFileIcon: true,
}

export const configurtions: { [typeName: string]: FileDownloaderConfiguration<IFile> } = {};

export function registerConfiguration<T extends IFile & ModifiableEntity>(type: Type<T>, configuration: FileDownloaderConfiguration<T>) {
  configurtions[type.typeName] = configuration as FileDownloaderConfiguration<IFile>;
}

export interface FileDownloaderConfiguration<T extends IFile> {
  fileUrl?: (file: T) => string;
  downloadClick?: (event: React.MouseEvent<any>, file: T) => void;
  viewClick?: (event: React.MouseEvent<any>, file: T) => void;
}

registerConfiguration(FileEntity, {
  fileUrl: file => AppContext.toAbsoluteUrl("~/api/files/downloadFile/" + file.id),
  viewClick: (event, file) => viewUrl(event, AppContext.toAbsoluteUrl("~/api/files/downloadFile/" + file.id))
});

registerConfiguration(FilePathEntity, {
  fileUrl: file => AppContext.toAbsoluteUrl("~/api/files/downloadFilePath/" + file.id),
});

registerConfiguration(FileEmbedded, {
  downloadClick: (event, file) => downloadBase64(event, file.binaryFile!, file.fileName!),
  viewClick: (event, file) => viewBase64(event, file.binaryFile!, file.fileName!)
});

registerConfiguration(FilePathEmbedded, {
  fileUrl: file => AppContext.toAbsoluteUrl(`~/api/files/downloadEmbeddedFilePath/${file.rootType}/${file.entityId}?${QueryString.stringify({ route: file.propertyRoute, rowId: file.mListRowId })}`)
});

function downloadUrl(e: React.MouseEvent<any>, url: string) {

  e.preventDefault();
  Services.ajaxGetRaw({ url: url })
    .then(resp => Services.saveFile(resp))
    .done();
};

function viewUrl(e: React.MouseEvent<any>, url: string) {

  e.preventDefault();
  const win = window.open();
  if (!win)
    return;

  Services.ajaxGetRaw({ url: url })
    .then(resp => resp.blob())
    .then(blob => {
      const url = URL.createObjectURL(blob);
      win.location.assign(url);
    })
    .done();

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

