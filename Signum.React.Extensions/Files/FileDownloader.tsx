import * as React from 'react'
import { Link } from 'react-router-dom'
import { classes, Dic } from '../../../Framework/Signum.React/Scripts/Globals'
import * as Services from '../../../Framework/Signum.React/Scripts/Services'
import * as Navigator from '../../../Framework/Signum.React/Scripts/Navigator'
import * as Constructor from '../../../Framework/Signum.React/Scripts/Constructor'
import * as Finder from '../../../Framework/Signum.React/Scripts/Finder'
import { FindOptions } from '../../../Framework/Signum.React/Scripts/FindOptions'
import { TypeContext, StyleContext, StyleOptions, FormGroupStyle } from '../../../Framework/Signum.React/Scripts/TypeContext'
import { PropertyRoute, PropertyRouteType, MemberInfo, getTypeInfo, getTypeInfos, TypeInfo, IsByAll, New, getTypeName } from '../../../Framework/Signum.React/Scripts/Reflection'
import { LineBase, LineBaseProps } from '../../../Framework/Signum.React/Scripts/Lines/LineBase'
import { ModifiableEntity, Lite, Entity, EntityControlMessage, JavascriptMessage, toLite, is, liteKey, getToString } from '../../../Framework/Signum.React/Scripts/Signum.Entities'
import { IFile, IFilePath, FileMessage, FileTypeSymbol, FileEntity, FilePathEntity, FileEmbedded, FilePathEmbedded } from './Signum.Entities.Files'
import { EntityBase, EntityBaseProps } from '../../../Framework/Signum.React/Scripts/Lines/EntityBase'
import * as QueryString from 'query-string'

import "./Files.css"
import { Type } from '../../../Framework/Signum.React/Scripts/Reflection';


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

        const entity = (entityOrLite as Lite<IFile & Entity>).EntityType ?
            (entityOrLite as Lite<IFile & Entity>).entity :
            (entityOrLite as IFile & Entity);

        if (!entity)
            return <span {...this.props.htmlAttributes}>{JavascriptMessage.loading.niceToString()}</span>;


        const configuration = this.props.configuration || FileDownloader.configurtions[entity.Type];
        if (!configuration)
            throw new Error("No configuration registered in FileDownloader.configurations for "); 

        return (
            <a
                href="#"
                onClick={e => entity.binaryFile ? downloadBase64(e, entity.binaryFile, entity.fileName!) : configuration.downloadClick(e, entity)}
                download={this.props.download == "View" ? undefined : entity.fileName }
                title={entity.fileName || undefined}
                target="_blank"
                {...this.props.htmlAttributes}>
                {entity.fileName}
            </a>
        );

    }
}


export interface FileDownloaderConfiguration<T extends IFile> {
    downloadClick: (event: React.MouseEvent<any>, file: T) => void;
}

FileDownloader.registerConfiguration(FileEntity, {
    downloadClick: (event, file) => downloadUrl(event, Navigator.toAbsoluteUrl("~/api/files/downloadFile/" + file.id.toString()))
});

FileDownloader.registerConfiguration(FilePathEntity, {
    downloadClick: (event, file) => downloadUrl(event, Navigator.toAbsoluteUrl("~/api/files/downloadFilePath/" + file.id.toString()))
});

FileDownloader.registerConfiguration(FileEmbedded, {
    downloadClick: (event, file) => downloadBase64(event, file.binaryFile!, file.fileName!)
});

FileDownloader.registerConfiguration(FilePathEmbedded, {
    downloadClick: (event, file) => downloadUrl(event,
        Navigator.toAbsoluteUrl(`~/api/files/downloadEmbeddedFilePath/${file.fileType!.key}?` + 
            QueryString.stringify({ suffix: file.suffix, fileName: file.fileName })))
});

function downloadUrl(e: React.MouseEvent<any>, url: string) {
    
    e.preventDefault();
    Services.ajaxGetRaw({ url: url })
        .then(resp => Services.saveFile(resp))
        .done();
};

function downloadBase64(e: React.MouseEvent<any>, binaryFile: string, fileName: string) {
    e.preventDefault();

    var blob = Services.b64toBlob(binaryFile);

    Services.saveFileBlob(blob, fileName);
};

