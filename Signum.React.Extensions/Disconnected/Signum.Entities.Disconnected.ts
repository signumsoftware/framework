//////////////////////////////////
//Auto-generated. Do NOT modify!//
//////////////////////////////////
import { MessageKey, QueryKey, Type, EnumType, registerSymbol } from 'Framework/Signum.React/Scripts/Reflection' 

import * as Entities from 'Framework/Signum.React/Scripts/Signum.Entities' 
export const DisconnectedCreatedMixin_Type = new Type<DisconnectedCreatedMixin>("DisconnectedCreatedMixin");
export interface DisconnectedCreatedMixin extends Entities.MixinEntity {
    disconnectedCreated?: boolean;
}

export const DisconnectedExportEntity_Type = new Type<DisconnectedExportEntity>("DisconnectedExportEntity");
export interface DisconnectedExportEntity extends Entities.Entity {
    creationDate?: string;
    machine?: Entities.Lite<DisconnectedMachineEntity>;
    lock?: number;
    createDatabase?: number;
    createSchema?: number;
    disableForeignKeys?: number;
    copies?: Entities.MList<DisconnectedExportTableEntity>;
    enableForeignKeys?: number;
    reseedIds?: number;
    backupDatabase?: number;
    dropDatabase?: number;
    total?: number;
    state?: DisconnectedExportState;
    exception?: Entities.Lite<Entities.Basics.ExceptionEntity>;
}

export enum DisconnectedExportState {
    InProgress = "InProgress" as any,
    Completed = "Completed" as any,
    Error = "Error" as any,
}
export const DisconnectedExportState_Type = new EnumType<DisconnectedExportState>("DisconnectedExportState", DisconnectedExportState);

export const DisconnectedExportTableEntity_Type = new Type<DisconnectedExportTableEntity>("DisconnectedExportTableEntity");
export interface DisconnectedExportTableEntity extends Entities.EmbeddedEntity {
    type?: Entities.Lite<Entities.Basics.TypeEntity>;
    copyTable?: number;
    errors?: string;
}

export const DisconnectedImportEntity_Type = new Type<DisconnectedImportEntity>("DisconnectedImportEntity");
export interface DisconnectedImportEntity extends Entities.Entity {
    creationDate?: string;
    machine?: Entities.Lite<DisconnectedMachineEntity>;
    restoreDatabase?: number;
    synchronizeSchema?: number;
    disableForeignKeys?: number;
    copies?: Entities.MList<DisconnectedImportTableEntity>;
    unlock?: number;
    enableForeignKeys?: number;
    dropDatabase?: number;
    total?: number;
    state?: DisconnectedImportState;
    exception?: Entities.Lite<Entities.Basics.ExceptionEntity>;
}

export enum DisconnectedImportState {
    InProgress = "InProgress" as any,
    Completed = "Completed" as any,
    Error = "Error" as any,
}
export const DisconnectedImportState_Type = new EnumType<DisconnectedImportState>("DisconnectedImportState", DisconnectedImportState);

export const DisconnectedImportTableEntity_Type = new Type<DisconnectedImportTableEntity>("DisconnectedImportTableEntity");
export interface DisconnectedImportTableEntity extends Entities.EmbeddedEntity {
    type?: Entities.Lite<Entities.Basics.TypeEntity>;
    copyTable?: number;
    disableForeignKeys?: boolean;
    insertedRows?: number;
    updatedRows?: number;
    insertedOrUpdated?: number;
}

export const DisconnectedMachineEntity_Type = new Type<DisconnectedMachineEntity>("DisconnectedMachineEntity");
export interface DisconnectedMachineEntity extends Entities.Entity {
    creationDate?: string;
    machineName?: string;
    state?: DisconnectedMachineState;
    seedMin?: number;
    seedMax?: number;
}

export module DisconnectedMachineOperation {
    export const Save : Entities.ExecuteSymbol<DisconnectedMachineEntity> = registerSymbol({ key: "DisconnectedMachineOperation.Save" });
    export const UnsafeUnlock : Entities.ExecuteSymbol<DisconnectedMachineEntity> = registerSymbol({ key: "DisconnectedMachineOperation.UnsafeUnlock" });
    export const FixImport : Entities.ConstructSymbol_From<DisconnectedImportEntity, DisconnectedMachineEntity> = registerSymbol({ key: "DisconnectedMachineOperation.FixImport" });
}

export enum DisconnectedMachineState {
    Connected = "Connected" as any,
    Disconnected = "Disconnected" as any,
    Faulted = "Faulted" as any,
    Fixed = "Fixed" as any,
}
export const DisconnectedMachineState_Type = new EnumType<DisconnectedMachineState>("DisconnectedMachineState", DisconnectedMachineState);

export module DisconnectedMessage {
    export const NotAllowedToSave0WhileOffline = new MessageKey("DisconnectedMessage", "NotAllowedToSave0WhileOffline");
    export const The0WithId12IsLockedBy3 = new MessageKey("DisconnectedMessage", "The0WithId12IsLockedBy3");
    export const Imports = new MessageKey("DisconnectedMessage", "Imports");
    export const Exports = new MessageKey("DisconnectedMessage", "Exports");
    export const _0OverlapsWith1 = new MessageKey("DisconnectedMessage", "_0OverlapsWith1");
}

export const DisconnectedSubsetMixin_Type = new Type<DisconnectedSubsetMixin>("DisconnectedSubsetMixin");
export interface DisconnectedSubsetMixin extends Entities.MixinEntity {
    lastOnlineTicks?: number;
    disconnectedMachine?: Entities.Lite<DisconnectedMachineEntity>;
}

