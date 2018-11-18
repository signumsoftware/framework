import * as React from 'react'
import { Retrieve } from '@framework/Retrieve'
import { TypeContext } from '@framework/TypeContext'
import { getSymbol } from '@framework/Reflection'
import { FormGroup } from '@framework/Lines/FormGroup'
import { ModifiableEntity, Lite, Entity } from '@framework/Signum.Entities'
import { IFile, FileTypeSymbol } from './Signum.Entities.Files'
import { EntityBase, EntityBaseProps } from '@framework/Lines/EntityBase'
import { FileDownloaderConfiguration } from './FileDownloader'
import FileUploader from './FileUploader'
import { FileImage } from './FileImage';
import "./Files.css"

export { FileTypeSymbol };

export interface FileImageLineProps extends EntityBaseProps {
  ctx: TypeContext<ModifiableEntity & IFile | Lite<IFile & Entity> | undefined | null>;
  dragAndDrop?: boolean;
  dragAndDropMessage?: string;
  fileType?: FileTypeSymbol;
  accept?: string;
  configuration?: FileDownloaderConfiguration<IFile>;
  helpText?: React.ReactChild;
  imageHtmlAttributes?: React.ImgHTMLAttributes<HTMLImageElement>;
  maxSizeInBytes?: number;
}


export default class FileImageLine extends EntityBase<FileImageLineProps, FileImageLineProps> {

  static defaultProps = {
    accept: "image/*",
    dragAndDrop: true
  }

  calculateDefaultState(state: FileImageLineProps) {

    super.calculateDefaultState(state);

    const m = state.ctx.propertyRoute.member;
    if (m && m.defaultFileTypeInfo) {

      if (state.fileType == null)
        state.fileType = getSymbol(FileTypeSymbol, m.defaultFileTypeInfo.key);

      //if (state.accept == null && m.defaultFileTypeInfo.onlyImages)
      //    state.accept = "images/*";

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
        {hasValue ? this.renderImage() : s.ctx.readOnly ? undefined :
          <FileUploader
            accept={s.accept}
            maxSizeInBytes={s.maxSizeInBytes}
            dragAndDrop={this.props.dragAndDrop}
            dragAndDropMessage={this.props.dragAndDropMessage}
            fileType={this.props.fileType}
            onFileLoaded={this.handleFileLoaded}
            typeName={s.ctx.propertyRoute.typeReference().name}
            buttonCss={s.ctx.buttonClass}
            divHtmlAttributes={{ className: "sf-file-line-new" }} />
        }
      </FormGroup>
    );
  }


  renderImage() {

    var ctx = this.state.ctx;

    const val = ctx.value!;

    var content = ctx.propertyRoute.typeReference().isLite ?
      Retrieve.create(val as Lite<IFile & Entity>, file => <FileImage file={file} {...this.props.imageHtmlAttributes} />) :
      <FileImage file={val as IFile & ModifiableEntity} {...this.props.imageHtmlAttributes} />

    const removeButton = this.renderRemoveButton(true, val);

    if (removeButton == null)
      return content;

    return (
      <div className="sf-file-image-container">
        {removeButton}
        {content}
      </div>
    );
  }
}

