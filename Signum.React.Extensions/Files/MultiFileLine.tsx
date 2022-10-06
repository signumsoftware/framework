import * as React from 'react'
import { classes } from '@framework/Globals'
import * as Constructor from '@framework/Constructor'
import { ButtonBarElement, TypeContext } from '@framework/TypeContext'
import { getSymbol } from '@framework/Reflection'
import { FormGroup } from '@framework/Lines/FormGroup'
import { ModifiableEntity, Lite, Entity, MList, SearchMessage, EntityControlMessage, EmbeddedEntity, MListElement, getToString } from '@framework/Signum.Entities'
import { IFile, FileTypeSymbol } from './Signum.Entities.Files'
import { FileDownloader, FileDownloaderConfiguration, DownloadBehaviour } from './FileDownloader'
import { FileUploader } from './FileUploader'
import { FontAwesomeIcon } from "@fortawesome/react-fontawesome";
import "./Files.css"
import { EntityListBaseController, EntityListBaseProps } from '@framework/Lines/EntityListBase'
import { useController } from '@framework/Lines/LineBase'
import { EntityBaseController } from '../../Signum.React/Scripts/Lines'

export { FileTypeSymbol };

interface MultiFileLineProps extends EntityListBaseProps {
  ctx: TypeContext<MList<ModifiableEntity & IFile | Lite<IFile & Entity> | ModifiableEntity /*implement getFile create Embedded*/>>;
  download?: DownloadBehaviour;
  showFileIcon?: boolean;
  dragAndDrop?: boolean;
  dragAndDropMessage?: string;
  fileType?: FileTypeSymbol;
  accept?: string;
  configuration?: FileDownloaderConfiguration<IFile>;
  maxSizeInBytes?: number;
  getFileFromElement?: (e: any /*ModifiableEntity*/) => ModifiableEntity & IFile | Lite<IFile & Entity>;
  createElementFromFile?: (file: ModifiableEntity & IFile) => Promise<ModifiableEntity>;
}

export class MultiFileLineController extends EntityListBaseController<MultiFileLineProps> {

  overrideProps(p: MultiFileLineProps, overridenProps: MultiFileLineProps) {

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
      this.convert(file)
        .then(f => this.addElement(f));
  }

  defaultCreate() {
    return Constructor.construct(this.props.type!.name);
  }

  renderElementViewButton(btn: boolean, entity: ModifiableEntity | Lite<Entity>, index: number) {

    if (!this.canView(entity))
      return undefined;

    return (
      <a href="#" className={classes("sf-line-button", "sf-view", btn ? "input-group-text" : undefined)}
        onClick={e => this.handleViewElement(e, index)}
        title={this.props.ctx.titleLabels ? EntityControlMessage.View.niceToString() : undefined}>
        {EntityBaseController.viewIcon}
      </a>
    );

  }
}

export const MultiFileLine = React.forwardRef(function MultiFileLine(props: MultiFileLineProps, ref: React.Ref<MultiFileLineController>) {
  const c = useController(MultiFileLineController, props, ref);
  const p = c.props;

  if (c.isHidden)
    return null;

  return (
    <FormGroup ctx={p.ctx} label={p.label}
      htmlAttributes={{ ...c.baseHtmlAttributes(), ...p.formGroupHtmlAttributes }}
      helpText={p.helpText}
      labelHtmlAttributes={p.labelHtmlAttributes}>
      <table className="sf-multi-value">
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
                  {p.getComponent ? p.getComponent(mlec) :
                    p.download == "None" ?
                      <span className={classes(mlec.formControlClass, "file-control")} >
                        {getToString(p.getFileFromElement ? p.getFileFromElement(mlec.value) : mlec.value)}
                      </span > :
                      <FileDownloader
                        configuration={p.configuration}
                        showFileIcon={p.showFileIcon}
                        download={p.download}
                        entityOrLite={p.getFileFromElement ? p.getFileFromElement(mlec.value as EmbeddedEntity) : mlec.value as ModifiableEntity & IFile | Lite<IFile & Entity>}
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
      </table>
    </FormGroup>
  );
});

(MultiFileLine as any).defaultProps = {
  download: "ViewOrSave",
  showFileIcon: true,
  dragAndDrop: true
} as MultiFileLineProps;
