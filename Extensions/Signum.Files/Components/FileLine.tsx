import * as React from 'react'
import { Form } from 'react-bootstrap'
import { classes, Dic } from '@framework/Globals'
import { TypeContext } from '@framework/TypeContext'
import { OperationInfo, Type, getOperationInfo, getSymbol, getTypeInfo, tryGetOperationInfo, tryGetTypeInfo } from '@framework/Reflection'
import { FormGroup } from '@framework/Lines/FormGroup'
import { ModifiableEntity, Lite, Entity, getToString } from '@framework/Signum.Entities'
import { IFile, FileTypeSymbol, IFilePath, FileMessage } from '../Signum.Files'
import { EntityBaseProps, EntityBaseController, Aprox } from '@framework/Lines/EntityBase'
import { FileDownloader, FileDownloaderConfiguration, DownloadBehaviour, toComputerSize } from './FileDownloader'
import { FileUploader, AsyncUploadOptions } from './FileUploader'

import "./Files.css"
import { genericMemo, useController } from '@framework/Lines/LineBase'
import { FilesClient } from '../FilesClient'
import ProgressBar from '@framework/Components/ProgressBar'
import { FontAwesomeIcon } from '@framework/Lines'
import { EntityOperationContext, Operations } from '@framework/Operations'
import { getNiceTypeName } from '@framework/Operations/MultiPropertySetter'
import { JSX } from 'react/jsx-runtime'

export { FileTypeSymbol };

export interface FileLineProps<V extends ModifiableEntity/* & IFile */ | Lite</*IFile & */Entity> | null> extends EntityBaseProps<V> {
  download?: DownloadBehaviour;
  showFileIcon?: boolean;
  dragAndDrop?: boolean;
  dragAndDropMessage?: string;
  fileType?: FileTypeSymbol;
  accept?: string;
  configuration?: FileDownloaderConfiguration<IFile>;
  maxSizeInBytes?: number;
  asyncUpload?: boolean;
  getFileFromElement?: (actx: NoInfer<V>) => ModifiableEntity & IFile | Lite<IFile & Entity>;
  createElementFromFile?: (file: ModifiableEntity & IFile) => Promise<NoInfer<V> | undefined>;
  ref?: React.Ref<FileLineController<V>>
}


export class FileLineController<V extends ModifiableEntity/* & IFile*/ | Lite</*IFile &*/ Entity> | null> extends EntityBaseController<FileLineProps<V>, V> {
  executeWhenFinished?: OperationInfo;

  overrideProps(p: FileLineProps<V>, overridenProps: FileLineProps<V>): void {
    p.view = EntityBaseController.defaultIsViewable(p.type!, false) && overridenProps.getFileFromElement != null;

    super.overrideProps(p, overridenProps);

    let pr = p.ctx.propertyRoute;
    if (pr) {
      if (p.getFileFromElement)
        pr = pr.addLambda(p.getFileFromElement);
      else if (!FilesClient.fileEntityTypeNames[pr.member!.type.name])
        throw new Error("getFileFromElement is mandatory because " + pr.member!.type.name + " is not a file");
    }

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
    if (this.props.createElementFromFile)
      this.props.createElementFromFile(file)
        .then(att => att && this.setValue(att));
    else
      this.convert(file as unknown as Aprox<V>)
        .then(f => this.setValue(f));
  }

  getAsyncOptions(): AsyncUploadOptions | undefined {
    if (this.props.asyncUpload) {

      if (!this.props.fileType)
        throw new Error("AsyncUpload requires a FileTypeSymbol");

      return ({
        chunkSizeMB: 5,
        onStart: (file) => {
          this.forceUpdate();
        }, 
        onProgress: (file) => this.forceUpdate(),
        onFinished: (file) => {
          this.forceUpdate();

          if (this.executeWhenFinished) {
            const frame = this.props.ctx.frame!;
            new EntityOperationContext(frame, frame.pack.entity as Entity, this.executeWhenFinished).click();
          }
        },
        onError: (file, error) => {
          this.setValue(null!);
          throw error;
        },
      });
    }
  }
}

