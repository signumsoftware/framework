import * as React from 'react'
import * as Services from '@framework/Services'
import * as Navigator from '@framework/Navigator'
import { ModifiableEntity, Lite, Entity, JavascriptMessage } from '@framework/Signum.Entities'
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
  htmlAttributes: React.HTMLAttributes<HTMLSpanElement | HTMLAnchorElement>
}

export default class FileDownloader extends React.Component<FileDownloaderProps> {

  static configurtions: { [typeName: string]: FileDownloaderConfiguration<IFile> } = {};

  static registerConfiguration<T extends IFile & ModifiableEntity>(type: Type<T>, configuration: FileDownloaderConfiguration<T>) {
    FileDownloader.configurtions[type.typeName] = configuration as FileDownloaderConfiguration<IFile>;
  }


  static defaultProps = {
    download: "SaveAs",
  }

  componentWillMount() {
    const entityOrLite = this.props.entityOrLite;
    if (entityOrLite && (entityOrLite as Lite<IFile & Entity>).EntityType)
      Navigator.API.fetchAndRemember(entityOrLite as Lite<IFile & Entity>)
        .then(() => this.forceUpdate())
        .done();
  }



  render() {

    const entityOrLite = this.props.entityOrLite;

    const entity = isLite(entityOrLite) ? entityOrLite.entity : entityOrLite;

    if (!entity)
      return <span {...this.props.htmlAttributes}>{JavascriptMessage.loading.niceToString()}</span>;


    const configuration = this.props.configuration || FileDownloader.configurtions[entity.Type];
    if (!configuration)
      throw new Error("No configuration registered in FileDownloader.configurations for ");

    return (
      <a
        href="#"
        onClick={e => {
          e.preventDefault();
          if (this.props.download == "SaveAs") {
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
        }}
        download={this.props.download == "View" ? undefined : entity.fileName}
        title={entity.fileName || undefined}
        target="_blank"
        {...this.props.htmlAttributes}>
        {entity.fileName}
      </a>
    );

  }
}

export interface FileDownloaderConfiguration<T extends IFile> {
  fileUrl?: (file: T) => string;
  downloadClick?: (event: React.MouseEvent<any>, file: T) => void;
  viewClick?: (event: React.MouseEvent<any>, file: T) => void;
}

FileDownloader.registerConfiguration(FileEntity, {
  fileUrl: file => Navigator.toAbsoluteUrl("~/api/files/downloadFile/" + file.id),
  viewClick: (event, file) => viewUrl(event, Navigator.toAbsoluteUrl("~/api/files/downloadFile/" + file.id))
});

FileDownloader.registerConfiguration(FilePathEntity, {
  fileUrl: file => Navigator.toAbsoluteUrl("~/api/files/downloadFilePath/" + file.id),
});

FileDownloader.registerConfiguration(FileEmbedded, {
  downloadClick: (event, file) => downloadBase64(event, file.binaryFile!, file.fileName!),
  viewClick: (event, file) => viewBase64(event, file.binaryFile!, file.fileName!)
});

FileDownloader.registerConfiguration(FilePathEmbedded, {
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

