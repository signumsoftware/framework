//////////////////////////////////
//Auto-generated. Do NOT modify!//
//////////////////////////////////
import { MessageKey, QueryKey, Type, EnumType, registerSymbol } from '../../../Framework/Signum.React/Scripts/Reflection' 

import * as Entities from '../../../Framework/Signum.React/Scripts/Signum.Entities' 

import * as Authorization from '../Authorization/Signum.Entities.Authorization' 

import * as Basics from '../../../Framework/Signum.React/Scripts/Signum.Entities.Basics' 


export interface IProcessDataEntity extends Entities.Entity {
}

export interface IProcessLineDataEntity extends Entities.Entity {
}

export const PackageEntity_Type = new Type<PackageEntity>("Package");
export interface PackageEntity extends Entities.Entity, IProcessDataEntity {
    name: string;
    operationArguments: string;
}

export const PackageLineEntity_Type = new Type<PackageLineEntity>("PackageLine");
export interface PackageLineEntity extends Entities.Entity, IProcessLineDataEntity {
    package: Entities.Lite<PackageEntity>;
    target: Entities.Entity;
    result: Entities.Lite<Entities.Entity>;
    finishTime: string;
}

export const PackageOperationEntity_Type = new Type<PackageOperationEntity>("PackageOperation");
export interface PackageOperationEntity extends PackageEntity {
    operation: Entities.OperationSymbol;
}

export module PackageOperationProcess {
    export const PackageOperation : ProcessAlgorithmSymbol = registerSymbol({ Type: "ProcessAlgorithm", key: "PackageOperationProcess.PackageOperation" });
}

export const ProcessAlgorithmSymbol_Type = new Type<ProcessAlgorithmSymbol>("ProcessAlgorithm");
export interface ProcessAlgorithmSymbol extends Entities.Symbol {
}

export const ProcessEntity_Type = new Type<ProcessEntity>("Process");
export interface ProcessEntity extends Entities.Entity {
    algorithm: ProcessAlgorithmSymbol;
    data: IProcessDataEntity;
    machineName: string;
    applicationName: string;
    user: Entities.Lite<Basics.IUserEntity>;
    state: ProcessState;
    creationDate: string;
    plannedDate: string;
    cancelationDate: string;
    queuedDate: string;
    executionStart: string;
    executionEnd: string;
    suspendDate: string;
    exceptionDate: string;
    exception: Entities.Lite<Basics.ExceptionEntity>;
    progress: number;
}

export const ProcessExceptionLineEntity_Type = new Type<ProcessExceptionLineEntity>("ProcessExceptionLine");
export interface ProcessExceptionLineEntity extends Entities.Entity {
    line: Entities.Lite<IProcessLineDataEntity>;
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
}

export module ProcessOperation {
    export const Plan : Entities.ExecuteSymbol<ProcessEntity> = registerSymbol({ Type: "Operation", key: "ProcessOperation.Plan" });
    export const Save : Entities.ExecuteSymbol<ProcessEntity> = registerSymbol({ Type: "Operation", key: "ProcessOperation.Save" });
    export const Cancel : Entities.ExecuteSymbol<ProcessEntity> = registerSymbol({ Type: "Operation", key: "ProcessOperation.Cancel" });
    export const Execute : Entities.ExecuteSymbol<ProcessEntity> = registerSymbol({ Type: "Operation", key: "ProcessOperation.Execute" });
    export const Suspend : Entities.ExecuteSymbol<ProcessEntity> = registerSymbol({ Type: "Operation", key: "ProcessOperation.Suspend" });
    export const Retry : Entities.ConstructSymbol_From<ProcessEntity, ProcessEntity> = registerSymbol({ Type: "Operation", key: "ProcessOperation.Retry" });
}

export module ProcessPermission {
    export const ViewProcessPanel : Authorization.PermissionSymbol = registerSymbol({ Type: "Permission", key: "ProcessPermission.ViewProcessPanel" });
}

export enum ProcessState {
    Created = "Created" as any,
    Planned = "Planned" as any,
    Canceled = "Canceled" as any,
    Queued = "Queued" as any,
    Executing = "Executing" as any,
    Suspending = "Suspending" as any,
    Suspended = "Suspended" as any,
    Finished = "Finished" as any,
    Error = "Error" as any,
}
export const ProcessState_Type = new EnumType<ProcessState>("ProcessState", ProcessState);

