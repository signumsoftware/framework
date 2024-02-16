import * as React from 'react'
import { classes } from '@framework/Globals'
import * as Constructor from '@framework/Constructor'
import { ButtonBarElement, TypeContext } from '@framework/TypeContext'
import { Type, getSymbol } from '@framework/Reflection'
import { FormGroup } from '@framework/Lines/FormGroup'
import { ModifiableEntity, Lite, Entity, MList, SearchMessage, EntityControlMessage, EmbeddedEntity, MListElement, getToString } from '@framework/Signum.Entities'
import { IFile, FileTypeSymbol } from '../Signum.Files'
import { FileDownloader, FileDownloaderConfiguration, DownloadBehaviour } from './FileDownloader'
import { FileUploader } from './FileUploader'
import { FontAwesomeIcon } from "@fortawesome/react-fontawesome";
import "./Files.css"
import { EntityListBaseController, EntityListBaseProps } from '@framework/Lines/EntityListBase'
import { genericForwardRef, useController } from '@framework/Lines/LineBase'
import { EntityBaseController } from '@framework/Lines'
import { Aprox, AsEntity } from '@framework/Lines/EntityBase'

export { FileTypeSymbol };

interface MultiFileLineProps<V extends ModifiableEntity/* & IFile*/ | Lite</*IFile & */Entity>> extends EntityListBaseProps<V> {
  download?: DownloadBehaviour;
  showFileIcon?: boolean;
  dragAndDrop?: boolean;
  dragAndDropMessage?: string;
  fileType?: FileTypeSymbol;
  accept?: string;
  configuration?: FileDownloaderConfiguration<IFile>;
  maxSizeInBytes?: number;
  getFileFromElement?: (ectx: V) => ModifiableEntity & IFile | Lite<IFile & Entity>;
  createElementFromFile?: (file: ModifiableEntity & IFile) => Promise<V | undefined>;
}

export class MultiFileLineController<V extends ModifiableEntity /*& IFile*/ | Lite</*IFile & */Entity>> extends EntityListBaseController<MultiFileLineProps<V>, V> {

  overrideProps(p: MultiFileLineProps<V>, overridenProps: MultiFileLineProps<V>) {

    p.view = EntityBaseController.defaultIsViewable(p.type!, false) && overridenProps.getFileFromElement != null;

    super.overrideProps(p, overridenProps);

    let pr = p.ctx.propertyRoute;
    if (pr && p.getFileFromElement)
      pr = pr.addMember("Indexer", "", true).addLambda(p.getFileFromElement);

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

  handleDeleteValue = (index: number) => {
    const list = this.props.ctx.value;
    list.removeAt(index);
    this.setValue(list);
  }

  handleFileLoaded = (file: IFile & ModifiableEntity) => {

    if (this.props.createElementFromFile)
      this.props.createElementFromFile(file)
        .then(em => em && this.addElement(em));
    else
      this.convert(file as unknown as Aprox<V>)
        .then(f => this.addElement(f));
  }

  renderElementViewButton(btn: boolean, entity: V, index: number) {

    if (!this.canView(entity))
      return undefined;

    return (
      <a href="#" className={classes("sf-line-button", "sf-view", btn ? "input-group-text" : undefined)}
        onClick={e => this.handleViewElement(e, index)}
        title={this.props.ctx.titleLabels ? EntityControlMessage.View.niceToString() : undefined}>
        {EntityBaseController.getViewIcon()}
      </a>
    );

  }
}

export const MultiFileLine = genericForwardRef(function MultiFileLine<V extends ModifiableEntity /*& IFile*/ | Lite</*IFile &*/ Entity>>(props: MultiFileLineProps<V>, ref: React.Ref<MultiFileLineController<V>>) {
  const c = useController(MultiFileLineController, props, ref);
  const p = c.props;

  if (c.isHidden)
    return null;

  return (
    <FormGroup ctx={p.ctx} label={p.label} labelIcon={p.labelIcon}
      htmlAttributes={{ ...c.baseHtmlAttributes(), ...p.formGroupHtmlAttributes }}
      helpText={p.helpText}
      labelHtmlAttributes={p.labelHtmlAttributes}>
      {() => <table className="sf-multi-value">
        <tbody>
          {
            c.getMListItemContext(p.ctx.subCtx({ formGroupStyle: "None" })).map(mlec =>
              <tr key={mlec.index!}>
                <td>
                  {!p.ctx.readOnly &&
                    <a href="#" title={EntityControlMessage.Remove.niceToString()}
                      className="sf-line-button sf-remove"
                      onClick={e => { e.preventDefault(); c.handleDeleteValue(mlec.index!); }}>
                      <FontAwesomeIcon icon="xmark" />
                    </a>}
                </td>
                <td style={{ width: "100%" }}>
                  {p.getComponent ? p.getComponent(mlec as TypeContext<AsEntity<V>>) :
                    p.download == "None" ?
                      <span className={classes(mlec.formControlClass, "file-control")} >
                        {getToString(p.getFileFromElement ? p.getFileFromElement(mlec.value) : mlec.value)}
                      </span > :
                      <FileDownloader
                        configuration={p.configuration}
                        showFileIcon={p.showFileIcon}
                        download={p.download}
                        containerEntity={p.getFileFromElement ? mlec.value as ModifiableEntity : undefined}
                        entityOrLite={p.getFileFromElement ? p.getFileFromElement(mlec.value) : mlec.value as ModifiableEntity & IFile | Lite<IFile & Entity>}
                        htmlAttributes={{ className: classes(mlec.formControlClass, "file-control") }} />
                  }
                </td>
                {p.view && <td> {c.renderElementViewButton(false, mlec.value, mlec.index!)} </td>}
              </tr>)
          }
          <tr >
            <td colSpan={4}>
              {p.ctx.readOnly ? undefined :
                <FileUploader
                  accept={p.accept}
                  multiple={true}
                  maxSizeInBytes={p.maxSizeInBytes}
                  dragAndDrop={p.dragAndDrop}
                  dragAndDropMessage={p.dragAndDropMessage}
                  fileType={p.fileType}
                  onFileLoaded={c.handleFileLoaded}
                  typeName={p.getFileFromElement ?
                    p.ctx.propertyRoute!.addMember("Indexer", "", true).addLambda(p.getFileFromElement).typeReference().name! :
                    p.ctx.propertyRoute!.typeReference().name}
                  buttonCss={p.ctx.buttonClass}
                  divHtmlAttributes={{ className: "sf-file-line-new" }} />}
            </td>
          </tr>
        </tbody>
      </table>}
    </FormGroup>
  );
});

(MultiFileLine as any).defaultProps = {
  download: "ViewOrSave",
  showFileIcon: true,
  dragAndDrop: true
} as MultiFileLineProps<any>;
