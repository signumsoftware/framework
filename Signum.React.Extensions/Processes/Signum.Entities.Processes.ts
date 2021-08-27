//////////////////////////////////
//Auto-generated. Do NOT modify!//
//////////////////////////////////

import { MessageKey, QueryKey, Type, EnumType, registerSymbol } from '../../../Framework/Signum.React/Scripts/Reflection'
import * as Entities from '../../../Framework/Signum.React/Scripts/Signum.Entities'
import * as Basics from '../../../Framework/Signum.React/Scripts/Signum.Entities.Basics'
import * as Authorization from '../Authorization/Signum.Entities.Authorization'


export interface IProcessDataEntity extends Entities.Entity {
}

export interface IProcessLineDataEntity extends Entities.Entity {
}

export const PackageEntity = new Type<PackageEntity>("Package");
export interface PackageEntity extends Entities.Entity, IProcessDataEntity {
  Type: "Package";
  name: string | null;
  operationArguments: string | null;
}

export const PackageLineEntity = new Type<PackageLineEntity>("PackageLine");
export interface PackageLineEntity extends Entities.Entity, IProcessLineDataEntity {
  Type: "PackageLine";
  package: Entities.Lite<PackageEntity>;
  target: Entities.Entity;
  result: Entities.Lite<Entities.Entity> | null;
  finishTime: string | null;
}

export const PackageOperationEntity = new Type<PackageOperationEntity>("PackageOperation");
export interface PackageOperationEntity extends PackageEntity {
  operation: Entities.OperationSymbol;
}

export module PackageOperationProcess {
  export const PackageOperation : ProcessAlgorithmSymbol = registerSymbol("ProcessAlgorithm", "PackageOperationProcess.PackageOperation");
}

export module PackageQuery {
  export const PackageLineLastProcess = new QueryKey("PackageQuery", "PackageLineLastProcess");
  export const PackageLastProcess = new QueryKey("PackageQuery", "PackageLastProcess");
  export const PackageOperationLastProcess = new QueryKey("PackageQuery", "PackageOperationLastProcess");
}

export const ProcessAlgorithmSymbol = new Type<ProcessAlgorithmSymbol>("ProcessAlgorithm");
export interface ProcessAlgorithmSymbol extends Entities.Symbol {
  Type: "ProcessAlgorithm";
}

export const ProcessEntity = new Type<ProcessEntity>("Process");
export interface ProcessEntity extends Entities.Entity {
  Type: "Process";
  algorithm: ProcessAlgorithmSymbol;
  data: IProcessDataEntity | null;
  machineName: string;
  applicationName: string;
  user: Entities.Lite<Basics.IUserEntity>;
  state: ProcessState;
  creationDate: string;
  plannedDate: string | null;
  cancelationDate: string | null;
  queuedDate: string | null;
  executionStart: string | null;
  executionEnd: string | null;
  suspendDate: string | null;
  exceptionDate: string | null;
  exception: Entities.Lite<Basics.ExceptionEntity> | null;
  progress: number | null;
  status: string | null;
}

export const ProcessExceptionLineEntity = new Type<ProcessExceptionLineEntity>("ProcessExceptionLine");
export interface ProcessExceptionLineEntity extends Entities.Entity {
  Type: "ProcessExceptionLine";
  elementInfo: string | null;
  line: Entities.Lite<IProcessLineDataEntity> | null;
  process: Entities.Lite<ProcessEntity>;
  exception: Entities.Lite<Basics.ExceptionEntity>;
}

export module ProcessMessage {
  export const Process0IsNotRunningAnymore = new MessageKey("ProcessMessage", "Process0IsNotRunningAnymore");
  export const ProcessStartIsGreaterThanProcessEnd = new MessageKey("ProcessMessage", "ProcessStartIsGreaterThanProcessEnd");
  export const ProcessStartIsNullButProcessEndIsNot = new MessageKey("ProcessMessage", "ProcessStartIsNullButProcessEndIsNot");
  export const Lines = new MessageKey("ProcessMessage", "Lines");
  export const LastProcess = new MessageKey("ProcessMessage", "LastProcess");
  export const ExceptionLines = new MessageKey("ProcessMessage", "ExceptionLines");
  export const SuspendIsTheSaferWayOfStoppingARunningProcessCancelAnyway = new MessageKey("ProcessMessage", "SuspendIsTheSaferWayOfStoppingARunningProcessCancelAnyway");
}

export module ProcessOperation {
  export const Plan : Entities.ExecuteSymbol<ProcessEntity> = registerSymbol("Operation", "ProcessOperation.Plan");
  export const Save : Entities.ExecuteSymbol<ProcessEntity> = registerSymbol("Operation", "ProcessOperation.Save");
  export const Cancel : Entities.ExecuteSymbol<ProcessEntity> = registerSymbol("Operation", "ProcessOperation.Cancel");
  export const Execute : Entities.ExecuteSymbol<ProcessEntity> = registerSymbol("Operation", "ProcessOperation.Execute");
  export const Suspend : Entities.ExecuteSymbol<ProcessEntity> = registerSymbol("Operation", "ProcessOperation.Suspend");
  export const Retry : Entities.ConstructSymbol_From<ProcessEntity, ProcessEntity> = registerSymbol("Operation", "ProcessOperation.Retry");
}

export module ProcessPermission {
  export const ViewProcessPanel : Authorization.PermissionSymbol = registerSymbol("Permission", "ProcessPermission.ViewProcessPanel");
}

export const ProcessState = new EnumType<ProcessState>("ProcessState");
export type ProcessState =
  "Created" |
  "Planned" |
  "Canceled" |
  "Queued" |
  "Executing" |
  "Suspending" |
  "Suspended" |
  "Finished" |
  "Error";


