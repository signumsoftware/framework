import * as React from 'react'
import { Link } from 'react-router'
import { classes, Dic } from '../../../Framework/Signum.React/Scripts/Globals'
import * as Services from '../../../Framework/Signum.React/Scripts/Services'
import * as Navigator from '../../../Framework/Signum.React/Scripts/Navigator'
import * as Constructor from '../../../Framework/Signum.React/Scripts/Constructor'
import * as Finder from '../../../Framework/Signum.React/Scripts/Finder'
import { FindOptions } from '../../../Framework/Signum.React/Scripts/FindOptions'
import { TypeContext, StyleContext, StyleOptions, FormGroupStyle } from '../../../Framework/Signum.React/Scripts/TypeContext'
import { PropertyRoute, PropertyRouteType, MemberInfo, getTypeInfo, getTypeInfos, TypeInfo, IsByAll, basicConstruct } from '../../../Framework/Signum.React/Scripts/Reflection'
import { LineBase, LineBaseProps, FormGroup, FormControlStatic, runTasks} from '../../../Framework/Signum.React/Scripts/Lines/LineBase'
import { ModifiableEntity, Lite, Entity, EntityControlMessage, JavascriptMessage, toLite, is, liteKey, getToString, } from '../../../Framework/Signum.React/Scripts/Signum.Entities'
import { IFile, IFilePath, FileMessage, FileTypeSymbol, FileEntity, FilePathEntity, EmbeddedFileEntity, EmbeddedFilePathEntity } from './Signum.Entities.Files'
import Typeahead from '../../../Framework/Signum.React/Scripts/Lines/Typeahead'
import { EntityBase, EntityBaseProps} from '../../../Framework/Signum.React/Scripts/Lines/EntityBase'

require("!style!css!./Files.css");

export { FileTypeSymbol };

export interface FileLineProps extends EntityBaseProps {
    ctx: TypeContext<ModifiableEntity & IFile | Lite<IFile & Entity> | undefined | null>;
    download?: DownloadBehaviour;
    dragAndDrop?: boolean;
    dragAndDropMessage?: string;
    fileType?: FileTypeSymbol;
    accept?: string;
    configuration?: FileLineConfiguration<IFile>;
}

export enum DownloadBehaviour {
    SaveAs,
    View,
    None
}

export interface FileLineState extends FileLineProps {
    isLoading?: boolean;
    isOver?: boolean;
}

export default class FileLine extends EntityBase<FileLineProps, FileLineState> {

    static defaultProps = {
        download: DownloadBehaviour.SaveAs,
        dragAndDrop: true
    }

    calculateDefaultState(state: FileLineProps) {
        super.calculateDefaultState(state);
        state.configuration = FileLine.configurtions[state.type!.name];
    }

    componentWillMount() {

        if (!this.state.configuration)
            throw new Error(`No FileLineConfiguration found for '${this.state.type!.name}'`)

        const ctx = this.state.ctx;
        if (ctx.value && (ctx.value as Lite<IFile & Entity>).EntityType)
            Navigator.API.fetchAndRemember(ctx.value as Lite<IFile & Entity>)
                .then(() => this.forceUpdate())
                .done();

    }

    renderInternal() {

        const s = this.state;

        const hasValue = !!s.ctx.value;

        return (
            <FormGroup ctx={s.ctx} labelText={s.labelText} labelProps={s.labelHtmlProps} htmlProps={Dic.extend(this.baseHtmlProps(), EntityBase.entityHtmlProps(s.ctx.value), s.formGroupHtmlProps) }>
                {hasValue ? this.renderFile() : this.renderPlaceholder() }
            </FormGroup>
        );
    }


    renderFile() {

        const val = this.state.ctx.value;
        const entity = (val as Lite<IFile & Entity>).EntityType ?
            (val as Lite<IFile & Entity>).entity :
            (val as IFile & Entity);

        return (
            <div className="input-group">
                {
                    entity == undefined ? <span className="form-control file-control">{JavascriptMessage.loading.niceToString() }</span> :
                        this.state.download == DownloadBehaviour.None || entity.isNew ? <span className="form-control file-control">{entity.fileName}</span> :
                            this.renderLink(entity)
                }
                <span className="input-group-btn">
                    {this.renderRemoveButton(true) }
                </span>
            </div>
        );
    }


