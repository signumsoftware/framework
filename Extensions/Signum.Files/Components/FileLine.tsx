import * as React from 'react'
import { classes } from '@framework/Globals'
import { TypeContext } from '@framework/TypeContext'
import { Type, getSymbol } from '@framework/Reflection'
import { FormGroup } from '@framework/Lines/FormGroup'
import { ModifiableEntity, Lite, Entity, getToString, } from '@framework/Signum.Entities'
import { IFile, FileTypeSymbol } from '../Signum.Files'
import { EntityBaseProps, EntityBaseController, AsEntity, Aprox } from '@framework/Lines/EntityBase'
import { FileDownloader, FileDownloaderConfiguration, DownloadBehaviour } from './FileDownloader'
import { FileUploader }  from './FileUploader'

import "./Files.css"
import { genericForwardRef, genericForwardRefWithMemo, useController } from '@framework/Lines/LineBase'

export { FileTypeSymbol };

export interface FileLineProps<V extends ModifiableEntity & IFile | Lite<IFile & Entity> | null> extends EntityBaseProps<V> {
  download?: DownloadBehaviour;
  showFileIcon?: boolean;
  dragAndDrop?: boolean;
  dragAndDropMessage?: string;
  fileType?: FileTypeSymbol;
  accept?: string;
  configuration?: FileDownloaderConfiguration<AsEntity<V>>;
  maxSizeInBytes?: number;
}


export class FileLineController<V extends ModifiableEntity & IFile | Lite<IFile & Entity> | null> extends EntityBaseController<FileLineProps<V>, V>{

  getDefaultProps(state: FileLineProps<V>) {

    super.getDefaultProps(state);

    const m = state.ctx.propertyRoute?.member;
    if (m?.defaultFileTypeInfo) {

      if (state.fileType == null)
        state.fileType = getSymbol(FileTypeSymbol, m.defaultFileTypeInfo.key)


      if (state.accept == null && m.defaultFileTypeInfo.onlyImages)
        state.accept = "image/*";

      if (state.maxSizeInBytes == null && m.defaultFileTypeInfo.maxSizeInBytes)
        state.maxSizeInBytes = m.defaultFileTypeInfo.maxSizeInBytes;
    }
  }

  handleFileLoaded = (file: IFile & ModifiableEntity) => {

    this.convert(file as Aprox<V>)
      .then(f => this.setValue(f));
  }
}

export const FileLine = genericForwardRefWithMemo(function FileLine<V extends ModifiableEntity & IFile | Lite<IFile & Entity> | null>(props: FileLineProps<V>, ref: React.Ref<FileLineController<V>>) {
  const c = useController(FileLineController, props, ref);
  const p = c.props;

  if (c.isHidden)
    return null;

  const hasValue = !!p.ctx.value;

  return (
    <FormGroup ctx={p.ctx} label={p.label} labelIcon={p.labelIcon}
      labelHtmlAttributes={p.labelHtmlAttributes}
      htmlAttributes={{ ...c.baseHtmlAttributes(), ...EntityBaseController.entityHtmlAttributes(p.ctx.value), ...p.formGroupHtmlAttributes }}
      helpText={p.helpText}>
      {() => hasValue ? renderFile() : p.ctx.readOnly ? undefined : renderFileUploader()}
    </FormGroup>
  );


  function renderFile() {

    var ctx = p.ctx;

    const val = ctx.value!;

    const content = p.download == "None" ?
      <span className={classes(ctx.formControlClass, "file-control")} > {getToString(val)}</span > :
      <FileDownloader
        configuration={p.configuration as FileDownloaderConfiguration<IFile>}
        download={p.download}
        showFileIcon={p.showFileIcon}
        entityOrLite={val}
        htmlAttributes={{ className: classes(ctx.formControlClass, "file-control") }} />;

    const buttons =
      <>
        {c.props.extraButtonsBefore && c.props.extraButtonsBefore(c)}
        {c.renderRemoveButton(true)}
        {c.props.extraButtons && c.props.extraButtons(c)}
      </>;

    if (!EntityBaseController.hasChildrens(buttons))
      return content;

    return (
      <div className={ctx.inputGroupClass}>
        {content}
        {buttons}
      </div>
    );
  }

  function renderFileUploader() {

    const content = <FileUploader
      accept={p.accept}
      maxSizeInBytes={p.maxSizeInBytes}
      dragAndDrop={p.dragAndDrop}
      dragAndDropMessage={p.dragAndDropMessage}
      fileType={p.fileType}
      onFileLoaded={c.handleFileLoaded}
      typeName={p.ctx.propertyRoute!.typeReference().name}
      buttonCss={p.ctx.buttonClass}
      fileDropCssClass={c.mandatoryClass ?? undefined}
      divHtmlAttributes={{ className: "sf-file-line-new" }} />;

    if (!p.extraButtonsBefore && !p.extraButtonsAfter)
      return content;

    return (
      <div className={p.ctx.inputGroupClass}>
        {p.extraButtonsBefore?.(c)}
        {content}
        {p.extraButtonsAfter?.(c)}
      </div>
    );
  }
}), (prev, next) => FileLineController.propEquals(prev, next));

(FileLine as any).defaultProps = {
  download: "ViewOrSave",
  dragAndDrop: true,
  showFileIcon: true
} as FileLineProps<any>;
