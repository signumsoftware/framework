//////////////////////////////////
//Auto-generated. Do NOT modify!//
//////////////////////////////////

import { MessageKey, QueryKey, Type, EnumType, registerSymbol } from '../../Signum/React/Reflection'
import * as Entities from '../../Signum/React/Signum.Entities'
import * as Basics from '../../Signum/React/Signum.Basics'
import * as Operations from '../../Signum/React/Signum.Operations'


export const DisconnectedCreatedMixin: Type<DisconnectedCreatedMixin> = new Type<DisconnectedCreatedMixin>("DisconnectedCreatedMixin");
export interface DisconnectedCreatedMixin extends Entities.MixinEntity {
  Type: "DisconnectedCreatedMixin";
  disconnectedCreated: boolean;
}

export const DisconnectedExportEntity: Type<DisconnectedExportEntity> = new Type<DisconnectedExportEntity>("DisconnectedExport");
export interface DisconnectedExportEntity extends Entities.Entity {
  Type: "DisconnectedExport";
  creationDate: string /*DateTime*/;
  machine: Entities.Lite<DisconnectedMachineEntity>;
  lock: number | null;
  createDatabase: number | null;
  createSchema: number | null;
  disableForeignKeys: number | null;
  copies: Entities.MList<DisconnectedExportTableEmbedded>;
  enableForeignKeys: number | null;
  reseedIds: number | null;
  backupDatabase: number | null;
  dropDatabase: number | null;
  total: number | null;
  state: DisconnectedExportState;
  exception: Entities.Lite<Basics.ExceptionEntity> | null;
}

export const DisconnectedExportState: EnumType<DisconnectedExportState> = new EnumType<DisconnectedExportState>("DisconnectedExportState");
export type DisconnectedExportState =
  "InProgress" |
  "Completed" |
  "Error";

export const DisconnectedExportTableEmbedded: Type<DisconnectedExportTableEmbedded> = new Type<DisconnectedExportTableEmbedded>("DisconnectedExportTableEmbedded");
export interface DisconnectedExportTableEmbedded extends Entities.EmbeddedEntity {
  Type: "DisconnectedExportTableEmbedded";
  type: Entities.Lite<Basics.TypeEntity>;
  copyTable: number | null;
  errors: string;
}

export const DisconnectedImportEntity: Type<DisconnectedImportEntity> = new Type<DisconnectedImportEntity>("DisconnectedImport");
export interface DisconnectedImportEntity extends Entities.Entity {
  Type: "DisconnectedImport";
  creationDate: string /*DateTime*/;
  machine: Entities.Lite<DisconnectedMachineEntity> | null;
  restoreDatabase: number | null;
  synchronizeSchema: number | null;
  disableForeignKeys: number | null;
  copies: Entities.MList<DisconnectedImportTableEmbedded>;
  unlock: number | null;
  enableForeignKeys: number | null;
  dropDatabase: number | null;
  total: number | null;
  state: DisconnectedImportState;
  exception: Entities.Lite<Basics.ExceptionEntity> | null;
}

export const DisconnectedImportState: EnumType<DisconnectedImportState> = new EnumType<DisconnectedImportState>("DisconnectedImportState");
export type DisconnectedImportState =
  "InProgress" |
  "Completed" |
  "Error";

export const DisconnectedImportTableEmbedded: Type<DisconnectedImportTableEmbedded> = new Type<DisconnectedImportTableEmbedded>("DisconnectedImportTableEmbedded");
export interface DisconnectedImportTableEmbedded extends Entities.EmbeddedEntity {
  Type: "DisconnectedImportTableEmbedded";
  type: Entities.Lite<Basics.TypeEntity>;
  copyTable: number | null;
  disableForeignKeys: boolean | null;
  insertedRows: number | null;
  updatedRows: number | null;
  insertedOrUpdated: number | null;
}

export const DisconnectedMachineEntity: Type<DisconnectedMachineEntity> = new Type<DisconnectedMachineEntity>("DisconnectedMachine");
export interface DisconnectedMachineEntity extends Entities.Entity {
  Type: "DisconnectedMachine";
  creationDate: string /*DateTime*/;
  machineName: string;
  state: DisconnectedMachineState;
  seedMin: number;
  seedMax: number;
}

export module DisconnectedMachineOperation {
  export const Save : Operations.ExecuteSymbol<DisconnectedMachineEntity> = registerSymbol("Operation", "DisconnectedMachineOperation.Save");
  export const UnsafeUnlock : Operations.ExecuteSymbol<DisconnectedMachineEntity> = registerSymbol("Operation", "DisconnectedMachineOperation.UnsafeUnlock");
  export const FixImport : Operations.ConstructSymbol_From<DisconnectedImportEntity, DisconnectedMachineEntity> = registerSymbol("Operation", "DisconnectedMachineOperation.FixImport");
}

export const DisconnectedMachineState: EnumType<DisconnectedMachineState> = new EnumType<DisconnectedMachineState>("DisconnectedMachineState");
export type DisconnectedMachineState =
  "Connected" |
  "Disconnected" |
  "Faulted" |
  "Fixed";

export module DisconnectedMessage {
  export const NotAllowedToSave0WhileOffline: MessageKey = new MessageKey("DisconnectedMessage", "NotAllowedToSave0WhileOffline");
  export const The0WithId12IsLockedBy3: MessageKey = new MessageKey("DisconnectedMessage", "The0WithId12IsLockedBy3");
  export const Imports: MessageKey = new MessageKey("DisconnectedMessage", "Imports");
  export const Exports: MessageKey = new MessageKey("DisconnectedMessage", "Exports");
  export const _0OverlapsWith1: MessageKey = new MessageKey("DisconnectedMessage", "_0OverlapsWith1");
}

export const DisconnectedSubsetMixin: Type<DisconnectedSubsetMixin> = new Type<DisconnectedSubsetMixin>("DisconnectedSubsetMixin");
export interface DisconnectedSubsetMixin extends Entities.MixinEntity {
  Type: "DisconnectedSubsetMixin";
  lastOnlineTicks: number | null;
  disconnectedMachine: Entities.Lite<DisconnectedMachineEntity> | null;
}

export const Download: EnumType<Download> = new EnumType<Download>("Download");
export type Download =
  "None" |
  "All" |
  "Subset" |
  "Replace";

export const Upload: EnumType<Upload> = new EnumType<Upload>("Upload");
export type Upload =
  "None" |
  "New" |
  "Subset";

