import * as React from 'react'
import { classes } from '@framework/Globals'
import { Constructor } from '@framework/Constructor'
import { ButtonBarElement, TypeContext } from '@framework/TypeContext'
import { Type, getSymbol } from '@framework/Reflection'
import { FormGroup } from '@framework/Lines/FormGroup'
import { ModifiableEntity, Lite, Entity, MList, SearchMessage, EntityControlMessage, EmbeddedEntity, MListElement, getToString } from '@framework/Signum.Entities'
import { IFile, FileTypeSymbol, FileMessage } from '../Signum.Files'
import { FileDownloader, FileDownloaderConfiguration, DownloadBehaviour } from './FileDownloader'
import { FileUploader } from './FileUploader'
import { FontAwesomeIcon } from "@fortawesome/react-fontawesome";
import "./Files.css"
import { EntityListBaseController, EntityListBaseProps } from '@framework/Lines/EntityListBase'
import { useController } from '@framework/Lines/LineBase'
import { EntityBaseController } from '@framework/Lines'
import { Aprox, AsEntity } from '@framework/Lines/EntityBase'
import { FilesClient } from '../FilesClient'
import { JSX } from 'react/jsx-runtime'

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
  getFileFromElement?: (ectx: NoInfer<V>) => ModifiableEntity & IFile | Lite<IFile & Entity>;
  createElementFromFile?: (file: ModifiableEntity & IFile) => Promise<NoInfer<V> | undefined>;
  forceShowUploader?: boolean;
  ref?: React.Ref<MultiFileLineController<V>>
}

export class MultiFileLineController<V extends ModifiableEntity /*& IFile*/ | Lite</*IFile & */Entity>> extends EntityListBaseController<MultiFileLineProps<V>, V> {

  forceShowUploader!: boolean;
  setForceShowUploader!: React.Dispatch<boolean>;


  init(p: MultiFileLineProps<V>): void {
    super.init(p);
    [this.forceShowUploader, this.setForceShowUploader] = React.useState<boolean>(() => this.getMListItemContext(p.ctx).length == 0);
  }


  overrideProps(p: MultiFileLineProps<V>, overridenProps: MultiFileLineProps<V>): void {

    p.view = EntityBaseController.defaultIsViewable(p.type!, false) && overridenProps.getFileFromElement != null;

    super.overrideProps(p, overridenProps);

    let pr = p.ctx.propertyRoute;
    if (pr) {
      let prElement = pr!.addMember("Indexer", "", true);
      if (p.getFileFromElement)
        pr = prElement.addLambda(p.getFileFromElement);
      else if (!FilesClient.fileEntityTypeNames[pr.member!.type.name]) {
        throw new Error("getFileFromElement is mandatory because " + pr.member!.type.name + " is not a file");
      }
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

    this.setForceShowUploader(false);
    if (this.props.createElementFromFile)
      this.props.createElementFromFile(file)
        .then(em => em && this.addElement(em));
    else
      this.convert(file as unknown as Aprox<V>)
        .then(f => this.addElement(f));
  }

  renderElementViewButton(btn: boolean, entity: V, index: number): React.JSX.Element | undefined {

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

export function MultiFileLine<V extends ModifiableEntity /*& IFile*/ | Lite</*IFile &*/ Entity>>(props: MultiFileLineProps<V>): JSX.Element | null {
  const c = useController<MultiFileLineController<V>, MultiFileLineProps<V>, MList<V>>(MultiFileLineController, props);
  const p = c.props;

  if (c.isHidden)
    return null;

  const helpText = p.helpText && (typeof p.helpText == "function" ? p.helpText(c) : p.helpText);
  const helpTextOnTop = p.helpTextOnTop && (typeof p.helpTextOnTop == "function" ? p.helpTextOnTop(c) : p.helpTextOnTop);

  const ctxs = c.getMListItemContext(p.ctx.subCtx({ formGroupStyle: "None" }));

  return (
    <FormGroup ctx={p.ctx} error={p.error} label={p.label} labelIcon={p.labelIcon}
      htmlAttributes={{ ...c.baseHtmlAttributes(), ...p.formGroupHtmlAttributes }}
      helpText={helpText}
      helpTextOnTop={helpTextOnTop}
      labelHtmlAttributes={p.labelHtmlAttributes}>
      {() => <table className="sf-multi-value">
        <tbody>
          {
            ctxs.map(mlec => {

              const drag = c.canMove(mlec.value) && p.moveMode == "DragIcon" && !p.ctx.readOnly ? c.getDragConfig(mlec.index!, "v") : undefined;

              return (
                <tr key={mlec.index!}
                  onDragEnter={drag?.onDragOver}
                  onDragOver={drag?.onDragOver}
                  onDrop={drag?.onDrop}
                  className={classes(drag?.dropClass)}
                >
                  <td className="item-group">
                    {drag && <a href="#" className={classes("sf-line-button", "sf-move")} onClick={e => { e.preventDefault(); e.stopPropagation(); }}
                      draggable={true}
                      onKeyDown={drag.onKeyDown}
                      onDragStart={drag.onDragStart}
                      onDragEnd={drag.onDragEnd}
                      title={drag.title}>
                      {EntityBaseController.getMoveIcon()}
                    </a>}

                    {!p.ctx.readOnly &&
                      <a href="#" title={EntityControlMessage.Remove.niceToString()}
                        className="sf-line-button sf-remove"
                        onClick={e => { e.preventDefault(); c.handleDeleteValue(mlec.index!); }}>
                        <FontAwesomeIcon aria-hidden={true} icon="xmark" />
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
                          showFileIcon={p.showFileIcon ?? true}
                          download={p.download ?? "ViewOrSave"}
                          containerEntity={p.getFileFromElement ? mlec.value as ModifiableEntity : undefined}
                          entityOrLite={p.getFileFromElement ? p.getFileFromElement(mlec.value) : mlec.value as ModifiableEntity & IFile | Lite<IFile & Entity>}
                          htmlAttributes={{ className: classes(mlec.formControlClass, "file-control") }} />
                    }
                  </td>
                  {p.view && <td> {c.renderElementViewButton(false, mlec.value, mlec.index!)} </td>}
                </tr>
              );

            })
          }

          <tr >
            <td colSpan={4}>
              {p.ctx.readOnly ? undefined :
                ctxs.length == 0 || c.forceShowUploader || p.forceShowUploader ?
                  <FileUploader
                    accept={p.accept}
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
                    divHtmlAttributes={{ className: "sf-file-line-new" }}
                  /> :
                  <button className="btn btn-link p-0 ms-3 sf-line-button sf-create" onClick={() => c.setForceShowUploader(true)}>{FileMessage.AddMoreFiles.niceToString()}</button>
              }
            </td>
          </tr>
        </tbody>
      </table>}
    </FormGroup>
  );
}
