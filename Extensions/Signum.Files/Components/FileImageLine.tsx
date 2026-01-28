import * as React from 'react'
import { TypeContext } from '@framework/TypeContext'
import { getSymbol } from '@framework/Reflection'
import { FormGroup } from '@framework/Lines/FormGroup'
import * as Services from '@framework/Services'
import { ModifiableEntity, Lite, Entity, isLite, isEntity } from '@framework/Signum.Entities'
import { IFile, FileTypeSymbol } from '../Signum.Files'
import { EntityBaseProps, EntityBaseController, Aprox, AsEntity } from '@framework/Lines/EntityBase'
import { FileDownloaderConfiguration } from './FileDownloader'
import { FileUploader } from './FileUploader'
import { FileImage } from './FileImage';
import "./Files.css"
import { FetchAndRemember } from '@framework/Lines'
import { genericMemo, useController } from '@framework/Lines/LineBase'
import { ImageModal } from './ImageModal'
import { FileLineProps } from './FileLine'
import { JSX } from 'react/jsx-runtime'

export { FileTypeSymbol };

export interface FileImageLineProps<V extends ModifiableEntity & IFile | Lite<IFile & Entity> | null> extends EntityBaseProps<V> {
  dragAndDrop?: boolean;
  dragAndDropMessage?: string;
  fileType?: FileTypeSymbol;
  accept?: string;
  configuration?: FileDownloaderConfiguration<IFile>;
  imageHtmlAttributes?: React.ImgHTMLAttributes<HTMLImageElement>;
  maxSizeInBytes?: number;
  ajaxOptions?: Omit<Services.AjaxOptions, "url">;
  ref?: React.Ref<FileImageLineController<V>>;
}


export class FileImageLineController<V extends ModifiableEntity & IFile | Lite<IFile & Entity> | null> extends EntityBaseController<FileImageLineProps<V>, V> {

  override getDefaultProps(state: FileImageLineProps<V>): void {

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

  handleFileChanged = (file: IFile & ModifiableEntity): void => {
    this.convert(file as Aprox<V>)
      .then(f => this.setValue(f));
  }
}

export const FileImageLine: <V extends (ModifiableEntity & IFile) | Lite<IFile & Entity> | null>(props: FileImageLineProps<V>) => React.ReactNode | null=
  genericMemo(function FileImageLine<V extends ModifiableEntity & IFile | Lite<IFile & Entity> | null>(props: FileImageLineProps<V>): JSX.Element | null {
    const c = useController<FileImageLineController<V>, FileImageLineProps<V>, V>(FileImageLineController, props);
    const p = c.props;

    const hasValue = !!p.ctx.value;

    if (c.isHidden)
      return null;

    const helpText = p.helpText && (typeof p.helpText == "function" ? p.helpText(c) : p.helpText);
    const helpTextOnTop = p.helpTextOnTop && (typeof p.helpTextOnTop == "function" ? p.helpTextOnTop(c) : p.helpTextOnTop);

    return (
      <FormGroup ctx={p.ctx} error={p.error} label={p.label} labelIcon={p.labelIcon}
        labelHtmlAttributes={p.labelHtmlAttributes}
        htmlAttributes={{ ...c.baseHtmlAttributes(), ...EntityBaseController.entityHtmlAttributes(p.ctx.value), ...p.formGroupHtmlAttributes }}
        helpText={helpText}
        helpTextOnTop={helpTextOnTop}
      >
        {() => hasValue ? renderImage() : p.ctx.readOnly ? undefined :
          <FileUploader
            accept={p.accept}
            maxSizeInBytes={p.maxSizeInBytes}
            dragAndDrop={c.props.dragAndDrop}
            dragAndDropMessage={c.props.dragAndDropMessage}
            fileType={c.props.fileType}
            onFileCreated={c.handleFileChanged}
            typeName={p.ctx.propertyRoute!.typeReference().name}
            buttonCss={p.ctx.buttonClass}
            fileDropCssClass={c.mandatoryClass ?? undefined}
            divHtmlAttributes={{ className: "sf-file-line-new" }} />
        }
      </FormGroup>
    );

    function renderImage() {

      var ctx = p.ctx;

      const val = ctx.value!;

      const display = ctx.formGroupStyle == "Basic" ? "block" : undefined;

      var content = ctx.propertyRoute!.typeReference().isLite ?
        <FetchAndRemember lite={val! as Lite<IFile & Entity>}>{file => <FileImage file={file} style={{ maxWidth: "100px", display }} onClick={e => ImageModal.show(file as IFile & ModifiableEntity, e)} {...p.imageHtmlAttributes} />}</FetchAndRemember> :
        <FileImage file={val as IFile & ModifiableEntity} style={{ maxWidth: "100px", display }} onClick={e => ImageModal.show(val as IFile & ModifiableEntity, e)} {...p.imageHtmlAttributes} ajaxOptions={p.ajaxOptions} />;

      const removeButton = c.renderRemoveButton(true);

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

(FileImageLine as any).defaultProps = {
  accept: "image/*",
  dragAndDrop: true
} as FileImageLineProps<any>;

