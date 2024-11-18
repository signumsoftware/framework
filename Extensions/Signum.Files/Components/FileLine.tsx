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
import { genericForwardRef, useController } from '@framework/Lines/LineBase'

export { FileTypeSymbol };

export interface FileLineProps<V extends ModifiableEntity /*& IFile*/ | Lite</*IFile &*/ Entity>> extends EntityBaseProps<V> {
  download?: DownloadBehaviour;
  showFileIcon?: boolean;
  dragAndDrop?: boolean;
  dragAndDropMessage?: string;
  fileType?: FileTypeSymbol;
  accept?: string;
  //configuration?: FileDownloaderConfiguration<AsEntity<V>>;
  configuration?: FileDownloaderConfiguration<IFile>;
  maxSizeInBytes?: number;
  getFile?: (ectx: NoInfer<V>) => ModifiableEntity & IFile | Lite<IFile & Entity>;
  createFromFile?: (file: ModifiableEntity & IFile) => Promise<NoInfer<V> | undefined>;
}


export class FileLineController<V extends ModifiableEntity /*& IFile */| Lite</*IFile & */Entity>> extends EntityBaseController<FileLineProps<V>, V>{

  overrideProps(p: FileLineProps<V>, overridenProps: FileLineProps<V>): void {
  //getDefaultProps(state: FileLineProps<V>): void {

    //super.getDefaultProps(state);
    p.view = EntityBaseController.defaultIsViewable(p.type!, false) && overridenProps.getFile != null;
    super.overrideProps(p, overridenProps);

    let pr = p.ctx.propertyRoute;
    //if (pr && p.getFile)
    //  pr = pr.addMember("Indexer", "", true).addLambda(p.getFile);

    const m = pr?.member;
    if (m?.defaultFileTypeInfo) {

      if (p.fileType == null)
        p.fileType = getSymbol(FileTypeSymbol, m.defaultFileTypeInfo.key)


      if (p.accept == null && m.defaultFileTypeInfo.onlyImages)
        p.accept = "image/*";

      if (p.maxSizeInBytes == null && m.defaultFileTypeInfo.maxSizeInBytes)
        p.maxSizeInBytes = m.defaultFileTypeInfo.maxSizeInBytes;
    }
  }

  handleFileLoaded = (file: IFile & ModifiableEntity): void => {

    if (this.props.createFromFile)
      this.props.createFromFile(file)
        .then(em => em && this.setValue(em));
    else
      this.convert(file as unknown as Aprox<V>)
      .then(f => this.setValue(f));
  }
}

export const FileLine: <V extends (ModifiableEntity/* & IFile*/) | Lite</*IFile & */Entity>>(props: FileLineProps<V> & React.RefAttributes<FileLineController<V>>) => React.ReactNode | null = 
  genericForwardRef(function FileLine<V extends ModifiableEntity/* & IFile */| Lite</*IFile & */Entity>>(props: FileLineProps<V>, ref: React.Ref<FileLineController<V>>) {

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
      <span className={classes(ctx.formControlClass, "file-control")}> {/*getToString(p.getFile ? p.getFile(val) : val)*/"Test"}</span > :
      <FileDownloader
        configuration={p.configuration /*as FileDownloaderConfiguration<IFile>*/}
        download={p.download}
        showFileIcon={p.showFileIcon}
        containerEntity={p.getFile ? val as ModifiableEntity : undefined}
        entityOrLite={p.getFile ? p.getFile(val) : val as ModifiableEntity & IFile | Lite<IFile & Entity>}
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

    if (!p.extraButtonsBefore && !p.extraButtons)
      return content;

    return (
      <div className={p.ctx.inputGroupClass}>
        {p.extraButtonsBefore?.(c)}
        {content}
        {p.extraButtons?.(c)}
      </div>
    );
  }
});

(FileLine as any).defaultProps = {
  download: "ViewOrSave",
  dragAndDrop: true,
  showFileIcon: true
} as FileLineProps<any>;
