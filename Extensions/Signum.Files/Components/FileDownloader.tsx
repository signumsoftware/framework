import * as React from 'react'
import * as Services from '@framework/Services'
import * as AppContext from '@framework/AppContext'
import { Navigator } from '@framework/Navigator'
import { ModifiableEntity, Lite, Entity, isModifiableEntity, getToString, EntityControlMessage, isLite } from '@framework/Signum.Entities'
import { IFile, FileEntity, FilePathEntity, FileEmbedded, FilePathEmbedded, IFilePath } from '../Signum.Files'
import { FilesClient } from '../FilesClient'
import { Type } from '@framework/Reflection';
import "./Files.css"
import { QueryString } from '@framework/QueryString'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { TypeContext } from '@framework/Lines'
import { LinkButton } from '@framework/Basics/LinkButton'

export type DownloadBehaviour = "SaveAs" | "View" | "ViewOrSave" | "None";

export interface FileDownloaderProps {
  entityOrLite: ModifiableEntity & IFile | Lite<IFile & Entity>;
  containerEntity?: ModifiableEntity;
  download?: DownloadBehaviour;
  configuration?: FileDownloaderConfiguration<IFile>;
  htmlAttributes?: React.HTMLAttributes<HTMLSpanElement | HTMLAnchorElement>;
  children?: React.ReactNode | ((info: FilesClient.ExtensionInfo | undefined) => React.ReactNode)
  showFileIcon?: boolean;
  hideFileName?: boolean;
}

