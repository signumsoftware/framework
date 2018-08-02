/// <reference path="filesclient.tsx" />
import * as React from 'react'
import { classes, Dic } from '@framework/Globals'
import * as Services from '@framework/Services'
import * as Navigator from '@framework/Navigator'
import * as Constructor from '@framework/Constructor'
import * as Finder from '@framework/Finder'
import { FindOptions } from '@framework/FindOptions'
import { TypeContext, StyleContext, StyleOptions, FormGroupStyle } from '@framework/TypeContext'
import { PropertyRoute, PropertyRouteType, MemberInfo, getTypeInfo, getTypeInfos, TypeInfo, IsByAll, New, getSymbol } from '@framework/Reflection'
import { LineBase, LineBaseProps } from '@framework/Lines/LineBase'
import { FormGroup } from '@framework/Lines/FormGroup'
import { ModifiableEntity, Lite, Entity, EntityControlMessage, JavascriptMessage, toLite, is, liteKey, getToString, } from '@framework/Signum.Entities'
import { IFile, IFilePath, FileMessage, FileTypeSymbol, FileEntity, FilePathEntity, FileEmbedded, FilePathEmbedded } from './Signum.Entities.Files'
import { EntityBase, EntityBaseProps } from '@framework/Lines/EntityBase'
import { default as FileDownloader, FileDownloaderConfiguration, DownloadBehaviour } from './FileDownloader'
import FileUploader from './FileUploader'

import "./Files.css"

export { FileTypeSymbol };

export interface FileLineProps extends EntityBaseProps {
    ctx: TypeContext<ModifiableEntity & IFile | Lite<IFile & Entity> | undefined | null>;
    download?: DownloadBehaviour;
    dragAndDrop?: boolean;
    dragAndDropMessage?: string;
    fileType?: FileTypeSymbol;
    accept?: string;
    configuration?: FileDownloaderConfiguration<IFile>;
    helpText?: React.ReactChild;
    maxSizeInBytes?: number;
}


export default class FileLine extends EntityBase<FileLineProps, FileLineProps> {

    static defaultProps = {
        download: "SaveAs",
        dragAndDrop: true
    }
   
    calculateDefaultState(state: FileLineProps) {
        
        super.calculateDefaultState(state);
        
        const m = state.ctx.propertyRoute.member;
        if (m && m.defaultFileTypeInfo) {

            if (state.fileType == null)
                state.fileType = getSymbol(FileTypeSymbol, m.defaultFileTypeInfo.key)


            if (state.accept == null && m.defaultFileTypeInfo.onlyImages)
                state.accept = "images/*";

            if (state.maxSizeInBytes == null && m.defaultFileTypeInfo.maxSizeInBytes)
                state.maxSizeInBytes = m.defaultFileTypeInfo.maxSizeInBytes;
        }
    }

    handleFileLoaded = (file: IFile & ModifiableEntity) => {

        this.convert(file)
            .then(f => this.setValue(f))
            .done();
    }
    

    renderInternal() {

        const s = this.state;

        const hasValue = !!s.ctx.value;

        return (
            <FormGroup ctx={s.ctx} labelText={s.labelText}
                labelHtmlAttributes={s.labelHtmlAttributes}
                htmlAttributes={{ ...this.baseHtmlAttributes(), ...EntityBase.entityHtmlAttributes(s.ctx.value), ...s.formGroupHtmlAttributes }}
                helpText={this.props.helpText}>
                {hasValue ? this.renderFile() : s.ctx.readOnly ? undefined :
                    <FileUploader
                        accept={s.accept}
                        maxSizeInBytes={s.maxSizeInBytes}
                        dragAndDrop={this.props.dragAndDrop}
                        dragAndDropMessage={this.props.dragAndDropMessage}
                        fileType={this.props.fileType}
                        onFileLoaded={this.handleFileLoaded}
                        typeName={s.ctx.propertyRoute.typeReference().name}
                        buttonCss={s.ctx.buttonClass}
                        divHtmlAttributes={{ className: "sf-file-line-new" }}/>
                }
            </FormGroup>
        );
    }


    renderFile() {

        var ctx = this.state.ctx;

        const val = ctx.value!;

        const content = this.state.download == "None" ?
            <span className={classes(ctx.formControlClass, "file-control")} > {val.toStr}</span > :
            <FileDownloader
                configuration={this.props.configuration}
                download={this.props.download}
                entityOrLite={val}
                htmlAttributes={{ className: classes(ctx.formControlClass, "file-control") }} />;

        const removeButton = this.renderRemoveButton(true, val);

        if (removeButton == null)
            return content;

        return (
            <div className={ctx.inputGroupClass}>
                {content}
                <span className="input-group-append">
                    {removeButton }
                </span>
            </div>
        );
    }
    
}

