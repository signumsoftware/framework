//////////////////////////////////
//Auto-generated. Do NOT modify!//
//////////////////////////////////

import { MessageKey, QueryKey, Type, EnumType, registerSymbol } from '../../Signum/React/Reflection'
import * as Entities from '../../Signum/React/Signum.Entities'
import * as Operations from '../../Signum/React/Signum.Operations'
import * as Basics from '../../Signum/React/Signum.Basics'
import * as Security from '../../Signum/React/Signum.Security'


export interface IProcessDataEntity extends Entities.Entity {
}

export const PackageEntity: Type<PackageEntity> = new Type<PackageEntity>("Package");
export interface PackageEntity extends Entities.Entity, IProcessDataEntity {
  Type: "Package";
  name: string | null;
  operationArguments: string /*Byte[]*/ | null;
  configString: string | null;
}

export const PackageLineEntity: Type<PackageLineEntity> = new Type<PackageLineEntity>("PackageLine");
export interface PackageLineEntity extends Entities.Entity {
  Type: "PackageLine";
  package: Entities.Lite<PackageEntity>;
  target: Entities.Entity;
  result: Entities.Lite<Entities.Entity> | null;
  finishTime: string /*DateTime*/ | null;
}

export const PackageOperationEntity: Type<PackageOperationEntity> = new Type<PackageOperationEntity>("PackageOperation");
export interface PackageOperationEntity extends PackageEntity {
  operation: Operations.OperationSymbol;
}

export module PackageOperationProcess {
  export const PackageOperation : ProcessAlgorithmSymbol = registerSymbol("ProcessAlgorithm", "PackageOperationProcess.PackageOperation");
}

export module PackageQuery {
  export const PackageLineLastProcess: QueryKey = new QueryKey("PackageQuery", "PackageLineLastProcess");
  export const PackageLastProcess: QueryKey = new QueryKey("PackageQuery", "PackageLastProcess");
  export const PackageOperationLastProcess: QueryKey = new QueryKey("PackageQuery", "PackageOperationLastProcess");
}

export const ProcessAlgorithmSymbol: Type<ProcessAlgorithmSymbol> = new Type<ProcessAlgorithmSymbol>("ProcessAlgorithm");
export interface ProcessAlgorithmSymbol extends Basics.Symbol {
  Type: "ProcessAlgorithm";
}

export const ProcessEntity: Type<ProcessEntity> = new Type<ProcessEntity>("Process");
export interface ProcessEntity extends Entities.Entity {
  Type: "Process";
  algorithm: ProcessAlgorithmSymbol;
  data: IProcessDataEntity | null;
  machineName: string;
  applicationName: string;
  user: Entities.Lite<Security.IUserEntity>;
  state: ProcessState;
  creationDate: string /*DateTime*/;
  plannedDate: string /*DateTime*/ | null;
  cancelationDate: string /*DateTime*/ | null;
  queuedDate: string /*DateTime*/ | null;
  executionStart: string /*DateTime*/ | null;
  executionEnd: string /*DateTime*/ | null;
  suspendDate: string /*DateTime*/ | null;
  exceptionDate: string /*DateTime*/ | null;
  exception: Entities.Lite<Basics.ExceptionEntity> | null;
  progress: number | null;
  status: string | null;
}

export const ProcessExceptionLineEntity: Type<ProcessExceptionLineEntity> = new Type<ProcessExceptionLineEntity>("ProcessExceptionLine");
export interface ProcessExceptionLineEntity extends Entities.Entity {
  Type: "ProcessExceptionLine";
  elementInfo: string | null;
  line: Entities.Lite<Entities.Entity> | null;
  process: Entities.Lite<ProcessEntity>;
  exception: Entities.Lite<Basics.ExceptionEntity>;
}

export module ProcessMessage {
  export const Process0IsNotRunningAnymore: MessageKey = new MessageKey("ProcessMessage", "Process0IsNotRunningAnymore");
  export const ProcessStartIsGreaterThanProcessEnd: MessageKey = new MessageKey("ProcessMessage", "ProcessStartIsGreaterThanProcessEnd");
  export const ProcessStartIsNullButProcessEndIsNot: MessageKey = new MessageKey("ProcessMessage", "ProcessStartIsNullButProcessEndIsNot");
  export const Lines: MessageKey = new MessageKey("ProcessMessage", "Lines");
  export const LastProcess: MessageKey = new MessageKey("ProcessMessage", "LastProcess");
  export const ExceptionLines: MessageKey = new MessageKey("ProcessMessage", "ExceptionLines");
  export const SuspendIsTheSaferWayOfStoppingARunningProcessCancelAnyway: MessageKey = new MessageKey("ProcessMessage", "SuspendIsTheSaferWayOfStoppingARunningProcessCancelAnyway");
  export const ProcessSettings: MessageKey = new MessageKey("ProcessMessage", "ProcessSettings");
  export const OnlyActive: MessageKey = new MessageKey("ProcessMessage", "OnlyActive");
}

export module ProcessOperation {
  export const Save : Operations.ExecuteSymbol<ProcessEntity> = registerSymbol("Operation", "ProcessOperation.Save");
  export const Execute : Operations.ExecuteSymbol<ProcessEntity> = registerSymbol("Operation", "ProcessOperation.Execute");
  export const Suspend : Operations.ExecuteSymbol<ProcessEntity> = registerSymbol("Operation", "ProcessOperation.Suspend");
  export const Cancel : Operations.ExecuteSymbol<ProcessEntity> = registerSymbol("Operation", "ProcessOperation.Cancel");
  export const Plan : Operations.ExecuteSymbol<ProcessEntity> = registerSymbol("Operation", "ProcessOperation.Plan");
  export const Retry : Operations.ConstructSymbol_From<ProcessEntity, ProcessEntity> = registerSymbol("Operation", "ProcessOperation.Retry");
}

export module ProcessPermission {
  export const ViewProcessPanel : Basics.PermissionSymbol = registerSymbol("Permission", "ProcessPermission.ViewProcessPanel");
}

export const ProcessState: EnumType<ProcessState> = new EnumType<ProcessState>("ProcessState");
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

