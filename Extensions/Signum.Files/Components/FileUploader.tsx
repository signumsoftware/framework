import * as React from 'react'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { classes, bytesToSize } from '@framework/Globals'
import { ModifiableEntity, JavascriptMessage } from '@framework/Signum.Entities'
import { IFile, IFilePath, FileMessage, FileTypeSymbol } from '../Signum.Files'

import "./Files.css"
import { New, PseudoType } from '@framework/Reflection';

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
  fileDropCssClass?: string;
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
    for (let i = 0; i < files!.length; i++) {
      promises.push(toFileEntity(files![i], { accept: p.accept, type: p.typeName, fileType: p.fileType, maxSizeInBytes: p.maxSizeInBytes })
        .then(fileEntity => p.onFileLoaded(fileEntity, i, files!.length, files[i]))
        .catch(error => setNewError((error as Error).message)));
    }

    Promise.all(promises).then(() => { setIsLodaing(false); setIsOver(false); });
  }

  function setNewError(newError: string) {
    setErrors(errors => [...errors, newError]);
  }

  return (
    <div {...p.divHtmlAttributes}>
      {isLoading ? <div className="sf-file-drop">{JavascriptMessage.loading.niceToString()}</div> :
        (p.dragAndDrop ? <div className={classes("sf-file-drop", p.fileDropCssClass, isOver ? "sf-file-drop-over" : undefined)}
          onDragEnter={handleDragOver}
          onDragOver={handleDragOver}
          onDragLeave={handleDragLeave}
          onDrop={handleDrop}
        >
          <div className={classes("sf-upload btn btn-light", p.buttonCss)}>
            <FontAwesomeIcon icon="upload" className="me-2" />
            {FileMessage.SelectFile.niceToString()}
            <input type='file' accept={p.accept} onChange={handleFileChange} multiple={p.multiple} />
          </div>
          &nbsp;{p.dragAndDropMessage ?? FileMessage.OrDragAFileHere.niceToString()}
        </div> :
          <div className={classes("sf-upload btn btn-light", p.buttonCss)}>
            <FontAwesomeIcon icon="upload" className="me-1" />
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

export function toFileEntity(file: File, o: { accept?: string, maxSizeInBytes?: number, fileType?: FileTypeSymbol, type: PseudoType }): Promise<ModifiableEntity & IFile> {

  return new Promise((resolve, reject) => {
    if (file.type && o.accept) {
      if (!o.accept.split(',').some(accept => file.type.startsWith(accept.replace("*", "")))) {
        reject(new Error(FileMessage.TheFile0IsNotA1.niceToString(file.name, o.accept)));
        return;
      }
    }

    if (o.maxSizeInBytes != null && o.maxSizeInBytes < file.size) {
      reject(new Error(FileMessage.File0IsTooBigTheMaximumSizeIs1.niceToString(file.name, bytesToSize(o.maxSizeInBytes))));
      return;
    }

    if (file.name.contains("%")) {
      reject(new Error(FileMessage.TheNameOfTheFileMustNotContainPercentSymbol.niceToString()));
      return;
    }

    const fileReader = new FileReader();
    fileReader.onerror = e => { reject(e); };
    fileReader.onload = e => {
      const fileEntity = New(o.type) as ModifiableEntity & IFile;
      fileEntity.fileName = file.name;
      fileEntity.binaryFile = ((e.target as any).result as string).after("base64,");

      if (o.fileType)
        (fileEntity as any as IFilePath).fileType = o.fileType;

      resolve(fileEntity);
    };
    fileReader.readAsDataURL(file);
  });
}
