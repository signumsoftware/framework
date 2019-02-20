/// <reference path="FilesClient.tsx" />
import * as React from 'react'
import { classes } from '@framework/Globals'
import * as Constructor from '@framework/Constructor'
import { TypeContext, mlistItemContext } from '@framework/TypeContext'
import { getSymbol } from '@framework/Reflection'
import { FormGroup } from '@framework/Lines/FormGroup'
import { ModifiableEntity, Lite, Entity, MList, SearchMessage, } from '@framework/Signum.Entities'
import { IFile, FileTypeSymbol } from './Signum.Entities.Files'
import { default as FileDownloader, FileDownloaderConfiguration, DownloadBehaviour } from './FileDownloader'
import FileUploader from './FileUploader'
import { FontAwesomeIcon } from "@fortawesome/react-fontawesome";
import { EntityListBase, EntityListBaseProps } from '@framework/Lines';
import "./Files.css"

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

export class MultiFileLine extends EntityListBase<MultiFileLineProps, MultiFileLineProps> {

  static defaultProps = {
    download: "SaveAs",
    dragAndDrop: true
  }

  calculateDefaultState(state: MultiFileLineProps) {

    super.calculateDefaultState(state);

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
    const list = this.state.ctx.value;
    list.removeAt(index);
    this.setValue(list);
  }

  handleFileLoaded = (file: IFile & ModifiableEntity) => {
    const list = this.state.ctx.value;

    this.convert(file)
      .then(f => this.addElement(f))
      .done();
  }

  defaultCreate() {
    return Constructor.construct(this.state.type!.name).then(a => a && a.entity);
  }

  renderInternal() {

    const s = this.state;
    const list = this.state.ctx.value!;

    return (
      <FormGroup ctx={s.ctx} labelText={s.labelText}
        htmlAttributes={{ ...this.baseHtmlAttributes(), ...this.state.formGroupHtmlAttributes }}
        helpText={this.state.helpText}
        labelHtmlAttributes={s.labelHtmlAttributes}>
        <table className="sf-multi-value">
          <tbody>
            {
              mlistItemContext(s.ctx.subCtx({ formGroupStyle: "None" })).map((mlec, i) =>
                <tr key={i}>
                  <td>
                    {!s.ctx.readOnly &&
                      <a href="#" title={SearchMessage.DeleteFilter.niceToString()}
                        className="sf-line-button sf-remove"
                        onClick={e => { e.preventDefault(); this.handleDeleteValue(i); }}>
                        <FontAwesomeIcon icon="times" />
                      </a>}
                  </td>
                  <td style={{ width: "100%" }}>
                    {this.state.download == "None" ?
                      <span className={classes(mlec.formControlClass, "file-control")} > {mlec.value.toStr}</span > :
                      <FileDownloader
                        configuration={this.props.configuration}
                        download={this.props.download}
                        entityOrLite={mlec.value}
                        htmlAttributes={{ className: classes(mlec.formControlClass, "file-control") }} />}
                  </td>
                </tr>)
            }
            <tr >
              <td colSpan={4}>
                {s.ctx.readOnly ? undefined :
                  <FileUploader
                    accept={s.accept}
                    multiple={true}
                    maxSizeInBytes={s.maxSizeInBytes}
                    dragAndDrop={this.state.dragAndDrop}
                    dragAndDropMessage={this.state.dragAndDropMessage}
                    fileType={this.state.fileType}
                    onFileLoaded={this.handleFileLoaded}
                    typeName={s.ctx.propertyRoute.typeReference().name}
                    buttonCss={s.ctx.buttonClass}
                    divHtmlAttributes={{ className: "sf-file-line-new" }} />}
              </td>
            </tr>
          </tbody>
        </table>
      </FormGroup>
    );
  }
}