const units = ["Bytes", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB"];

export function getFileName(toStr: string): string {
  if (units.some(u => toStr.endsWith(" " + u)))
    return toStr.beforeLast(" - ");

  return toStr;
}

export function toComputerSize(value: number): string {
  let size = value;
  let i = 0;

  while (i < units.length && size >= 1024) {
    size /= 1024;
    i++;
  }

  return `${size.toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 })} ${units[i]}`;
}

export function FileDownloader(p: FileDownloaderProps): React.JSX.Element {

  const entityOrLite = p.entityOrLite;
  const configuration = p.configuration ?? configurations[isLite(entityOrLite) ? entityOrLite.EntityType : entityOrLite.Type];
  if (!configuration)
    throw new Error("No configuration registered in FileDownloader.configurations for ");

  function handleOnClick(e: React.MouseEvent, save: boolean) {

    const promise = isModifiableEntity(entityOrLite) ? Promise.resolve(entityOrLite) :
      Navigator.API.fetchAndRemember(entityOrLite as Lite<IFile & Entity>);

    promise.then(entity => {

      const container = p.containerEntity;

      if (save) {
        if (entity.binaryFile)
          downloadBase64(e, entity.binaryFile, entity.fileName!);
        else if (configuration.downloadClick != null)
          configuration.downloadClick(e, entity, container);
        else
          downloadUrl(e, configuration.fileUrl!(entity, container));

      } else {

        if (entity.binaryFile) 
          viewBase64(e, entity.binaryFile, entity.fileName!); //view without mime type is problematic
        else if (configuration.viewClick)
          configuration.viewClick(e, entity, container);
        else
          viewUrl(e, configuration.fileUrl!(entity, container));
      }

    });
  }

  const toStr = getToString(entityOrLite);

  const fileName = getFileName(toStr); //Hacky

  const info: FilesClient.ExtensionInfo | undefined = FilesClient.extensionInfo[fileName.tryAfterLast(".")?.toLowerCase()!]

  function getChildren(){
    return !p.children ? null : (typeof p.children === 'function') ? p.children(info) : p.children
  }

  const download = p.download ?? "ViewOrSave";

  const children =  getChildren() ??
    <>
      {(p.showFileIcon ?? true) && <FontAwesomeIcon className="me-1"
        icon={info?.icon ?? "file"}
        color={info?.color ?? "grey"} />}
      {!p.hideFileName && toStr}
    </>
;

  const fullWebPAth = isModifiableEntity(p.entityOrLite) ? (p.entityOrLite as IFile as IFilePath).fullWebPath : undefined;

  if (fullWebPAth) {
    return (
      <div {...p.htmlAttributes}>
        <a
          href={fullWebPAth}
          title={toStr ?? undefined}
          target="_blank"
        >
          {children}
        </a>
        {p.download == "ViewOrSave" && <a href={fullWebPAth}
          download={(p.entityOrLite as ModifiableEntity & IFilePath).fileName}
            className="sf-view sf-line-button">
            <FontAwesomeIcon className="ms-1 sf-pointer" icon={"download"} title={EntityControlMessage.Download.niceToString()} />
          </a>
        }
      </div>
    );
  }

  const enabled = configuration.canDownload == null ||
    (!isLite(p.entityOrLite) ? configuration.canDownload(p.entityOrLite) :
      p.entityOrLite.entity == null || configuration.canDownload(p.entityOrLite.entity));

  return (
    <div {...p.htmlAttributes}>
      <LinkButton
        href={!enabled ? undefined :  "#"}
        onClick={!enabled ? undefined : e => {
          handleOnClick(e, download == "SaveAs" || download == "ViewOrSave" && !(info?.browserView));
        }}
        title={toStr ?? undefined}
        target="_blank">
        {children}
      </LinkButton>
      {p.download == "ViewOrSave" && enabled &&
        <LinkButton 
          title={EntityControlMessage.Download.niceToString()}
          className="sf-view sf-line-button ms-1"          
          onClick={e => {
            handleOnClick(e, true);
          }}>
          <FontAwesomeIcon icon={"download"}/>
        </LinkButton>
      }
    </div>
  );
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
  canDownload?: (file: T) => boolean;
}

registerConfiguration(FileEntity, {
  fileUrl: file => AppContext.toAbsoluteUrl("/api/files/downloadFile/" + file.id),
  fileLiteUrl: file => AppContext.toAbsoluteUrl("/api/files/downloadFile/" + file.id),
});

registerConfiguration(FilePathEntity, {
  canDownload: f => f.binaryFile != null || f.fullWebPath != null || f.id != null,
  fileUrl: file => file.fullWebPath ?? AppContext.toAbsoluteUrl(`/api/files/downloadFilePath/${file.id}?${QueryString.stringify({ hash: file.hash })}`),
  fileLiteUrl: file => AppContext.toAbsoluteUrl("/api/files/downloadFilePath/" + file.id),
});

registerConfiguration(FileEmbedded, {
  downloadClick: (event, file) => downloadBase64(event, file.binaryFile!, file.fileName!),
  viewClick: (event, file) => viewBase64(event, file.binaryFile!, file.fileName!)
});

registerConfiguration(FilePathEmbedded, {
  canDownload: f => f.binaryFile != null || f.fullWebPath != null || f.entityId != null,
  fileUrl: file => file.fullWebPath ?? AppContext.toAbsoluteUrl(
    `/api/files/downloadEmbeddedFilePath/${file.rootType}/${file.entityId}?${QueryString.stringify({ route: file.propertyRoute, rowId: file.mListRowId, hash: file.hash })}`),
});

export function downloadFile(file: IFilePath & ModifiableEntity): Promise<Response> {
  var fileUrl = configurations[file.Type].fileUrl!(file);
  return Services.ajaxGetRaw({ url: fileUrl });
}

export function downloadUrl(e: React.MouseEvent<any>, url: string): void {

  Services.ajaxGetRaw({ url: url })
    .then(resp => Services.saveFile(resp));
};

export function viewUrl(e: React.MouseEvent<any>, url: string): void {

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

  const blob = Services.b64toBlob(binaryFile);

  Services.saveFileBlob(blob, fileName);
};

function viewBase64(e: React.MouseEvent<any>, binaryFile: string, fileName: string) {

  const info = FilesClient.extensionInfo[fileName.tryAfterLast(".")?.toLocaleLowerCase()!];

  const blob = Services.b64toBlob(binaryFile, info?.mimeType);

  const url = URL.createObjectURL(blob);

  window.open(url);
};

