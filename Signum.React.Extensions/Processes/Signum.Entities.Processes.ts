//////////////////////////////////
//Auto-generated. Do NOT modify!//
//////////////////////////////////
import { MessageKey, QueryKey, Type, EnumType, registerSymbol } from 'Framework/Signum.React/Scripts/Reflection' 

import * as Entities from 'Framework/Signum.React/Scripts/Signum.Entities' 

import * as Authorization from 'Extensions/Signum.React.Extensions/Authorization/Signum.Entities.Authorization' 
export interface IProcessDataEntity extends Entities.IEntity {
}

export interface IProcessLineDataEntity extends Entities.IEntity {
}

export const PackageEntity_Type = new Type<PackageEntity>("PackageEntity");
export interface PackageEntity extends Entities.Entity, IProcessDataEntity {
    name?: string;
    operationArguments?: string;
}

export const PackageLineEntity_Type = new Type<PackageLineEntity>("PackageLineEntity");
export interface PackageLineEntity extends Entities.Entity, IProcessLineDataEntity {
    package?: Entities.Lite<PackageEntity>;
    target?: Entities.Entity;
    result?: Entities.Lite<Entities.Entity>;
    finishTime?: string;
}

export const PackageOperationEntity_Type = new Type<PackageOperationEntity>("PackageOperationEntity");
export interface PackageOperationEntity extends PackageEntity {
    operation?: Entities.OperationSymbol;
}

export module PackageOperationProcess {
    export const PackageOperation : ProcessAlgorithmSymbol = registerSymbol({ key: "PackageOperationProcess.PackageOperation" });
}

export const ProcessAlgorithmSymbol_Type = new Type<ProcessAlgorithmSymbol>("ProcessAlgorithmSymbol");
export interface ProcessAlgorithmSymbol extends Entities.Symbol {
}

export const ProcessEntity_Type = new Type<ProcessEntity>("ProcessEntity");
export interface ProcessEntity extends Entities.Entity {
    algorithm?: ProcessAlgorithmSymbol;
    data?: IProcessDataEntity;
    machineName?: string;
    applicationName?: string;
    user?: Entities.Lite<Entities.Basics.IUserEntity>;
    state?: ProcessState;
    creationDate?: string;
    plannedDate?: string;
    cancelationDate?: string;
    queuedDate?: string;
    executionStart?: string;
    executionEnd?: string;
    suspendDate?: string;
    exceptionDate?: string;
    exception?: Entities.Lite<Entities.Basics.ExceptionEntity>;
    progress?: number;
}

export const ProcessExceptionLineEntity_Type = new Type<ProcessExceptionLineEntity>("ProcessExceptionLineEntity");
export interface ProcessExceptionLineEntity extends Entities.Entity {
    line?: Entities.Lite<IProcessLineDataEntity>;
    process?: Entities.Lite<ProcessEntity>;
    exception?: Entities.Lite<Entities.Basics.ExceptionEntity>;
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
    export const Plan : Entities.ExecuteSymbol<ProcessEntity> = registerSymbol({ key: "ProcessOperation.Plan" });
    export const Save : Entities.ExecuteSymbol<ProcessEntity> = registerSymbol({ key: "ProcessOperation.Save" });
    export const Cancel : Entities.ExecuteSymbol<ProcessEntity> = registerSymbol({ key: "ProcessOperation.Cancel" });
    export const Execute : Entities.ExecuteSymbol<ProcessEntity> = registerSymbol({ key: "ProcessOperation.Execute" });
    export const Suspend : Entities.ExecuteSymbol<ProcessEntity> = registerSymbol({ key: "ProcessOperation.Suspend" });
    export const Retry : Entities.ConstructSymbol_From<ProcessEntity, ProcessEntity> = registerSymbol({ key: "ProcessOperation.Retry" });
}

export module ProcessPermission {
    export const ViewProcessPanel : Authorization.PermissionSymbol = registerSymbol({ key: "ProcessPermission.ViewProcessPanel" });
}

export enum ProcessState {
    Created,
    Planned,
    Canceled,
    Queued,
    Executing,
    Suspending,
    Suspended,
    Finished,
    Error,
}
export const ProcessState_Type = new EnumType<ProcessState>("ProcessState", ProcessState);

