//////////////////////////////////
//Auto-generated. Do NOT modify!//
//////////////////////////////////
import { MessageKey, QueryKey, Type, EnumType, registerSymbol } from '../../../Framework/Signum.React/Scripts/Reflection' 

import * as Entities from '../../../Framework/Signum.React/Scripts/Signum.Entities' 

import * as Basics from '../../../Framework/Signum.React/Scripts/Signum.Entities.Basics' 

import * as Patterns from '../../../Framework/Signum.React/Scripts/Signum.Entities.Patterns' 


export interface IFile
{
    binaryFile: string;
    fileName: string 
    fullWebPath : string 
}

export interface FileEntity extends IFile { }
export interface EmbeddedFileEntity extends IFile { }

export interface IFilePath extends IFile
{
   fullPhysicalPath : string
   fileType: FileTypeSymbol 
   suffix : string
}

export interface FilePathEntity extends IFilePath { }
export interface EmbeddedFilePathEntity extends IFilePath { }
export const EmbeddedFileEntity = new Type<EmbeddedFileEntity>("EmbeddedFileEntity");
export interface EmbeddedFileEntity extends Entities.EmbeddedEntity {
    fileName: string;
    binaryFile: string;
}

export const EmbeddedFilePathEntity = new Type<EmbeddedFilePathEntity>("EmbeddedFilePathEntity");
export interface EmbeddedFilePathEntity extends Entities.EmbeddedEntity {
    fileName: string;
    binaryFile: string;
    fileLength: number;
    fileLengthString: string;
    suffix: string;
    calculatedDirectory: string;
    fileType: FileTypeSymbol;
}

export const FileEntity = new Type<FileEntity>("File");
export interface FileEntity extends Entities.ImmutableEntity {
    fileName: string;
    hash: string;
    binaryFile: string;
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
    export const DragAndDropHere = new MessageKey("FileMessage", "DragAndDropHere");
}

export const FilePathEntity = new Type<FilePathEntity>("FilePath");
export interface FilePathEntity extends Patterns.LockableEntity {
    creationDate: string;
    fileName: string;
    binaryFile: string;
    fileLength: number;
    fileLengthString: string;
    suffix: string;
    calculatedDirectory: string;
    fileType: FileTypeSymbol;
}

export module FilePathOperation {
    export const Save : Entities.ExecuteSymbol<FilePathEntity> = registerSymbol({ Type: "Operation", key: "FilePathOperation.Save" });
}

export const FileTypeSymbol = new Type<FileTypeSymbol>("FileType");
export interface FileTypeSymbol extends Entities.Symbol {
}

