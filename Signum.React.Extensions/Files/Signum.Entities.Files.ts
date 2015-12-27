//////////////////////////////////
//Auto-generated. Do NOT modify!//
//////////////////////////////////
import { MessageKey, QueryKey, Type, EnumType, registerSymbol } from 'Framework/Signum.React/Scripts/Reflection' 

import * as Entities from 'Framework/Signum.React/Scripts/Signum.Entities' 
export const EmbeddedFileEntity_Type = new Type<EmbeddedFileEntity>("EmbeddedFile");
export interface EmbeddedFileEntity extends Entities.EmbeddedEntity {
    fileName?: string;
    binaryFile?: string;
    fullWebPath?: string;
}

export const EmbeddedFilePathEntity_Type = new Type<EmbeddedFilePathEntity>("EmbeddedFilePath");
export interface EmbeddedFilePathEntity extends Entities.EmbeddedEntity {
    fileName?: string;
    binaryFile?: string;
    fileLength?: number;
    fileLengthString?: string;
    sufix?: string;
    calculatedDirectory?: string;
    fileType?: FileTypeSymbol;
    fullPhysicalPath?: string;
    fullWebPath?: string;
}

export const FileEntity_Type = new Type<FileEntity>("File");
export interface FileEntity extends Entities.ImmutableEntity {
    fileName?: string;
    hash?: string;
    binaryFile?: string;
    fullWebPath?: string;
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
}

export const FilePathEntity_Type = new Type<FilePathEntity>("FilePath");
export interface FilePathEntity extends Entities.Patterns.LockableEntity {
    creationDate?: string;
    fileName?: string;
    binaryFile?: string;
    fileLength?: number;
    fileLengthString?: string;
    sufix?: string;
    calculatedDirectory?: string;
    fileType?: FileTypeSymbol;
    fullPhysicalPath?: string;
    fullWebPath?: string;
}

export module FilePathOperation {
    export const Save : Entities.ExecuteSymbol<FilePathEntity> = registerSymbol({ Type: "Operation", key: "FilePathOperation.Save" });
}

export const FileTypeSymbol_Type = new Type<FileTypeSymbol>("FileType");
export interface FileTypeSymbol extends Entities.Symbol {
}

