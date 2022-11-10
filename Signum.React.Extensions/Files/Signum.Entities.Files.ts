//////////////////////////////////
//Auto-generated. Do NOT modify!//
//////////////////////////////////

import { MessageKey, QueryKey, Type, EnumType, registerSymbol } from '../../Signum.React/Scripts/Reflection'
import * as Entities from '../../Signum.React/Scripts/Signum.Entities'

export interface IFile
{
  __isFile__ : true; //only for type-checking
  binaryFile?: string | null;
  fileName?: string | null;
  fullWebPath?: string | null;
}

export interface FileEntity extends IFile { }
export interface FileEmbedded extends IFile { }

export interface IFilePath extends IFile
{
  fileType?: FileTypeSymbol | null;
  suffix?: string | null;
}

export interface FilePathEntity extends IFilePath { }
export interface FilePathEmbedded extends IFilePath {
  entityId: number | string;
  mListRowId: number | string | null;
  propertyRoute: string;
  rootType: string;
}

export const BigStringMixin = new Type<BigStringMixin>("BigStringMixin");
export interface BigStringMixin extends Entities.MixinEntity {
  Type: "BigStringMixin";
  file: FilePathEmbedded | null;
}

export const FileEmbedded = new Type<FileEmbedded>("FileEmbedded");
export interface FileEmbedded extends Entities.EmbeddedEntity {
  Type: "FileEmbedded";
  fileName: string;
  binaryFile: string /*Byte[]*/;
}

export const FileEntity = new Type<FileEntity>("File");
export interface FileEntity extends Entities.ImmutableEntity {
  Type: "File";
  fileName: string;
  hash: string;
  binaryFile: string /*Byte[]*/;
}

export module FileMessage {
  export const DownloadFile = new MessageKey("FileMessage", "DownloadFile");
  export const ErrorSavingFile = new MessageKey("FileMessage", "ErrorSavingFile");
  export const FileTypes = new MessageKey("FileMessage", "FileTypes");
  export const Open = new MessageKey("FileMessage", "Open");
  export const OpeningHasNotDefaultImplementationFor0 = new MessageKey("FileMessage", "OpeningHasNotDefaultImplementationFor0");
  export const WebDownload = new MessageKey("FileMessage", "WebDownload");
  export const WebImage = new MessageKey("FileMessage", "WebImage");
  export const Remove = new MessageKey("FileMessage", "Remove");
  export const SavingHasNotDefaultImplementationFor0 = new MessageKey("FileMessage", "SavingHasNotDefaultImplementationFor0");
  export const SelectFile = new MessageKey("FileMessage", "SelectFile");
  export const ViewFile = new MessageKey("FileMessage", "ViewFile");
  export const ViewingHasNotDefaultImplementationFor0 = new MessageKey("FileMessage", "ViewingHasNotDefaultImplementationFor0");
  export const OnlyOneFileIsSupported = new MessageKey("FileMessage", "OnlyOneFileIsSupported");
  export const OrDragAFileHere = new MessageKey("FileMessage", "OrDragAFileHere");
  export const TheFile0IsNotA1 = new MessageKey("FileMessage", "TheFile0IsNotA1");
  export const File0IsTooBigTheMaximumSizeIs1 = new MessageKey("FileMessage", "File0IsTooBigTheMaximumSizeIs1");
  export const TheNameOfTheFileMustNotContainPercentSymbol = new MessageKey("FileMessage", "TheNameOfTheFileMustNotContainPercentSymbol");
}

export const FilePathEmbedded = new Type<FilePathEmbedded>("FilePathEmbedded");
export interface FilePathEmbedded extends Entities.EmbeddedEntity {
  Type: "FilePathEmbedded";
  fileName: string;
  binaryFile: string /*Byte[]*/;
  hash: string | null;
  fileLength: number;
  suffix: string;
  fileType: FileTypeSymbol;
}

export const FilePathEntity = new Type<FilePathEntity>("FilePath");
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

export module FilePathOperation {
  export const Save : Entities.ExecuteSymbol<FilePathEntity> = registerSymbol("Operation", "FilePathOperation.Save");
}

export const FileTypeSymbol = new Type<FileTypeSymbol>("FileType");
export interface FileTypeSymbol extends Entities.Symbol {
  Type: "FileType";
}


