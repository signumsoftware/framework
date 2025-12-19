import * as React from 'react'
import { classes } from '@framework/Globals'
import { Constructor } from '@framework/Constructor'
import { TypeContext } from '@framework/TypeContext'
import { Type, getSymbol } from '@framework/Reflection'
import { FormGroup } from '@framework/Lines/FormGroup'
import { ModifiableEntity, Lite, Entity, MList, SearchMessage, EmbeddedEntity, EntityControlMessage, getToString } from '@framework/Signum.Entities'
import { IFile, FileTypeSymbol } from '../Signum.Files'
import { FileDownloader, FileDownloaderConfiguration, DownloadBehaviour } from './FileDownloader'
import { FileUploader } from './FileUploader'
import { Modal } from 'react-bootstrap';
import { FontAwesomeIcon } from "@fortawesome/react-fontawesome";
import "./Files.css"
import { EntityListBaseController, EntityListBaseProps } from '@framework/Lines/EntityListBase'
import { FetchAndRemember } from '@framework/Lines'
import { FileImage } from './FileImage';
import { useController } from '@framework/Lines/LineBase'
import { ImageModal } from './ImageModal'
import { Aprox, AsEntity } from '@framework/Lines/EntityBase'
import { FilesClient } from '../FilesClient'
import { JSX } from 'react/jsx-runtime'
import { LinkButton } from '@framework/Basics/LinkButton'

export { FileTypeSymbol };

interface MultiFileImageLineProps<V extends ModifiableEntity/* & IFile*/ | Lite</*IFile & */Entity>> extends EntityListBaseProps<V> {
  download?: DownloadBehaviour;
  dragAndDrop?: boolean;
  dragAndDropMessage?: string;
  fileType?: FileTypeSymbol;
  accept?: string;
  configuration?: FileDownloaderConfiguration<IFile>;
  imageHtmlAttributes?: React.ImgHTMLAttributes<HTMLImageElement>;
  maxSizeInBytes?: number;
  getFileFromElement?: (ectx: NoInfer<V>) => ModifiableEntity & IFile | Lite<IFile & Entity>;
  createElementFromFile?: (file: ModifiableEntity & IFile) => Promise<NoInfer<V> | undefined>;
  ref?: React.Ref<MultiFileImageLineController<V>>;
}

export class MultiFileImageLineController<V extends ModifiableEntity /*& IFile*/ | Lite</*IFile & */Entity>> extends EntityListBaseController<MultiFileImageLineProps<V>, V> {

  overrideProps(p: MultiFileImageLineProps<V>, overridenProps: MultiFileImageLineProps<V>): void {
    super.overrideProps(p, overridenProps);

    let pr = p.ctx.propertyRoute;
    if (pr) {
      if (p.getFileFromElement)
        pr = pr.addMember("Indexer", "", true).addLambda(p.getFileFromElement);
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

  handleDeleteValue = (index: number): void => {
    const list = this.props.ctx.value;
    list.removeAt(index);
    this.setValue(list);
  }

  handleFileLoaded = (file: IFile & ModifiableEntity): void => {
    if (this.props.createElementFromFile)
      this.props.createElementFromFile(file)
        .then(em => em && this.addElement(em));
    else
      this.convert(file as unknown as Aprox<V>)
        .then(f => this.addElement(f));
  }
}

export function MultiFileImageLine<V extends ModifiableEntity /*& IFile*/ | Lite</*IFile &*/ Entity>>(props: MultiFileImageLineProps<V>): JSX.Element | null {

  const c = useController(MultiFileImageLineController<V>, props);
  const p = c.props;

  if (c.isHidden)
    return null;


  const helpText = p.helpText && (typeof p.helpText == "function" ? p.helpText(c) : p.helpText);
  const helpTextOnTop = p.helpTextOnTop && (typeof p.helpTextOnTop == "function" ? p.helpTextOnTop(c) : p.helpTextOnTop);


  return (
    <FormGroup ctx={p.ctx} error={p.error} label={p.label} labelIcon={p.labelIcon}
      htmlAttributes={{ ...c.baseHtmlAttributes(), ...p.formGroupHtmlAttributes }}
      helpText={helpText}
      helpTextOnTop={helpTextOnTop}
      labelHtmlAttributes={p.labelHtmlAttributes}>
      {() => <div>
        <div className="d-flex">
          {
            c.getMListItemContext(p.ctx.subCtx({ formGroupStyle: "None" })).map(mlec =>
              <div className="sf-file-image-container m-2" key={mlec.index}>
                {p.getComponent ? p.getComponent(mlec as TypeContext<AsEntity<V>>) :
                  p.download == "None" ? <span className={classes(mlec.formControlClass, "file-control")} > {getToString(mlec.value)}</span > :
                    renderFile(p.getFileFromElement ? mlec.subCtx(p.getFileFromElement) : mlec as unknown as TypeContext<ModifiableEntity & IFile | Lite<IFile & Entity>>)}
                {!p.ctx.readOnly &&
                  <LinkButton title={EntityControlMessage.Remove.niceToString()}
                    className="sf-line-button sf-remove"
                    onClick={e => { c.handleDeleteValue(mlec.index!); }}>
                    <FontAwesomeIcon aria-hidden={true} icon="xmark" />
                  </LinkButton>}
              </div>
            )
          }
        </div>
        <div>
          {p.ctx.readOnly ? undefined :
            <FileUploader
              accept={p.accept || "image/*"}
              multiple={true}
              maxSizeInBytes={p.maxSizeInBytes}
              dragAndDrop={p.dragAndDrop ?? true}
              dragAndDropMessage={p.dragAndDropMessage}
              fileType={p.fileType}
              onFileCreated={c.handleFileLoaded}
              typeName={p.getFileFromElement ?
                p.ctx.propertyRoute!.addMember("Indexer", "", true).addLambda(p.getFileFromElement).typeReference().name! :
                p.ctx.propertyRoute!.typeReference().name}
              buttonCss={p.ctx.buttonClass}
              fileDropCssClass={c.mandatoryClass ?? undefined}
              divHtmlAttributes={{ className: "sf-file-line-new" }} />}
        </div>
      </div>}
    </FormGroup>
  );


  function renderFile(ctx: TypeContext<ModifiableEntity & IFile | Lite<IFile & Entity>>) {
    const val = ctx.value!;

    return ctx.propertyRoute!.typeReference().isLite ?
      <FetchAndRemember lite={val! as Lite<IFile & Entity>}>{file => <FileImage file={file} {...p.imageHtmlAttributes} style={{ maxWidth: "100px" }} />}</FetchAndRemember> :
      <FileImage file={val as IFile & ModifiableEntity} {...p.imageHtmlAttributes} style={{ maxWidth: "100px" }} onClick={e => ImageModal.show(val as IFile & ModifiableEntity, e)} />;
  }
}



