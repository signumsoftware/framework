import * as React from 'react'
import { classes } from '@framework/Globals'
import * as Constructor from '@framework/Constructor'
import { TypeContext } from '@framework/TypeContext'
import { getSymbol } from '@framework/Reflection'
import { FormGroup } from '@framework/Lines/FormGroup'
import { ModifiableEntity, Lite, Entity, MList, SearchMessage, EntityControlMessage, EmbeddedEntity } from '@framework/Signum.Entities'
import { IFile, FileTypeSymbol } from './Signum.Entities.Files'
import { FileDownloader, FileDownloaderConfiguration, DownloadBehaviour } from './FileDownloader'
import { FileUploader } from './FileUploader'
import { FontAwesomeIcon } from "@fortawesome/react-fontawesome";
import "./Files.css"
import { EntityListBaseController, EntityListBaseProps } from '@framework/Lines/EntityListBase'
import { useController } from '@framework/Lines/LineBase'

export { FileTypeSymbol };

interface MultiFileLineProps extends EntityListBaseProps {
  ctx: TypeContext<MList<ModifiableEntity & IFile | Lite<IFile & Entity> | EmbeddedEntity /*implement getFile create Embedded*/>>;
  download?: DownloadBehaviour;
  showFileIcon?: boolean;
  dragAndDrop?: boolean;
  dragAndDropMessage?: string;
  fileType?: FileTypeSymbol;
  accept?: string;
  configuration?: FileDownloaderConfiguration<IFile>;
  maxSizeInBytes?: number;
  getFile?: (e: any /*EmbeddedEntity*/) => ModifiableEntity & IFile | Lite<IFile & Entity>;
  createEmbedded?: (file: ModifiableEntity & IFile) => Promise<EmbeddedEntity>;
}

export class MultiFileLineController extends EntityListBaseController<MultiFileLineProps> {

  overrideProps(p: MultiFileLineProps, overridenProps: MultiFileLineProps) {
    super.overrideProps(p, overridenProps);

    let pr = p.ctx.propertyRoute;
    if (pr && p.getFile)
      pr = pr.addMember("Indexer", "", true).addLambda(p.getFile);

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

    if (this.props.createEmbedded)
      this.props.createEmbedded(file)
        .then(em => em && this.addElement(em))
        .done();
    else
      this.convert(file)
        .then(f => this.addElement(f))
        .done();
  }

  defaultCreate() {
    return Constructor.construct(this.props.type!.name);
  }
}

export const MultiFileLine = React.forwardRef(function MultiFileLine(props: MultiFileLineProps, ref: React.Ref<MultiFileLineController>) {
  const c = useController(MultiFileLineController, props, ref);
  const p = c.props;

  if (c.isHidden)
    return null;

  return (
    <FormGroup ctx={p.ctx} labelText={p.labelText}
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
                      <FontAwesomeIcon icon="times" />
                    </a>}
                </td>
                <td style={{ width: "100%" }}>
                  {p.getComponent ? p.getComponent(mlec) :
                    p.download == "None" ?
                      <span className={classes(mlec.formControlClass, "file-control")} >
                        {p.getFile ? p.getFile(mlec.value).toStr : mlec.value.toStr}
                      </span > :
                      <FileDownloader
                        configuration={p.configuration}
                        showFileIcon={p.showFileIcon}
                        download={p.download}
                        entityOrLite={p.getFile ? p.getFile(mlec.value as EmbeddedEntity) : mlec.value as ModifiableEntity & IFile | Lite<IFile & Entity>}
                        htmlAttributes={{ className: classes(mlec.formControlClass, "file-control") }} />
                  }
                </td>
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
                  typeName={p.getFile ?
                    p.ctx.propertyRoute!.addMember("Indexer", "", true).addLambda(p.getFile).typeReference().name! :
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
