import * as React from 'react'
import { Link } from 'react-router'
import { classes, Dic } from '../../../Framework/Signum.React/Scripts/Globals'
import * as Navigator from '../../../Framework/Signum.React/Scripts/Navigator'
import * as Constructor from '../../../Framework/Signum.React/Scripts/Constructor'
import * as Finder from '../../../Framework/Signum.React/Scripts/Finder'
import { FindOptions } from '../../../Framework/Signum.React/Scripts/FindOptions'
import { TypeContext, StyleContext, StyleOptions, FormGroupStyle } from '../../../Framework/Signum.React/Scripts/TypeContext'
import { PropertyRoute, PropertyRouteType, MemberInfo, getTypeInfo, getTypeInfos, TypeInfo, IsByAll } from '../../../Framework/Signum.React/Scripts/Reflection'
import { LineBase, LineBaseProps, FormGroup, FormControlStatic, runTasks} from '../../../Framework/Signum.React/Scripts/Lines/LineBase'
import { ModifiableEntity, Lite, Entity, EntityControlMessage, JavascriptMessage, toLite, is, liteKey, getToString } from '../../../Framework/Signum.React/Scripts/Signum.Entities'
import { IFile, IFilePath, FileMessage, FileTypeSymbol, FileEntity, FilePathEntity, EmbeddedFileEntity, EmbeddedFilePathEntity } from './Signum.Entities.Files'
import Typeahead from '../../../Framework/Signum.React/Scripts/Lines/Typeahead'
import { EntityBase, EntityBaseProps} from '../../../Framework/Signum.React/Scripts/Lines/EntityBase'

require("!style!css!./Files.css");

export { FileTypeSymbol };

export interface FileLineProps extends EntityBaseProps {
    ctx?: TypeContext<ModifiableEntity & IFile | Lite<IFile & Entity>>;
    download?: DownloadBehaviour;
    dragAndDrop?: boolean;
    dragAndDropMessage?: string;
    fileType?: FileTypeSymbol;
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

    static defaultProps: FileLineProps = {
        ctx: null,
        download: DownloadBehaviour.SaveAs,
        dragAndDrop: true
    }

    calculateDefaultState(state: FileLineProps) {
        super.calculateDefaultState(state);
        state.configuration = FileLine.configurtions[state.type.name];
    }

    componentWillMount() {

        if (!this.state.configuration)
            throw new Error(`No FileLineConfiguration found for '${this.state.type.name}'`)

        var ctx = this.state.ctx;
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
                {hasValue ? this.renderLink() : this.renderPlaceholder() }
            </FormGroup>
        );
    }


    renderLink() {

        var val = this.state.ctx.value;
        var entity = (val as Lite<IFile & Entity>).EntityType ?
            (val as Lite<IFile & Entity>).entity :
            (val as IFile & Entity);

        return (
            <div className="input-group">
                {
                    entity == null ? <span className="form-control file-control">{JavascriptMessage.loading.niceToString() }</span> :
                        this.state.download == DownloadBehaviour.None || entity.isNew ? <span className="form-control file-control">{entity.fileName}</span> :
                            <a className="form-control file-control"
                                href={this.state.configuration.downloadLink(entity) }
                                title={entity.fileName}
                                download={this.state.download == DownloadBehaviour.View ? null : entity.fileName}>
                                {entity.fileName}
                            </a>
                }

                <span className="input-group-btn">
                    {this.renderRemoveButton(true) }
                </span>
            </div>
        );
    }


    handleDragOver = (e: React.DragEvent) => {
        e.preventDefault();
        this.setState({ isOver: true });
    }

    handleDragLeave = (e: React.DragEvent) => {
        e.preventDefault();
        this.setState({ isOver: false });
    }

    handleDrop = (e: React.DragEvent) => {
        e.preventDefault();
        this.setState({ isOver: false, isLoading: true });

        var file = e.dataTransfer.files[0];

        this.uploadFile(file);
    }

    handleFileChange = (e: React.FormEvent) => {
        e.preventDefault();
        this.setState({ isOver: false, isLoading: true });


        this.uploadFile((e.target as HTMLInputElement).files[0]);
    }

    uploadFile(file: File) {
        var fileReader = new FileReader();
        fileReader.onerror = e => { setTimeout(() => { throw (e as any).error; }, 0); };
        fileReader.onload = e => {
            var newEntity = {
                Type: this.state.type.name,
                isNew: true,
                modified: true,
                fileName: file.name,
                binaryFile: ((e.target as any).result as string).after("base64,")
            } as ModifiableEntity & IFile;

            if (this.state.fileType)
                (newEntity as any as IFilePath).fileType = this.state.fileType;

            this.convert(newEntity).then(e => {
                this.setValue(e);
                this.setState({ isLoading: false });
            }).done();
        };
        fileReader.readAsDataURL(file);
    }

    renderPlaceholder() {

        return (
            <div className="sf-file-line-new">
                <input type='file' className='form-control' onChange={this.handleFileChange}/>
                {this.state.isLoading ? <div className="sf-file-drop">{JavascriptMessage.loading.niceToString() }</div> :
                    (this.state.dragAndDrop && <div className={classes("sf-file-drop", this.state.isOver ? "sf-file-drop-over" : null) }
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
    downloadLink: (entity: T)=> string;
}


FileLine.configurtions[FileEntity.typeName] = {
    downloadLink: e => Navigator.currentHistory.createHref("api/files/downloadFile/" + e.id.toString())
} as FileLineConfiguration<FileEntity>;

FileLine.configurtions[FilePathEntity.typeName] = {
    downloadLink: e => Navigator.currentHistory.createHref("api/files/downloadFilePath/" + e.id.toString())
} as FileLineConfiguration<FilePathEntity>;

FileLine.configurtions[EmbeddedFileEntity.typeName] = {
    downloadLink: e => "data:application/octet-stream;base64," + e.binaryFile
} as FileLineConfiguration<EmbeddedFileEntity>;

FileLine.configurtions[EmbeddedFilePathEntity.typeName] = {
    downloadLink: e => Navigator.currentHistory.createHref({
        pathname: "api/files/downloadEmbeddedFilePath/" + e.fileType.key,
        query: {
            suffix: e.suffix,
            fileName: e.fileName
        }
    })
} as FileLineConfiguration<EmbeddedFilePathEntity>;
