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

export namespace PackageOperationProcess {
  export const PackageOperation : ProcessAlgorithmSymbol = registerSymbol("ProcessAlgorithm", "PackageOperationProcess.PackageOperation");
}

export namespace PackageQuery {
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

export namespace ProcessMessage {
  export const Process0IsNotRunningAnymore: MessageKey = new MessageKey("ProcessMessage", "Process0IsNotRunningAnymore");
  export const ProcessStartIsGreaterThanProcessEnd: MessageKey = new MessageKey("ProcessMessage", "ProcessStartIsGreaterThanProcessEnd");
  export const ProcessStartIsNullButProcessEndIsNot: MessageKey = new MessageKey("ProcessMessage", "ProcessStartIsNullButProcessEndIsNot");
  export const Lines: MessageKey = new MessageKey("ProcessMessage", "Lines");
  export const LastProcess: MessageKey = new MessageKey("ProcessMessage", "LastProcess");
  export const ExceptionLines: MessageKey = new MessageKey("ProcessMessage", "ExceptionLines");
  export const SuspendIsTheSaferWayOfStoppingARunningProcessCancelAnyway: MessageKey = new MessageKey("ProcessMessage", "SuspendIsTheSaferWayOfStoppingARunningProcessCancelAnyway");
  export const ProcessSettings: MessageKey = new MessageKey("ProcessMessage", "ProcessSettings");
  export const OnlyActive: MessageKey = new MessageKey("ProcessMessage", "OnlyActive");
  export const ProcessLogicStateLoading: MessageKey = new MessageKey("ProcessMessage", "ProcessLogicStateLoading");
  export const ProcessPanel: MessageKey = new MessageKey("ProcessMessage", "ProcessPanel");
  export const Start: MessageKey = new MessageKey("ProcessMessage", "Start");
  export const Stop: MessageKey = new MessageKey("ProcessMessage", "Stop");
  export const Running: MessageKey = new MessageKey("ProcessMessage", "Running");
  export const Stopped: MessageKey = new MessageKey("ProcessMessage", "Stopped");
  export const SimpleStatus: MessageKey = new MessageKey("ProcessMessage", "SimpleStatus");
  export const JustMyProcesses: MessageKey = new MessageKey("ProcessMessage", "JustMyProcesses");
  export const MachineName: MessageKey = new MessageKey("ProcessMessage", "MachineName");
  export const ApplicationName: MessageKey = new MessageKey("ProcessMessage", "ApplicationName");
  export const MaxDegreeOfParallelism: MessageKey = new MessageKey("ProcessMessage", "MaxDegreeOfParallelism");
  export const InitialDelayMilliseconds: MessageKey = new MessageKey("ProcessMessage", "InitialDelayMilliseconds");
  export const NextPlannedExecution: MessageKey = new MessageKey("ProcessMessage", "NextPlannedExecution");
  export const None: MessageKey = new MessageKey("ProcessMessage", "None");
  export const ExecutingProcesses: MessageKey = new MessageKey("ProcessMessage", "ExecutingProcesses");
  export const Process: MessageKey = new MessageKey("ProcessMessage", "Process");
  export const State: MessageKey = new MessageKey("ProcessMessage", "State");
  export const Progress: MessageKey = new MessageKey("ProcessMessage", "Progress");
  export const IsCancellationRequest: MessageKey = new MessageKey("ProcessMessage", "IsCancellationRequest");
  export const _0ProcessesExcecutingIn1_2: MessageKey = new MessageKey("ProcessMessage", "_0ProcessesExcecutingIn1_2");
  export const LatestProcesses: MessageKey = new MessageKey("ProcessMessage", "LatestProcesses");
}

export namespace ProcessOperation {
  export const Save : Operations.ExecuteSymbol<ProcessEntity> = registerSymbol("Operation", "ProcessOperation.Save");
  export const Execute : Operations.ExecuteSymbol<ProcessEntity> = registerSymbol("Operation", "ProcessOperation.Execute");
  export const Suspend : Operations.ExecuteSymbol<ProcessEntity> = registerSymbol("Operation", "ProcessOperation.Suspend");
  export const Cancel : Operations.ExecuteSymbol<ProcessEntity> = registerSymbol("Operation", "ProcessOperation.Cancel");
  export const Plan : Operations.ExecuteSymbol<ProcessEntity> = registerSymbol("Operation", "ProcessOperation.Plan");
  export const Retry : Operations.ConstructSymbol_From<ProcessEntity, ProcessEntity> = registerSymbol("Operation", "ProcessOperation.Retry");
}

export namespace ProcessPermission {
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

