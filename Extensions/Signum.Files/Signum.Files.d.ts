import { MessageKey, Type } from '../../Signum/React/Reflection';
import * as Entities from '../../Signum/React/Signum.Entities';
import * as Basics from '../../Signum/React/Signum.Basics';
import * as Operations from '../../Signum/React/Signum.Operations';
export interface IFile {
    __isFile__: true;
    binaryFile?: string | null;
    fileName?: string | null;
    fullWebPath?: string | null;
}
export interface FileEntity extends IFile {
}
export interface FileEmbedded extends IFile {
}
export interface IFilePath extends IFile {
    fileType?: FileTypeSymbol | null;
    suffix?: string | null;
}
export interface FilePathEntity extends IFilePath {
}
export interface FilePathEmbedded extends IFilePath {
    entityId: number | string;
    mListRowId: number | string | null;
    propertyRoute: string;
    rootType: string;
}
export declare const BigStringMixin: Type<BigStringMixin>;
export interface BigStringMixin extends Entities.MixinEntity {
    Type: "BigStringMixin";
    file: FilePathEmbedded | null;
}
export declare const FileEmbedded: Type<FileEmbedded>;
export interface FileEmbedded extends Entities.EmbeddedEntity {
    Type: "FileEmbedded";
    fileName: string;
    binaryFile: string;
}
export declare const FileEntity: Type<FileEntity>;
export interface FileEntity extends Entities.ImmutableEntity {
    Type: "File";
    fileName: string;
    hash: string;
    binaryFile: string;
}
export declare module FileMessage {
    const DownloadFile: MessageKey;
    const ErrorSavingFile: MessageKey;
    const FileTypes: MessageKey;
    const Open: MessageKey;
    const OpeningHasNotDefaultImplementationFor0: MessageKey;
    const WebDownload: MessageKey;
    const WebImage: MessageKey;
    const Remove: MessageKey;
    const SavingHasNotDefaultImplementationFor0: MessageKey;
    const SelectFile: MessageKey;
    const ViewFile: MessageKey;
    const ViewingHasNotDefaultImplementationFor0: MessageKey;
    const OnlyOneFileIsSupported: MessageKey;
    const OrDragAFileHere: MessageKey;
    const TheFile0IsNotA1: MessageKey;
    const File0IsTooBigTheMaximumSizeIs1: MessageKey;
    const TheNameOfTheFileMustNotContainPercentSymbol: MessageKey;
    const FileImage: MessageKey;
}
export declare const FilePathEmbedded: Type<FilePathEmbedded>;
export interface FilePathEmbedded extends Entities.EmbeddedEntity {
    Type: "FilePathEmbedded";
    fileName: string;
    binaryFile: string;
    hash: string | null;
    fileLength: number;
    suffix: string;
    fileType: FileTypeSymbol;
}
export declare const FilePathEntity: Type<FilePathEntity>;
export interface FilePathEntity extends Entities.Entity {
    Type: "FilePath";
    creationDate: string;
    fileName: string;
    binaryFile: string;
    hash: string | null;
    fileLength: number;
    suffix: string;
    fileType: FileTypeSymbol;
}
export declare module FilePathOperation {
    const Save: Operations.ExecuteSymbol<FilePathEntity>;
}
export declare const FileTypeSymbol: Type<FileTypeSymbol>;
export interface FileTypeSymbol extends Basics.Symbol {
    Type: "FileType";
}
//# sourceMappingURL=Signum.Files.d.ts.map