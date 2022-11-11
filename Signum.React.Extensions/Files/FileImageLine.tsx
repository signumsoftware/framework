import * as React from 'react'
import { TypeContext } from '@framework/TypeContext'
import { getSymbol } from '@framework/Reflection'
import { FormGroup } from '@framework/Lines/FormGroup'
import { ModifiableEntity, Lite, Entity, isLite, isEntity } from '@framework/Signum.Entities'
import { IFile, FileTypeSymbol } from './Signum.Entities.Files'
import { EntityBaseProps, EntityBaseController } from '@framework/Lines/EntityBase'
import { FileDownloaderConfiguration } from './FileDownloader'
import { FileUploader } from './FileUploader'
import { FileImage } from './FileImage';
import "./Files.css"
import { FetchAndRemember } from '@framework/Lines'
import { useController } from '@framework/Lines/LineBase'
import { ImageModal } from './ImageModal'

export { FileTypeSymbol };

export interface FileImageLineProps extends EntityBaseProps {
  ctx: TypeContext<ModifiableEntity & IFile | Lite<IFile & Entity> | undefined | null>;
  dragAndDrop?: boolean;
  dragAndDropMessage?: string;
  fileType?: FileTypeSymbol;
  accept?: string;
  configuration?: FileDownloaderConfiguration<IFile>;
  imageHtmlAttributes?: React.ImgHTMLAttributes<HTMLImageElement>;
  maxSizeInBytes?: number;
}


export class FileImageLineController extends EntityBaseController<FileImageLineProps> {

  getDefaultProps(state: FileImageLineProps) {

    super.getDefaultProps(state);

    const m = state.ctx.propertyRoute?.member;
    if (m?.defaultFileTypeInfo) {

      if (state.fileType == null)
        state.fileType = getSymbol(FileTypeSymbol, m.defaultFileTypeInfo.key);

      //if (state.accept == null && m.defaultFileTypeInfo.onlyImages)
      //    state.accept = "images/*";

      if (state.maxSizeInBytes == null && m.defaultFileTypeInfo.maxSizeInBytes)
        state.maxSizeInBytes = m.defaultFileTypeInfo.maxSizeInBytes;
    }
  }

  handleFileLoaded = (file: IFile & ModifiableEntity) =>{
    this.convert(file)
      .then(f => this.setValue(f));
  }
}

export const FileImageLine = React.forwardRef(function FileImageLine(props: FileImageLineProps, ref: React.Ref<FileImageLineController>) {
  const c = useController(FileImageLineController, props, ref);
  const p = c.props;

  const hasValue = !!p.ctx.value;

  if (c.isHidden)
    return null;

  return (
    <FormGroup ctx={p.ctx} label={p.label}
      labelHtmlAttributes={p.labelHtmlAttributes}
      htmlAttributes={{ ...c.baseHtmlAttributes(), ...EntityBaseController.entityHtmlAttributes(p.ctx.value), ...p.formGroupHtmlAttributes }}
      helpText={c.props.helpText}>
      {hasValue ? renderImage() : p.ctx.readOnly ? undefined :
        <FileUploader
          accept={p.accept}
          maxSizeInBytes={p.maxSizeInBytes}
          dragAndDrop={c.props.dragAndDrop}
          dragAndDropMessage={c.props.dragAndDropMessage}
          fileType={c.props.fileType}
          onFileLoaded={c.handleFileLoaded}
          typeName={p.ctx.propertyRoute!.typeReference().name}
          buttonCss={p.ctx.buttonClass}
          divHtmlAttributes={{ className: "sf-file-line-new" }} />
      }
    </FormGroup>
  );

  function renderImage() {

    var ctx = p.ctx;

    const val = ctx.value!;

    var content = ctx.propertyRoute!.typeReference().isLite ?
      <FetchAndRemember lite={val! as Lite<IFile & Entity>}>{file => <FileImage file={file} style={{ maxWidth: "100px" }} onClick={e => ImageModal.show(file as IFile & ModifiableEntity, e)} {...p.imageHtmlAttributes} />}</FetchAndRemember> :
      <FileImage file={val as IFile & ModifiableEntity} style={{ maxWidth: "100px" }} onClick={e => ImageModal.show(val as IFile & ModifiableEntity, e)} {...p.imageHtmlAttributes} />;

    const removeButton = c.renderRemoveButton(true, val);

    if (removeButton == null)
      return content;

    return (
      <div className="sf-file-image-container">
        {removeButton}
        {content}
      </div>
    );
  }
});

FileImageLine.defaultProps = {
  accept: "image/*",
  dragAndDrop: true
};

