//////////////////////////////////
//Auto-generated. Do NOT modify!//
//////////////////////////////////

import { MessageKey, QueryKey, Type, EnumType, registerSymbol } from '../../../Framework/Signum.React/Scripts/Reflection'
import * as Entities from '../../../Framework/Signum.React/Scripts/Signum.Entities'
import * as Basics from '../../../Framework/Signum.React/Scripts/Signum.Entities.Basics'


export const DisconnectedCreatedMixin = new Type<DisconnectedCreatedMixin>("DisconnectedCreatedMixin");
export interface DisconnectedCreatedMixin extends Entities.MixinEntity {
  Type: "DisconnectedCreatedMixin";
  disconnectedCreated: boolean;
}

export const DisconnectedExportEntity = new Type<DisconnectedExportEntity>("DisconnectedExport");
export interface DisconnectedExportEntity extends Entities.Entity {
  Type: "DisconnectedExport";
  creationDate: string;
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

export const DisconnectedExportState = new EnumType<DisconnectedExportState>("DisconnectedExportState");
export type DisconnectedExportState =
  "InProgress" |
  "Completed" |
  "Error";

export const DisconnectedExportTableEmbedded = new Type<DisconnectedExportTableEmbedded>("DisconnectedExportTableEmbedded");
export interface DisconnectedExportTableEmbedded extends Entities.EmbeddedEntity {
  Type: "DisconnectedExportTableEmbedded";
  type: Entities.Lite<Basics.TypeEntity>;
  copyTable: number | null;
  errors: string;
}

export const DisconnectedImportEntity = new Type<DisconnectedImportEntity>("DisconnectedImport");
export interface DisconnectedImportEntity extends Entities.Entity {
  Type: "DisconnectedImport";
  creationDate: string;
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

export const DisconnectedImportState = new EnumType<DisconnectedImportState>("DisconnectedImportState");
export type DisconnectedImportState =
  "InProgress" |
  "Completed" |
  "Error";

export const DisconnectedImportTableEmbedded = new Type<DisconnectedImportTableEmbedded>("DisconnectedImportTableEmbedded");
export interface DisconnectedImportTableEmbedded extends Entities.EmbeddedEntity {
  Type: "DisconnectedImportTableEmbedded";
  type: Entities.Lite<Basics.TypeEntity>;
  copyTable: number | null;
  disableForeignKeys: boolean | null;
  insertedRows: number | null;
  updatedRows: number | null;
  insertedOrUpdated: number | null;
}

export const DisconnectedMachineEntity = new Type<DisconnectedMachineEntity>("DisconnectedMachine");
export interface DisconnectedMachineEntity extends Entities.Entity {
  Type: "DisconnectedMachine";
  creationDate: string;
  machineName: string;
  state: DisconnectedMachineState;
  seedMin: number;
  seedMax: number;
}

export module DisconnectedMachineOperation {
  export const Save : Entities.ExecuteSymbol<DisconnectedMachineEntity> = registerSymbol("Operation", "DisconnectedMachineOperation.Save");
  export const UnsafeUnlock : Entities.ExecuteSymbol<DisconnectedMachineEntity> = registerSymbol("Operation", "DisconnectedMachineOperation.UnsafeUnlock");
  export const FixImport : Entities.ConstructSymbol_From<DisconnectedImportEntity, DisconnectedMachineEntity> = registerSymbol("Operation", "DisconnectedMachineOperation.FixImport");
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
  Type: "DisconnectedSubsetMixin";
  lastOnlineTicks: number | null;
  disconnectedMachine: Entities.Lite<DisconnectedMachineEntity> | null;
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


