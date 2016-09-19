import * as React from 'react'
import { Link } from 'react-router'
import { classes, Dic } from '../../../Framework/Signum.React/Scripts/Globals'
import * as Services from '../../../Framework/Signum.React/Scripts/Services'
import * as Navigator from '../../../Framework/Signum.React/Scripts/Navigator'
import * as Constructor from '../../../Framework/Signum.React/Scripts/Constructor'
import * as Finder from '../../../Framework/Signum.React/Scripts/Finder'
import { FindOptions } from '../../../Framework/Signum.React/Scripts/FindOptions'
import { TypeContext, StyleContext, StyleOptions, FormGroupStyle } from '../../../Framework/Signum.React/Scripts/TypeContext'
import { PropertyRoute, PropertyRouteType, MemberInfo, getTypeInfo, getTypeInfos, TypeInfo, IsByAll, basicConstruct, getTypeName } from '../../../Framework/Signum.React/Scripts/Reflection'
import { LineBase, LineBaseProps, FormGroup, FormControlStatic, runTasks } from '../../../Framework/Signum.React/Scripts/Lines/LineBase'
import { ModifiableEntity, Lite, Entity, EntityControlMessage, JavascriptMessage, toLite, is, liteKey, getToString } from '../../../Framework/Signum.React/Scripts/Signum.Entities'
import { IFile, IFilePath, FileMessage, FileTypeSymbol, FileEntity, FilePathEntity, EmbeddedFileEntity, EmbeddedFilePathEntity } from './Signum.Entities.Files'
import Typeahead from '../../../Framework/Signum.React/Scripts/Lines/Typeahead'
import { EntityBase, EntityBaseProps} from '../../../Framework/Signum.React/Scripts/Lines/EntityBase'

require("!style!css!./Files.css");


export type DownloadBehaviour = "SaveAs" | "View" | "None";

export interface FileDownloaderProps {
    entityOrLite: ModifiableEntity & IFile | Lite<IFile & Entity>;
    download?: DownloadBehaviour;
    configuration?: FileDownloaderConfiguration<IFile>;
    htmlProps: React.HTMLAttributes;
}

export default class FileDownloader extends React.Component<FileDownloaderProps, void> {

    static configurtions: { [typeName: string]: FileDownloaderConfiguration<IFile> } = {};


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

    handleDownloadClick = (e: React.MouseEvent, url: string) => {
        e.preventDefault();
        Services.ajaxGetRaw({ url: url })
            .then(resp => Services.saveFile(resp))
            .done();
    };

    render() {

        const entityOrLite = this.props.entityOrLite;

        const entity = (entityOrLite as Lite<IFile & Entity>).EntityType ?
            (entityOrLite as Lite<IFile & Entity>).entity :
            (entityOrLite as IFile & Entity);

        if (!entity)
            return <span {...this.props.htmlProps}>{JavascriptMessage.loading.niceToString()}</span>;


        const configuration = this.props.configuration || FileDownloader.configurtions[entity.Type];

        if (!configuration)
            throw new Error("No configuration registered in FileDownloader.configurations for "); 

        const dl = entity.binaryFile ?
            { url: "data:application/octet-stream;base64," + entity.binaryFile, requiresToken: false } as DownloadLinkResult :
            configuration.downloadLink(entity);


        return (
            <a
                onClick={dl.requiresToken ? ((e) => this.handleDownloadClick(e, dl.url)) : undefined}
                download={this.props.download == "View" ? undefined : entity.fileName }
                href={dl.requiresToken ? "" : dl.url}
                title={entity.fileName || undefined}
                {...this.props.htmlProps}>
                {entity.fileName}
            </a>
        );

    }
}


export interface FileDownloaderConfiguration<T extends IFile> {
    downloadLink: (entity: T) => DownloadLinkResult;
}

export interface DownloadLinkResult {
    url: string;
    requiresToken: boolean;
}


FileDownloader.configurtions[FileEntity.typeName] = {
    downloadLink: e => ({ url: Navigator.currentHistory.createHref("~/api/files/downloadFile/" + e.id.toString()), requiresToken: true })
} as FileDownloaderConfiguration<FileEntity>;

FileDownloader.configurtions[FilePathEntity.typeName] = {
    downloadLink: e => ({ url: Navigator.currentHistory.createHref("~/api/files/downloadFilePath/" + e.id.toString()), requiresToken: true })
} as FileDownloaderConfiguration<FilePathEntity>;

FileDownloader.configurtions[EmbeddedFileEntity.typeName] = {
    downloadLink: e => ({ url: "data:application/octet-stream;base64," + e.binaryFile, requiresToken: false })
} as FileDownloaderConfiguration<EmbeddedFileEntity>;

FileDownloader.configurtions[EmbeddedFilePathEntity.typeName] = {
    downloadLink: e => ({
        url: Navigator.currentHistory.createHref({
            pathname: "~/api/files/downloadEmbeddedFilePath/" + e.fileType!.key,
            query: {  suffix: e.suffix, fileName: e.fileName }
        }),
        requiresToken: true
    })
} as FileDownloaderConfiguration<EmbeddedFilePathEntity>;
