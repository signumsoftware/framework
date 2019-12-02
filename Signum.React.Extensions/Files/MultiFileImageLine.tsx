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
import { Modal } from 'react-bootstrap';
import { FontAwesomeIcon } from "@fortawesome/react-fontawesome";
import "./Files.css"
import { EntityListBaseController, EntityListBaseProps } from '../../../Framework/Signum.React/Scripts/Lines/EntityListBase'
import { FetchAndRemember } from '../../../Framework/Signum.React/Scripts/Lines'
import { FileImage } from './FileImage';
import { useController } from '../../../Framework/Signum.React/Scripts/Lines/LineBase'
import { center, wrap } from '../Map/Utils'
import { openModal, IModalProps } from '../../../Framework/Signum.React/Scripts/Modals'
import { useForceUpdate } from '../../../Framework/Signum.React/Scripts/Hooks'

export { FileTypeSymbol };

interface MultiFileImageLineProps extends EntityListBaseProps {
  ctx: TypeContext<MList<ModifiableEntity & IFile | Lite<IFile & Entity>>>;
  download?: DownloadBehaviour;
  dragAndDrop?: boolean;
  dragAndDropMessage?: string;
  fileType?: FileTypeSymbol;
  accept?: string;
  configuration?: FileDownloaderConfiguration<IFile>;
  helpText?: React.ReactChild;
  imageHtmlAttributes?: React.ImgHTMLAttributes<HTMLImageElement>;
  maxSizeInBytes?: number;
}

export class MultiFileImageLineController extends EntityListBaseController<MultiFileImageLineProps> {

  getDefaultProps(state: MultiFileImageLineProps) {

    super.getDefaultProps(state);

    const m = state.ctx.propertyRoute.member;
    if (m && m.defaultFileTypeInfo) {

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

export const MultiFileImageLine = React.forwardRef(function MultiFileLine(props: MultiFileImageLineProps, ref: React.Ref<MultiFileImageLineController>) {

  const c = useController(MultiFileImageLineController, props, ref);
  const p = c.props;

  if (c.isHidden)
    return null;

    return (
    <FormGroup ctx={p.ctx} labelText={p.labelText}
      htmlAttributes={{ ...c.baseHtmlAttributes(), ...p.formGroupHtmlAttributes }}
      helpText={p.helpText}
      labelHtmlAttributes={p.labelHtmlAttributes}>
        <table className="sf-multi-value" >
          <tbody>
            <tr>
              {
                c.getMListItemContext(p.ctx.subCtx({ formGroupStyle: "None" })).map(mlec => 
                  <>
                  <td style={{ width: "100px", verticalAlign: "center", paddingLeft: "10px" }}>
                    {p.getComponent ? p.getComponent(mlec) :
                      p.download == "None" ? <span className={classes(mlec.formControlClass, "file-control")} > {mlec.value.toStr}</span > :
                        renderImage(mlec)}
                  </td>
                    <td style={{ width: "10px"}}>
                    {!p.ctx.readOnly &&
                      <a href="#" title={SearchMessage.DeleteFilter.niceToString()}
                      className="sf-line-button sf-remove"
                      onClick={e => { e.preventDefault(); c.handleDeleteValue(mlec.index!); }}>
                        <FontAwesomeIcon icon="times" />
                      </a>}
                      </td>
                </>
              )
              }
              <td >
              </td>
            </tr>
            <tr>
              <td colSpan={p.ctx.value.length*2 + 1}>
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


  function renderImage(ctx: TypeContext<ModifiableEntity & IFile | Lite<IFile & Entity> | undefined | null>)
  {
    const val = ctx.value!;

    var content = ctx.propertyRoute.typeReference().isLite ?
      <FetchAndRemember lite={val! as Lite<IFile & Entity>}>{file => <FileImage file={file} {...p.imageHtmlAttributes} style={{ maxWidth: "100px" }} />}</FetchAndRemember> :
      <FileImage file={val as IFile & ModifiableEntity} {...p.imageHtmlAttributes} style={{ maxWidth: "100px" }} onClick={e => showImageLarge(val as IFile & ModifiableEntity)}/>;


    return (
      <div className="sf-file-image-container" style={{ maxWidth: "100px" }}>
        {content}
      </div>
    );
 }
                        
});

(MultiFileImageLine as any).defaultProps = {
  download: "SaveAs",
  dragAndDrop: true
} as MultiFileImageLineProps;