export const FileLine: <V extends ModifiableEntity /* & IFile */ | Lite</*IFile &*/ Entity> | null>(props: FileLineProps<V>) => React.ReactNode | null =
  genericMemo(function FileLine<V extends ModifiableEntity/* & IFile */ | Lite</*IFile &*/ Entity> | null>(props: FileLineProps<V>) {
    const c = useController<FileLineController<V>, FileLineProps<V>, V>(FileLineController, props);
    const p = c.props;

    if (c.isHidden)
      return null;

    const ctx = p.ctx;
    const val = ctx.value!;

    const download = p.download ?? "ViewOrSave";
    const showFileIcon = p.showFileIcon ?? true;
    const dragAndDrop = p.dragAndDrop ?? true;

    const helpText = p.helpText && (typeof p.helpText == "function" ? p.helpText(c) : p.helpText);
    const helpTextOnTop = p.helpTextOnTop && (typeof p.helpTextOnTop == "function" ? p.helpTextOnTop(c) : p.helpTextOnTop);

    function tryGetSaveOperation() {
      const pack = p.ctx.frame?.pack;
      if (pack == null)
        return null;

      const ti = tryGetTypeInfo(pack.entity.Type);
      if (ti == null || ti.operations == null)
        return null;

      var oi = Dic.getValues(ti.operations).onlyOrNull(o => Operations.Defaults.isSave(o));

      if (oi == null || pack.canExecute[oi.key] != null)
        return null;

      return { oi, ti };
    }

    return (
      <FormGroup ctx={p.ctx} error={p.error} label={p.label} labelIcon={p.labelIcon}
        labelHtmlAttributes={p.labelHtmlAttributes}
        htmlAttributes={{ ...c.baseHtmlAttributes(), ...EntityBaseController.entityHtmlAttributes(p.ctx.value), ...p.formGroupHtmlAttributes }}
        helpText={helpText}
        helpTextOnTop={helpTextOnTop}
      >
        {() => {
          if (val) {
            const fp = FilesClient.uploadingInProgress(val);
            if (fp != null) {

              const pair = tryGetSaveOperation();
              
              return <>
                <UploadProgress file={fp} />
                {pair && <small><Form.Check checked={c.executeWhenFinished == pair.oi}
                  label={FileMessage.SaveThe0WhenFinished.niceToString().forGenderAndNumber(pair.ti.gender).formatHtml(<strong>{pair.ti.niceName}</strong>)} onChange={e => {
                  c.executeWhenFinished = e.currentTarget.checked ? pair.oi : undefined;
                  c.forceUpdate();
                  }} /></small>}
              </>;

            }

            return renderFile();
          }

          if (p.ctx.readOnly)
            return null;

          return renderFileUploader();
        }}
      </FormGroup>
    );


    function renderFile() {
      const content = download == "None" ?
        <span className={classes(ctx.formControlClass, "file-control")} >
          {getToString(p.getFileFromElement ? p.getFileFromElement(val) : val)}</span > :
        <FileDownloader
          configuration={p.configuration}
          download={download}
          showFileIcon={showFileIcon}
          containerEntity={p.getFileFromElement ? val as ModifiableEntity : undefined}
          entityOrLite={p.getFileFromElement ? p.getFileFromElement(val) : val as ModifiableEntity & IFile | Lite<IFile & Entity>}
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
        dragAndDrop={dragAndDrop}
        dragAndDropMessage={p.dragAndDropMessage}
        fileType={p.fileType}
        onFileCreated={c.handleFileLoaded}
        typeName={p.getFileFromElement ?
          p.ctx.propertyRoute!.addLambda(p.getFileFromElement).typeReference().name! :
          p.ctx.propertyRoute!.typeReference().name}
        buttonCss={p.ctx.buttonClass}
        fileDropCssClass={c.mandatoryClass ?? undefined}
        divHtmlAttributes={{ className: "sf-file-line-new" }}
        asyncOptions={c.getAsyncOptions()}
      />;

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
  }, (prev, next) => FileLineController.propEquals(prev, next));


function UploadProgress(p: { file: IFilePath }) {
  const abortController = p.file.__abortController;
  return (
    <div>
      <div>
        {abortController && <a href="#" className="sf-line-button sf-remove" onClick={e => { e.preventDefault(); abortController.abort(); }}><FontAwesomeIcon aria-hidden={true} icon="xmark" /></a>}
        <small>{FileMessage.Uploading01.niceToString(p.file.fileName, toComputerSize(p.file.fileLength))}</small>
      </div>
      <ProgressBar color={abortController?.signal.aborted ? "warning" : undefined} value={(p.file.__uploadingOffset == null ? null : p.file.__uploadingOffset / p.file.fileLength)} />
    </div>
  );
}
