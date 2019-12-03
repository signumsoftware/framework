import * as React from 'react'
import { classes } from '@framework/Globals'
import * as Constructor from '@framework/Constructor'
import { TypeContext } from '@framework/TypeContext'
import { getSymbol } from '@framework/Reflection'
import { FormGroup } from '@framework/Lines/FormGroup'
import { ModifiableEntity, Lite, Entity, MList, SearchMessage, } from '@framework/Signum.Entities'
import { IFile, FileTypeSymbol } from './Signum.Entities.Files'
import { FileDownloader, FileDownloaderConfiguration, DownloadBehaviour } from './FileDownloader'
import { FileUploader } from './FileUploader'
import { FontAwesomeIcon } from "@fortawesome/react-fontawesome";
import "./Files.css"
import { EntityListBaseController, EntityListBaseProps } from '../../../Framework/Signum.React/Scripts/Lines/EntityListBase'
import { useController } from '../../../Framework/Signum.React/Scripts/Lines/LineBase'

export { FileTypeSymbol };

interface MultiFileLineProps extends EntityListBaseProps {
  ctx: TypeContext<MList<ModifiableEntity & IFile | Lite<IFile & Entity>>>;
  download?: DownloadBehaviour;
  dragAndDrop?: boolean;
  dragAndDropMessage?: string;
  fileType?: FileTypeSymbol;
  accept?: string;
  configuration?: FileDownloaderConfiguration<IFile>;
  helpText?: React.ReactChild;
  maxSizeInBytes?: number;
}

export class MultiFileLineController extends EntityListBaseController<MultiFileLineProps> {

  getDefaultProps(state: MultiFileLineProps) {

    super.getDefaultProps(state);

    const m = state.ctx.propertyRoute.member;
    if (m?.defaultFileTypeInfo) {

      if (state.fileType == null)
        state.fileType = getSymbol(FileTypeSymbol, m.defaultFileTypeInfo.key)


      if (state.accept == null && m.defaultFileTypeInfo.onlyImages)
        state.accept = "image/*";

      if (state.maxSizeInBytes == null && m.defaultFileTypeInfo.maxSizeInBytes)
        state.maxSizeInBytes = m.defaultFileTypeInfo.maxSizeInBytes;
    }
  }

  handleDeleteValue = (index: number) => {
    const list = this.props.ctx.value;
    list.removeAt(index);
    this.setValue(list);
  }

  handleFileLoaded = (file: IFile & ModifiableEntity) => {
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
                      <a href="#" title={SearchMessage.DeleteFilter.niceToString()}
                      className="sf-line-button sf-remove"
                      onClick={e => { e.preventDefault(); c.handleDeleteValue(mlec.index!); }}>
                        <FontAwesomeIcon icon="times" />
                      </a>}
                  </td>
                  <td style={{ width: "100%" }}>
                  { p.getComponent ? p.getComponent(mlec) :
                    p.download == "None" ? <span className={classes(mlec.formControlClass, "file-control")} > {mlec.value.toStr}</span > :
                      <FileDownloader
                      configuration={p.configuration}
                      download={p.download}
                        entityOrLite={mlec.value}
                        htmlAttributes={{ className: classes(mlec.formControlClass, "file-control") }} />}
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
                  typeName={p.ctx.propertyRoute.typeReference().name}
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
  download: "SaveAs",
  dragAndDrop: true
} as MultiFileLineProps;
