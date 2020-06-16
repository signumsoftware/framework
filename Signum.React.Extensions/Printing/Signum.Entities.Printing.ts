//////////////////////////////////
//Auto-generated. Do NOT modify!//
//////////////////////////////////

import { MessageKey, QueryKey, Type, EnumType, registerSymbol } from '../../../Framework/Signum.React/Scripts/Reflection'
import * as Entities from '../../../Framework/Signum.React/Scripts/Signum.Entities'
import * as Processes from '../Processes/Signum.Entities.Processes'
import * as Files from '../Files/Signum.Entities.Files'
import * as Authorization from '../Authorization/Signum.Entities.Authorization'
import * as Scheduler from '../Scheduler/Signum.Entities.Scheduler'


export const PrintLineEntity = new Type<PrintLineEntity>("PrintLine");
export interface PrintLineEntity extends Entities.Entity, Processes.IProcessLineDataEntity {
  Type: "PrintLine";
  creationDate: string;
  testFileType: Files.FileTypeSymbol;
  file: Files.FilePathEmbedded;
  package: Entities.Lite<PrintPackageEntity> | null;
  printedOn: string | null;
  referred: Entities.Lite<Entities.Entity>;
  state: PrintLineState;
}

export module PrintLineOperation {
  export const CreateTest : Entities.ConstructSymbol_Simple<PrintLineEntity> = registerSymbol("Operation", "PrintLineOperation.CreateTest");
  export const SaveTest : Entities.ExecuteSymbol<PrintLineEntity> = registerSymbol("Operation", "PrintLineOperation.SaveTest");
  export const Print : Entities.ExecuteSymbol<PrintLineEntity> = registerSymbol("Operation", "PrintLineOperation.Print");
  export const Retry : Entities.ExecuteSymbol<PrintLineEntity> = registerSymbol("Operation", "PrintLineOperation.Retry");
  export const Cancel : Entities.ExecuteSymbol<PrintLineEntity> = registerSymbol("Operation", "PrintLineOperation.Cancel");
}

export const PrintLineState = new EnumType<PrintLineState>("PrintLineState");
export type PrintLineState =
  "NewTest" |
  "ReadyToPrint" |
  "Enqueued" |
  "Printed" |
  "Cancelled" |
  "Error" |
  "PrintedAndDeleted";

export const PrintPackageEntity = new Type<PrintPackageEntity>("PrintPackage");
export interface PrintPackageEntity extends Entities.Entity, Processes.IProcessDataEntity {
  Type: "PrintPackage";
  name: string | null;
}

export module PrintPackageProcess {
  export const PrintPackage : Processes.ProcessAlgorithmSymbol = registerSymbol("ProcessAlgorithm", "PrintPackageProcess.PrintPackage");
}

export module PrintPermission {
  export const ViewPrintPanel : Authorization.PermissionSymbol = registerSymbol("Permission", "PrintPermission.ViewPrintPanel");
}

export module PrintTask {
  export const RemoveOldFiles : Scheduler.SimpleTaskSymbol = registerSymbol("SimpleTask", "PrintTask.RemoveOldFiles");
}


