import * as React from 'react'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { classes, bytesToSize } from '@framework/Globals'
import { ModifiableEntity, JavascriptMessage } from '@framework/Signum.Entities'
import { IFile, IFilePath, FileMessage, FileTypeSymbol } from './Signum.Entities.Files'

import "./Files.css"
import { New } from '@framework/Reflection';

export { FileTypeSymbol };

export interface FileUploaderProps {
  onFileLoaded: (file: IFile & ModifiableEntity, index: number, count: number) => void;
  typeName: string;
  fileType?: FileTypeSymbol;
  dragAndDrop?: boolean;
  dragAndDropMessage?: string;
  accept?: string;
  multiple?: boolean;
  buttonCss?: string;
  divHtmlAttributes?: React.HTMLAttributes<HTMLDivElement>;
  maxSizeInBytes?: number;
}

export interface FileUploaderState {
  isLoading?: boolean;
  isOver?: boolean;
  errors: string[];
}

export default class FileUploader extends React.Component<FileUploaderProps, FileUploaderState> {

  static defaultProps = {
    dragAndDrop: true
  };

  constructor(props: FileUploaderProps) {
    super(props);

    this.state = { isLoading: false, isOver: false, errors: [] };
  }

  handleDragOver = (e: React.DragEvent<any>) => {
    e.stopPropagation();
    e.preventDefault();
    this.setState({ isOver: true });
  }

  handleDragLeave = (e: React.DragEvent<any>) => {
    e.stopPropagation();
    e.preventDefault();
    this.setState({ isOver: false });
  }

  handleDrop = (e: React.DragEvent<any>) => {
    e.stopPropagation();
    e.preventDefault();

    this.uploadAll(e.dataTransfer.files);
  }

  handleFileChange = (e: React.FormEvent<any>) => {
    e.preventDefault();

    var input = e.target as HTMLInputElement;

    this.uploadAll(input.files!);
  }

  uploadAll(files: FileList) {
    this.state.errors.clear();
    this.setState({
      isOver: false,
      isLoading: true,
    });
    var promises: Promise<void>[] = [];
    for (var i = 0; i < files!.length; i++) {
      promises.push(this.uploadFile(files![i], i + 1, files!.length));
    }

    Promise.all(promises).then(() => this.setState({ isLoading: false, isOver: false })).done();
  }

  setError(error: string) {
    this.state.errors.push(error);
    this.forceUpdate();
  }

  uploadFile(file: File, index: number, count: number): Promise<void> {

    return new Promise((resolve) => {
      if (file.type && this.props.accept) {
        if (!file.type.startsWith(this.props.accept.replace("*", ""))) {
          this.setError(FileMessage.TheFile0IsNotA1.niceToString(file.name, this.props.accept));
          return resolve();
        }
      }

      if (this.props.maxSizeInBytes != null && this.props.maxSizeInBytes < file.size) {
        this.setError(FileMessage.File0IsTooBigTheMaximumSizeIs1.niceToString(file.name, bytesToSize(this.props.maxSizeInBytes)));
        return resolve();
      }

      if (file.name.contains("%")) {
        this.setError(FileMessage.TheNameOfTheFileMustNotContainPercentSymbol.niceToString());
        return resolve();
      }

      const fileReader = new FileReader();
      fileReader.onerror = e => { setTimeout(() => { throw (e as any).error; }, 0); };
      fileReader.onload = e => {
        const fileEntity = New(this.props.typeName) as ModifiableEntity & IFile;
        fileEntity.fileName = file.name;
        fileEntity.binaryFile = ((e.target as any).result as string).after("base64,");

        if (this.props.fileType)
          (fileEntity as any as IFilePath).fileType = this.props.fileType;


        this.props.onFileLoaded(fileEntity, index, count);
        resolve();
      };
      fileReader.readAsDataURL(file);
    });
  }

  render() {
    return (
      <div {...this.props.divHtmlAttributes}>
        {this.state.isLoading ? <div className="sf-file-drop">{JavascriptMessage.loading.niceToString()}</div> :
          (this.props.dragAndDrop ? <div className={classes("sf-file-drop", this.state.isOver ? "sf-file-drop-over" : undefined)}
            onDragEnter={this.handleDragOver}
            onDragOver={this.handleDragOver}
            onDragLeave={this.handleDragLeave}
            onDrop={this.handleDrop}
          >
            <div className={classes("sf-upload btn btn-light", this.props.buttonCss)}>
              <FontAwesomeIcon icon="upload" />
              {FileMessage.SelectFile.niceToString()}
              <input type='file' accept={this.props.accept} onChange={this.handleFileChange} multiple={this.props.multiple} />
            </div>
            &nbsp;{this.props.dragAndDropMessage || FileMessage.OrDragAFileHere.niceToString()}
          </div> :
            <div className={classes("sf-upload btn btn-light", this.props.buttonCss)}>
              <FontAwesomeIcon icon="upload"  className="mr-1"/>
              {FileMessage.SelectFile.niceToString()}
              <input type='file' accept={this.props.accept} onChange={this.handleFileChange} multiple={this.props.multiple} />
            </div>
          )
        }
        {this.state.errors.map((e, i) => <p key={i} className="text-danger">{e}</p>)}
      </div>
    );
  }

}
