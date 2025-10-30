//////////////////////////////////
//Auto-generated. Do NOT modify!//
//////////////////////////////////

import { MessageKey, QueryKey, Type, EnumType, registerSymbol } from '../../Signum/React/Reflection'
import * as Entities from '../../Signum/React/Signum.Entities'
import * as Basics from '../../Signum/React/Signum.Basics'
import * as Operations from '../../Signum/React/Signum.Operations'

export interface IFile
{
  __isFile__ : true; //only for type-checking
  binaryFile?: string | null;
  fileName?: string | null;

}

export interface FileEntity extends IFile { }
export interface FileEmbedded extends IFile { }

export interface IFilePath extends IFile
{
  fileType?: FileTypeSymbol | null;
  suffix?: string | null;
  hash?: string | null;
  fullWebPath?: string | null;
  fileLength: number;
  __uploadingOffset?: number;
  __abortController?: AbortController;
  __uploadId?: string;
}

export interface FilePathEntity extends IFilePath { }
export interface FilePathEmbedded extends IFilePath {
  entityId: number | string;
  mListRowId: number | string | null;
  propertyRoute: string;
  rootType: string;
}

export const BigStringMixin: Type<BigStringMixin> = new Type<BigStringMixin>("BigStringMixin");
export interface BigStringMixin extends Entities.MixinEntity {
  Type: "BigStringMixin";
  file: FilePathEmbedded | null;
}

export const FileEmbedded: Type<FileEmbedded> = new Type<FileEmbedded>("FileEmbedded");
export interface FileEmbedded extends Entities.EmbeddedEntity {
  Type: "FileEmbedded";
  fileName: string;
  binaryFile: string /*Byte[]*/;
}

export const FileEntity: Type<FileEntity> = new Type<FileEntity>("File");
export interface FileEntity extends Entities.ImmutableEntity {
  Type: "File";
  fileName: string;
  hash: string;
  binaryFile: string /*Byte[]*/;
}

export namespace FileMessage {
  export const DownloadFile: MessageKey = new MessageKey("FileMessage", "DownloadFile");
  export const ErrorSavingFile: MessageKey = new MessageKey("FileMessage", "ErrorSavingFile");
  export const FileTypes: MessageKey = new MessageKey("FileMessage", "FileTypes");
  export const Open: MessageKey = new MessageKey("FileMessage", "Open");
  export const OpeningHasNotDefaultImplementationFor0: MessageKey = new MessageKey("FileMessage", "OpeningHasNotDefaultImplementationFor0");
  export const WebDownload: MessageKey = new MessageKey("FileMessage", "WebDownload");
  export const WebImage: MessageKey = new MessageKey("FileMessage", "WebImage");
  export const Remove: MessageKey = new MessageKey("FileMessage", "Remove");
  export const SavingHasNotDefaultImplementationFor0: MessageKey = new MessageKey("FileMessage", "SavingHasNotDefaultImplementationFor0");
  export const SelectFile: MessageKey = new MessageKey("FileMessage", "SelectFile");
  export const ViewFile: MessageKey = new MessageKey("FileMessage", "ViewFile");
  export const ViewingHasNotDefaultImplementationFor0: MessageKey = new MessageKey("FileMessage", "ViewingHasNotDefaultImplementationFor0");
  export const OnlyOneFileIsSupported: MessageKey = new MessageKey("FileMessage", "OnlyOneFileIsSupported");
  export const OrDragAFileHere: MessageKey = new MessageKey("FileMessage", "OrDragAFileHere");
  export const TheFile0IsNotA1: MessageKey = new MessageKey("FileMessage", "TheFile0IsNotA1");
  export const File0IsTooBigTheMaximumSizeIs1: MessageKey = new MessageKey("FileMessage", "File0IsTooBigTheMaximumSizeIs1");
  export const TheNameOfTheFileMustNotContainPercentSymbol: MessageKey = new MessageKey("FileMessage", "TheNameOfTheFileMustNotContainPercentSymbol");
  export const FileImage: MessageKey = new MessageKey("FileMessage", "FileImage");
  export const File0IsStillUploading: MessageKey = new MessageKey("FileMessage", "File0IsStillUploading");
  export const Uploading01: MessageKey = new MessageKey("FileMessage", "Uploading01");
  export const SaveThe0WhenFinished: MessageKey = new MessageKey("FileMessage", "SaveThe0WhenFinished");
  export const AddMoreFiles: MessageKey = new MessageKey("FileMessage", "AddMoreFiles");
}

export const FilePathEmbedded: Type<FilePathEmbedded> = new Type<FilePathEmbedded>("FilePathEmbedded");
export interface FilePathEmbedded extends Entities.EmbeddedEntity {
  Type: "FilePathEmbedded";
  fileName: string;
  binaryFile: string /*Byte[]*/;
  hash: string | null;
  fileLength: number;
  suffix: string;
  fileType: FileTypeSymbol;
}

export const FilePathEntity: Type<FilePathEntity> = new Type<FilePathEntity>("FilePath");
export interface FilePathEntity extends Entities.Entity {
  Type: "FilePath";
  creationDate: string /*DateTime*/;
  fileName: string;
  binaryFile: string /*Byte[]*/;
  hash: string | null;
  fileLength: number;
  suffix: string;
  fileType: FileTypeSymbol;
}

export namespace FilePathOperation {
  export const Save : Operations.ExecuteSymbol<FilePathEntity> = registerSymbol("Operation", "FilePathOperation.Save");
}

export const FileTypeSymbol: Type<FileTypeSymbol> = new Type<FileTypeSymbol>("FileType");
export interface FileTypeSymbol extends Basics.Symbol {
  Type: "FileType";
}

