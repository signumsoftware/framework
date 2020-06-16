import * as React from 'react'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { classes, bytesToSize } from '@framework/Globals'
import { ModifiableEntity, JavascriptMessage } from '@framework/Signum.Entities'
import { IFile, IFilePath, FileMessage, FileTypeSymbol } from './Signum.Entities.Files'

import "./Files.css"
import { New } from '@framework/Reflection';

export { FileTypeSymbol };

export interface FileUploaderProps {
  onFileLoaded: (file: IFile & ModifiableEntity, index: number, count: number, htmlFile: File) => void;
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

export function FileUploader(p: FileUploaderProps) {

  const [isLoading, setIsLodaing] = React.useState<boolean>(false);
  const [isOver, setIsOver] = React.useState<boolean>(false);
  const [errors, setErrors] = React.useState<string[]>([]);



  function handleDragOver(e: React.DragEvent<any>) {
    e.stopPropagation();
    e.preventDefault();
    setIsOver(true);
  }

  function handleDragLeave(e: React.DragEvent<any>) {
    e.stopPropagation();
    e.preventDefault();
    setIsOver(false);
  }

  function handleDrop(e: React.DragEvent<any>) {
    e.stopPropagation();
    e.preventDefault();

    uploadAll(e.dataTransfer.files);
  }

  function handleFileChange(e: React.FormEvent<any>) {
    e.preventDefault();

    var input = e.target as HTMLInputElement;

    uploadAll(input.files!);
  }

  function uploadAll(files: FileList) {
    setErrors([]);
    setIsOver(false);
    setIsLodaing(true);

    var promises: Promise<void>[] = [];
    for (var i = 0; i < files!.length; i++) {
      promises.push(uploadFile(files![i], i + 1, files!.length));
    }

    Promise.all(promises).then(() => { setIsLodaing(false); setIsOver(false); }).done();
  }

  function setNewError(newError: string) {
    setErrors([...errors, newError]);
  }

  function uploadFile(file: File, index: number, count: number): Promise<void> {

    return new Promise((resolve) => {
      if (file.type && p.accept) {


        if (!p.accept.split(',').some(accept => file.type.startsWith(accept.replace("*", "")))) {
          setNewError(FileMessage.TheFile0IsNotA1.niceToString(file.name, p.accept));
          return resolve();
        }
      }

      if (p.maxSizeInBytes != null && p.maxSizeInBytes < file.size) {
        setNewError(FileMessage.File0IsTooBigTheMaximumSizeIs1.niceToString(file.name, bytesToSize(p.maxSizeInBytes)));
        return resolve();
      }

      if (file.name.contains("%")) {
        setNewError(FileMessage.TheNameOfTheFileMustNotContainPercentSymbol.niceToString());
        return resolve();
      }

      const fileReader = new FileReader();
      fileReader.onerror = e => { setTimeout(() => { throw (e as any).error; }, 0); };
      fileReader.onload = e => {
        const fileEntity = New(p.typeName) as ModifiableEntity & IFile;
        fileEntity.fileName = file.name;
        fileEntity.binaryFile = ((e.target as any).result as string).after("base64,");

        if (p.fileType)
          (fileEntity as any as IFilePath).fileType = p.fileType;


        p.onFileLoaded(fileEntity, index, count, file);
        resolve();
      };
      fileReader.readAsDataURL(file);
    });
  }

  return (
    <div {...p.divHtmlAttributes}>
      {isLoading ? <div className="sf-file-drop">{JavascriptMessage.loading.niceToString()}</div> :
        (p.dragAndDrop ? <div className={classes("sf-file-drop", isOver ? "sf-file-drop-over" : undefined)}
          onDragEnter={handleDragOver}
          onDragOver={handleDragOver}
          onDragLeave={handleDragLeave}
          onDrop={handleDrop}
        >
          <div className={classes("sf-upload btn btn-light", p.buttonCss)}>
            <FontAwesomeIcon icon="upload" />
            {FileMessage.SelectFile.niceToString()}
            <input type='file' accept={p.accept} onChange={handleFileChange} multiple={p.multiple} />
          </div>
          &nbsp;{p.dragAndDropMessage ?? FileMessage.OrDragAFileHere.niceToString()}
        </div> :
          <div className={classes("sf-upload btn btn-light", p.buttonCss)}>
            <FontAwesomeIcon icon="upload" className="mr-1" />
            {FileMessage.SelectFile.niceToString()}
            <input type='file' accept={p.accept} onChange={handleFileChange} multiple={p.multiple} />
          </div>
        )
      }
      {errors.map((e, i) => <p key={i} className="text-danger">{e}</p>)}
    </div>
  );
}

FileUploader.defaultProps = {
  dragAndDrop: true
};
