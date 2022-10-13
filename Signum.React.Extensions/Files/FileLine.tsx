import * as React from 'react'
import { classes } from '@framework/Globals'
import { TypeContext } from '@framework/TypeContext'
import { getSymbol } from '@framework/Reflection'
import { FormGroup } from '@framework/Lines/FormGroup'
import { ModifiableEntity, Lite, Entity, getToString, } from '@framework/Signum.Entities'
import { IFile, FileTypeSymbol } from './Signum.Entities.Files'
import { EntityBaseProps, EntityBaseController } from '@framework/Lines/EntityBase'
import { FileDownloader, FileDownloaderConfiguration, DownloadBehaviour } from './FileDownloader'
import { FileUploader }  from './FileUploader'

import "./Files.css"
import { useController } from '@framework/Lines/LineBase'

export { FileTypeSymbol };

export interface FileLineProps extends EntityBaseProps {
  ctx: TypeContext<ModifiableEntity & IFile | Lite<IFile & Entity> | undefined | null>;
  download?: DownloadBehaviour;
  showFileIcon?: boolean;
  dragAndDrop?: boolean;
  dragAndDropMessage?: string;
  fileType?: FileTypeSymbol;
  accept?: string;
  configuration?: FileDownloaderConfiguration<IFile>;
  maxSizeInBytes?: number;
}


export class FileLineController extends EntityBaseController<FileLineProps>{

  getDefaultProps(state: FileLineProps) {

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

    this.convert(file)
      .then(f => this.setValue(f));
  }
}

export const FileLine = React.memo(React.forwardRef(function FileLine(props: FileLineProps, ref: React.Ref<FileLineController>) {
  const c = useController(FileLineController, props, ref);
  const p = c.props;

  if (c.isHidden)
    return null;

  const hasValue = !!p.ctx.value;

  return (
    <FormGroup ctx={p.ctx} label={p.label}
      labelHtmlAttributes={p.labelHtmlAttributes}
      htmlAttributes={{ ...c.baseHtmlAttributes(), ...EntityBaseController.entityHtmlAttributes(p.ctx.value), ...p.formGroupHtmlAttributes }}
      helpText={p.helpText}>
      {hasValue ? renderFile() : p.ctx.readOnly ? undefined :
        <FileUploader
          accept={p.accept}
          maxSizeInBytes={p.maxSizeInBytes}
          dragAndDrop={p.dragAndDrop}
          dragAndDropMessage={p.dragAndDropMessage}
          fileType={p.fileType}
          onFileLoaded={c.handleFileLoaded}
          typeName={p.ctx.propertyRoute!.typeReference().name}
          buttonCss={p.ctx.buttonClass}
          fileDropCssClass={c.mandatoryClass ?? undefined}
          divHtmlAttributes={{ className: "sf-file-line-new" }} />
      }
    </FormGroup>
  );


  function renderFile() {

    var ctx = p.ctx;

    const val = ctx.value!;

    const content = p.download == "None" ?
      <span className={classes(ctx.formControlClass, "file-control")} > {getToString(val)}</span > :
      <FileDownloader
        configuration={p.configuration}
        download={p.download}
        showFileIcon={p.showFileIcon}
        entityOrLite={val}
        htmlAttributes={{ className: classes(ctx.formControlClass, "file-control") }} />;

    const removeButton = c.renderRemoveButton(true, val);

    if (removeButton == null)
      return content;

    return (
      <div className={ctx.inputGroupClass}>
        {content}
        {removeButton}
      </div>
    );
  }
}), (prev, next) => FileLineController.propEquals(prev, next));

(FileLine as any).defaultProps = {
  download: "ViewOrSave",
  dragAndDrop: true,
  showFileIcon: true
} as FileLineProps;
