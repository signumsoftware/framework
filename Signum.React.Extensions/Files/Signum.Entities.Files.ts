//////////////////////////////////
//Auto-generated. Do NOT modify!//
//////////////////////////////////

import { MessageKey, QueryKey, Type, EnumType, registerSymbol } from '../../../Framework/Signum.React/Scripts/Reflection'
import * as Entities from '../../../Framework/Signum.React/Scripts/Signum.Entities'
import * as Patterns from '../../../Framework/Signum.React/Scripts/Signum.Entities.Patterns'


export interface IFile
{
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
export interface FilePathEmbedded extends IFilePath { }
export const FileEmbedded = new Type<FileEmbedded>("FileEmbedded");
export interface FileEmbedded extends Entities.EmbeddedEntity {
    Type: "FileEmbedded";
    fileName?: string | null;
    binaryFile?: string | null;
}

export const FileEntity = new Type<FileEntity>("File");
export interface FileEntity extends Entities.ImmutableEntity {
    Type: "File";
    fileName?: string | null;
    hash?: string | null;
    binaryFile?: string | null;
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

export const FilePathEmbedded = new Type<FilePathEmbedded>("FilePathEmbedded");
export interface FilePathEmbedded extends Entities.EmbeddedEntity {
    Type: "FilePathEmbedded";
    fileName?: string | null;
    binaryFile?: string | null;
    fileLength?: number;
    suffix?: string | null;
    calculatedDirectory?: string | null;
    fileType?: FileTypeSymbol | null;
}

export const FilePathEntity = new Type<FilePathEntity>("FilePath");
export interface FilePathEntity extends Patterns.LockableEntity {
    Type: "FilePath";
    creationDate?: string;
    fileName?: string | null;
    binaryFile?: string | null;
    fileLength?: number;
    suffix?: string | null;
    calculatedDirectory?: string | null;
    fileType?: FileTypeSymbol | null;
}

export module FilePathOperation {
    export const Save : Entities.ExecuteSymbol<FilePathEntity> = registerSymbol("Operation", "FilePathOperation.Save");
}

export const FileTypeSymbol = new Type<FileTypeSymbol>("FileType");
export interface FileTypeSymbol extends Entities.Symbol {
    Type: "FileType";
}


