//////////////////////////////////
//Auto-generated. Do NOT modify!//
//////////////////////////////////

import { MessageKey, QueryKey, Type, EnumType, registerSymbol } from '../../Signum/React/Reflection'
import * as Entities from '../../Signum/React/Signum.Entities'
import * as Operations from '../../Signum/React/Signum.Operations'
import * as Basics from '../../Signum/React/Signum.Basics'
import * as Files from '../Signum.Files/Signum.Files'
import * as Processes from '../Signum.Processes/Signum.Processes'
import * as Scheduler from '../Signum.Scheduler/Signum.Scheduler'


export const PrintLineEntity: Type<PrintLineEntity> = new Type<PrintLineEntity>("PrintLine");
export interface PrintLineEntity extends Entities.Entity {
  Type: "PrintLine";
  creationDate: string /*DateTime*/;
  testFileType: Files.FileTypeSymbol | null;
  file: Files.FilePathEmbedded;
  package: Entities.Lite<PrintPackageEntity> | null;
  printedOn: string /*DateTime*/ | null;
  referred: Entities.Lite<Entities.Entity> | null;
  state: PrintLineState;
}

export namespace PrintLineOperation {
  export const CreateTest : Operations.ConstructSymbol_Simple<PrintLineEntity> = registerSymbol("Operation", "PrintLineOperation.CreateTest");
  export const SaveTest : Operations.ExecuteSymbol<PrintLineEntity> = registerSymbol("Operation", "PrintLineOperation.SaveTest");
  export const Print : Operations.ExecuteSymbol<PrintLineEntity> = registerSymbol("Operation", "PrintLineOperation.Print");
  export const Retry : Operations.ExecuteSymbol<PrintLineEntity> = registerSymbol("Operation", "PrintLineOperation.Retry");
  export const Cancel : Operations.ExecuteSymbol<PrintLineEntity> = registerSymbol("Operation", "PrintLineOperation.Cancel");
}

export const PrintLineState: EnumType<PrintLineState> = new EnumType<PrintLineState>("PrintLineState");
export type PrintLineState =
  "NewTest" |
  "ReadyToPrint" |
  "Enqueued" |
  "Printed" |
  "Cancelled" |
  "Error" |
  "PrintedAndDeleted";

export const PrintPackageEntity: Type<PrintPackageEntity> = new Type<PrintPackageEntity>("PrintPackage");
export interface PrintPackageEntity extends Entities.Entity, Processes.IProcessDataEntity {
  Type: "PrintPackage";
  name: string | null;
}

export namespace PrintPackageProcess {
  export const PrintPackage : Processes.ProcessAlgorithmSymbol = registerSymbol("ProcessAlgorithm", "PrintPackageProcess.PrintPackage");
}

export namespace PrintPermission {
  export const ViewPrintPanel : Basics.PermissionSymbol = registerSymbol("Permission", "PrintPermission.ViewPrintPanel");
}

export namespace PrintTask {
  export const RemoveOldFiles : Scheduler.SimpleTaskSymbol = registerSymbol("SimpleTask", "PrintTask.RemoveOldFiles");
}

