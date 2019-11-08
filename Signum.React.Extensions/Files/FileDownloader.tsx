import * as React from 'react'
import * as Services from '@framework/Services'
import * as Navigator from '@framework/Navigator'
import { ModifiableEntity, Lite, Entity, JavascriptMessage, isEntity, isModifiableEntity, getToString } from '@framework/Signum.Entities'
import { IFile, FileEntity, FilePathEntity, FileEmbedded, FilePathEmbedded } from './Signum.Entities.Files'
import * as QueryString from 'query-string'
import { Type } from '@framework/Reflection';
import { isLite } from '@framework/Signum.Entities';
import "./Files.css"

export type DownloadBehaviour = "SaveAs" | "View" | "None";

export interface FileDownloaderProps {
  entityOrLite: ModifiableEntity & IFile | Lite<IFile & Entity>;
  download?: DownloadBehaviour;
  configuration?: FileDownloaderConfiguration<IFile>;
  htmlAttributes?: React.HTMLAttributes<HTMLSpanElement | HTMLAnchorElement>;
  children?: React.ReactNode;
}

export function FileDownloader(p: FileDownloaderProps) {

  function handleOnClick(e: React.MouseEvent<any>) {
    e.preventDefault();
    const entityOrLite = p.entityOrLite;
    var promise = isModifiableEntity(entityOrLite) ? Promise.resolve(entityOrLite) :
      Navigator.API.fetchAndRemember(entityOrLite as Lite<IFile & Entity>);

    promise.then(entity => {

      const configuration = p.configuration ?? configurtions[entity.Type];
      if (!configuration)
        throw new Error("No configuration registered in FileDownloader.configurations for ");

      if (p.download == "SaveAs") {
        if (entity.binaryFile)
          downloadBase64(e, entity.binaryFile, entity.fileName!);
        else
          configuration.downloadClick ? configuration.downloadClick(e, entity) : downloadUrl(e, configuration.fileUrl!(entity));
      } else {
        if (entity.binaryFile)
          viewBase64(e, entity.binaryFile, entity.fileName!);
        else
          configuration.viewClick ? configuration.viewClick(e, entity) : viewUrl(e, configuration.fileUrl!(entity));
      }

    }).done();
  }

  const entityOrLite = p.entityOrLite;

  const toStr = getToString(entityOrLite);

  const fileName = toStr!.tryBeforeLast(" - ") ?? toStr; //Hacky

  return (
    <a
      href="#"
      onClick={handleOnClick}
      download={p.download == "View" ? undefined : fileName}
      title={toStr ?? undefined}
      target="_blank"
      {...p.htmlAttributes}>
      {p.children ?? toStr}
    </a>
  );
}

FileDownloader.defaultProps = {
  download: "SaveAs",
}

export const configurtions: { [typeName: string]: FileDownloaderConfiguration<IFile> } = { };

export function registerConfiguration<T extends IFile & ModifiableEntity>(type: Type<T>, configuration: FileDownloaderConfiguration<T>) {
  configurtions[type.typeName] = configuration as FileDownloaderConfiguration<IFile>;
}

export interface FileDownloaderConfiguration<T extends IFile> {
  fileUrl?: (file: T) => string;
  downloadClick?: (event: React.MouseEvent<any>, file: T) => void;
  viewClick?: (event: React.MouseEvent<any>, file: T) => void;
}

registerConfiguration(FileEntity, {
  fileUrl: file => Navigator.toAbsoluteUrl("~/api/files/downloadFile/" + file.id),
  viewClick: (event, file) => viewUrl(event, Navigator.toAbsoluteUrl("~/api/files/downloadFile/" + file.id))
});

registerConfiguration(FilePathEntity, {
  fileUrl: file => Navigator.toAbsoluteUrl("~/api/files/downloadFilePath/" + file.id),
});

registerConfiguration(FileEmbedded, {
  downloadClick: (event, file) => downloadBase64(event, file.binaryFile!, file.fileName!),
  viewClick: (event, file) => viewBase64(event, file.binaryFile!, file.fileName!)
});

registerConfiguration(FilePathEmbedded, {
  fileUrl: file => Navigator.toAbsoluteUrl(`~/api/files/downloadEmbeddedFilePath/${file.fileType!.key}?` + QueryString.stringify({ suffix: file.suffix, fileName: file.fileName }))
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

  const blob = Services.b64toBlob(binaryFile);

  const url = URL.createObjectURL(blob);

  window.open(url);
};

