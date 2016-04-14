//////////////////////////////////
//Auto-generated. Do NOT modify!//
//////////////////////////////////
import { MessageKey, QueryKey, Type, EnumType, registerSymbol } from '../../../Framework/Signum.React/Scripts/Reflection' 

import * as Entities from '../../../Framework/Signum.React/Scripts/Signum.Entities' 

import * as Basics from '../../../Framework/Signum.React/Scripts/Signum.Entities.Basics' 


export const DisconnectedCreatedMixin = new Type<DisconnectedCreatedMixin>("DisconnectedCreatedMixin");
export interface DisconnectedCreatedMixin extends Entities.MixinEntity {
    disconnectedCreated: boolean;
}

export const DisconnectedExportEntity = new Type<DisconnectedExportEntity>("DisconnectedExport");
export interface DisconnectedExportEntity extends Entities.Entity {
    creationDate: string;
    machine: Entities.Lite<DisconnectedMachineEntity>;
    lock: number;
    createDatabase: number;
    createSchema: number;
    disableForeignKeys: number;
    copies: Entities.MList<DisconnectedExportTableEntity>;
    enableForeignKeys: number;
    reseedIds: number;
    backupDatabase: number;
    dropDatabase: number;
    total: number;
    state: DisconnectedExportState;
    exception: Entities.Lite<Basics.ExceptionEntity>;
}

export const DisconnectedExportState = new EnumType<DisconnectedExportState>("DisconnectedExportState");
export type DisconnectedExportState =
    "InProgress" |
    "Completed" |
    "Error";

export const DisconnectedExportTableEntity = new Type<DisconnectedExportTableEntity>("DisconnectedExportTableEntity");
export interface DisconnectedExportTableEntity extends Entities.EmbeddedEntity {
    type: Entities.Lite<Basics.TypeEntity>;
    copyTable: number;
    errors: string;
}

export const DisconnectedImportEntity = new Type<DisconnectedImportEntity>("DisconnectedImport");
export interface DisconnectedImportEntity extends Entities.Entity {
    creationDate: string;
    machine: Entities.Lite<DisconnectedMachineEntity>;
    restoreDatabase: number;
    synchronizeSchema: number;
    disableForeignKeys: number;
    copies: Entities.MList<DisconnectedImportTableEntity>;
    unlock: number;
    enableForeignKeys: number;
    dropDatabase: number;
    total: number;
    state: DisconnectedImportState;
    exception: Entities.Lite<Basics.ExceptionEntity>;
}

export const DisconnectedImportState = new EnumType<DisconnectedImportState>("DisconnectedImportState");
export type DisconnectedImportState =
    "InProgress" |
    "Completed" |
    "Error";

export const DisconnectedImportTableEntity = new Type<DisconnectedImportTableEntity>("DisconnectedImportTableEntity");
export interface DisconnectedImportTableEntity extends Entities.EmbeddedEntity {
    type: Entities.Lite<Basics.TypeEntity>;
    copyTable: number;
    disableForeignKeys: boolean;
    insertedRows: number;
    updatedRows: number;
    insertedOrUpdated: number;
}

export const DisconnectedMachineEntity = new Type<DisconnectedMachineEntity>("DisconnectedMachine");
export interface DisconnectedMachineEntity extends Entities.Entity {
    creationDate: string;
    machineName: string;
    state: DisconnectedMachineState;
    seedMin: number;
    seedMax: number;
}

export module DisconnectedMachineOperation {
    export const Save : Entities.ExecuteSymbol<DisconnectedMachineEntity> = registerSymbol({ Type: "Operation", key: "DisconnectedMachineOperation.Save" });
    export const UnsafeUnlock : Entities.ExecuteSymbol<DisconnectedMachineEntity> = registerSymbol({ Type: "Operation", key: "DisconnectedMachineOperation.UnsafeUnlock" });
    export const FixImport : Entities.ConstructSymbol_From<DisconnectedImportEntity, DisconnectedMachineEntity> = registerSymbol({ Type: "Operation", key: "DisconnectedMachineOperation.FixImport" });
}

export const DisconnectedMachineState = new EnumType<DisconnectedMachineState>("DisconnectedMachineState");
export type DisconnectedMachineState =
    "Connected" |
    "Disconnected" |
    "Faulted" |
    "Fixed";

export module DisconnectedMessage {
    export const NotAllowedToSave0WhileOffline = new MessageKey("DisconnectedMessage", "NotAllowedToSave0WhileOffline");
    export const The0WithId12IsLockedBy3 = new MessageKey("DisconnectedMessage", "The0WithId12IsLockedBy3");
    export const Imports = new MessageKey("DisconnectedMessage", "Imports");
    export const Exports = new MessageKey("DisconnectedMessage", "Exports");
    export const _0OverlapsWith1 = new MessageKey("DisconnectedMessage", "_0OverlapsWith1");
}

export const DisconnectedSubsetMixin = new Type<DisconnectedSubsetMixin>("DisconnectedSubsetMixin");
export interface DisconnectedSubsetMixin extends Entities.MixinEntity {
    lastOnlineTicks: number;
    disconnectedMachine: Entities.Lite<DisconnectedMachineEntity>;
}

export const Download = new EnumType<Download>("Download");
export type Download =
    "None" |
    "All" |
    "Subset" |
    "Replace";

export const Upload = new EnumType<Upload>("Upload");
export type Upload =
    "None" |
    "New" |
    "Subset";