    renderLink(entity: IFile) {

        const dl = this.state.configuration!.downloadLink(entity);

        return (
            <a className="form-control file-control"
                onClick={dl.requiresToken ? ((e) => this.handleDownloadClick(e, dl.url)) : undefined}
                download={this.state.download == DownloadBehaviour.View ? undefined : entity.fileName}
                href={dl.requiresToken ? "#" : dl.url}
                title={entity.fileName || undefined}>
                {entity.fileName}
            </a>
        );

    }

    handleDownloadClick = (e: React.MouseEvent, url: string) => {
        e.preventDefault();
        Services.ajaxGetRaw({ url: url })
            .then(resp => Services.saveFile(resp))
            .done();
    };

    handleDragOver = (e: React.DragEvent) => {
        e.preventDefault();
        this.state.isOver = true;
        this.forceUpdate();
    }

    handleDragLeave = (e: React.DragEvent) => {
        e.preventDefault();
        this.state.isOver = false;
        this.forceUpdate();
    }

    handleDrop = (e: React.DragEvent) => {
        e.preventDefault();
        this.state.isOver = false;
        this.state.isLoading = true;
        this.forceUpdate();
        
        const file = e.dataTransfer.files[0];

        this.uploadFile(file);
    }

    handleFileChange = (e: React.FormEvent) => {
        e.preventDefault();
        this.state.isOver = false;
        this.state.isLoading = true;
        this.forceUpdate();

        this.uploadFile((e.target as HTMLInputElement).files![0]);
    }

    uploadFile(file: File) {
        const fileReader = new FileReader();
        fileReader.onerror = e => { setTimeout(() => { throw (e as any).error; }, 0); };
        fileReader.onload = e => {
            const newEntity = basicConstruct(this.state.type!.name) as ModifiableEntity & IFile;
            newEntity.fileName = file.name;
            newEntity.binaryFile = ((e.target as any).result as string).after("base64,");

            if (this.state.fileType)
                (newEntity as any as IFilePath).fileType = this.state.fileType;

            this.convert(newEntity).then(e => {
                this.setValue(e);
                this.state.isLoading = false;
                this.forceUpdate();
            }).done();
        };
        fileReader.readAsDataURL(file);
    }

    renderPlaceholder() {

        return (
            <div className="sf-file-line-new">
                <input type='file' className='form-control' accept={this.props.accept} onChange={this.handleFileChange}/>
                {this.state.isLoading ? <div className="sf-file-drop">{JavascriptMessage.loading.niceToString() }</div> :
                    (this.state.dragAndDrop && <div className={classes("sf-file-drop", this.state.isOver ? "sf-file-drop-over" : undefined) }
                        onDragOver={this.handleDragOver}
                        onDragLeave={this.handleDragLeave}
                        onDrop={this.handleDrop}
                        >
                        {this.state.dragAndDropMessage || FileMessage.DragAndDropHere.niceToString() }
                    </div>)
                }
            </div>
        );
    }

    static configurtions: { [typeName: string]: FileLineConfiguration<IFile> } = {};
}


interface FileLineConfiguration<T extends IFile> {
    downloadLink: (entity: T) => DownloadLinkResult;
}

interface DownloadLinkResult {
    url: string;
    requiresToken: boolean;
}


FileLine.configurtions[FileEntity.typeName] = {
    downloadLink: e => ({ url: Navigator.currentHistory.createHref("~/api/files/downloadFile/" + e.id.toString()), requiresToken: true })
} as FileLineConfiguration<FileEntity>;

FileLine.configurtions[FilePathEntity.typeName] = {
    downloadLink: e => ({ url: Navigator.currentHistory.createHref("~/api/files/downloadFilePath/" + e.id.toString()), requiresToken: true })
} as FileLineConfiguration<FilePathEntity>;

FileLine.configurtions[EmbeddedFileEntity.typeName] = {
    downloadLink: e => ({ url: "data:application/octet-stream;base64," + e.binaryFile, requiresToken: false })
} as FileLineConfiguration<EmbeddedFileEntity>;

FileLine.configurtions[EmbeddedFilePathEntity.typeName] = {
    downloadLink: e => ({
        url: Navigator.currentHistory.createHref({
            pathname: "~/api/files/downloadEmbeddedFilePath/" + e.fileType!.key,
            query: {  suffix: e.suffix, fileName: e.fileName }
        }),
        requiresToken: true
    })
} as FileLineConfiguration<EmbeddedFilePathEntity>;
