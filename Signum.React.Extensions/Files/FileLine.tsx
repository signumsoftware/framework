/// <reference path="FilesClient.tsx" />
import * as React from 'react'
import { classes } from '@framework/Globals'
import { TypeContext } from '@framework/TypeContext'
import { getSymbol } from '@framework/Reflection'
import { FormGroup } from '@framework/Lines/FormGroup'
import { ModifiableEntity, Lite, Entity, } from '@framework/Signum.Entities'
import { IFile, FileTypeSymbol } from './Signum.Entities.Files'
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
        state.accept = "image/*";

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
        helpText={this.state.helpText}>
        {hasValue ? this.renderFile() : s.ctx.readOnly ? undefined :
          <FileUploader
            accept={s.accept}
            maxSizeInBytes={s.maxSizeInBytes}
            dragAndDrop={this.state.dragAndDrop}
            dragAndDropMessage={this.state.dragAndDropMessage}
            fileType={this.state.fileType}
            onFileLoaded={this.handleFileLoaded}
            typeName={s.ctx.propertyRoute.typeReference().name}
            buttonCss={s.ctx.buttonClass}
            divHtmlAttributes={{ className: "sf-file-line-new" }} />
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
          {removeButton}
        </span>
      </div>
    );
  }

}

