//////////////////////////////////
//Auto-generated. Do NOT modify!//
//////////////////////////////////
import { MessageKey, QueryKey, Type, EnumType, registerSymbol } from './Reflection' 

import * as Entities from './Signum.Entities' 
export const ColorEntity = new Type<ColorEntity>("ColorEntity");
export interface ColorEntity extends Entities.EmbeddedEntity {
    argb: number;
}

export const DeleteLogParametersEntity = new Type<DeleteLogParametersEntity>("DeleteLogParametersEntity");
export interface DeleteLogParametersEntity extends Entities.EmbeddedEntity {
    deleteLogsWithMoreThan: number;
    dateLimit: string;
    chunkSize: number;
    maxChunks: number;
}

export const ExceptionEntity = new Type<ExceptionEntity>("Exception");
export interface ExceptionEntity extends Entities.Entity {
    creationDate: string;
    exceptionType: string;
    exceptionMessage: string;
    exceptionMessageHash: number;
    stackTrace: string;
    stackTraceHash: number;
    threadId: number;
    user: Entities.Lite<IUserEntity>;
    environment: string;
    version: string;
    userAgent: string;
    requestUrl: string;
    controllerName: string;
    actionName: string;
    urlReferer: string;
    machineName: string;
    applicationName: string;
    userHostAddress: string;
    userHostName: string;
    form: string;
    queryString: string;
    session: string;
    data: string;
    referenced: boolean;
}

export interface IUserEntity extends Entities.Entity {
}

export const OperationLogEntity = new Type<OperationLogEntity>("OperationLog");
export interface OperationLogEntity extends Entities.Entity {
    target: Entities.Lite<Entities.Entity>;
    origin: Entities.Lite<Entities.Entity>;
    operation: Entities.OperationSymbol;
    user: Entities.Lite<IUserEntity>;
    start: string;
    end: string;
    exception: Entities.Lite<ExceptionEntity>;
}

export const PropertyRouteEntity = new Type<PropertyRouteEntity>("PropertyRoute");
export interface PropertyRouteEntity extends Entities.Entity {
    path: string;
    rootType: TypeEntity;
}

export const QueryEntity = new Type<QueryEntity>("Query");
export interface QueryEntity extends Entities.Entity {
    key: string;
}

export interface SemiSymbol extends Entities.Entity {
    key: string;
    name: string;
}

export const TypeEntity = new Type<TypeEntity>("Type");
export interface TypeEntity extends Entities.Entity {
    fullClassName: string;
    cleanName: string;
    tableName: string;
    namespace: string;
    className: string;
}

export namespace External {

    export module CollectionMessage {
        export const And = new MessageKey("CollectionMessage", "And");
        export const Or = new MessageKey("CollectionMessage", "Or");
        export const No0Found = new MessageKey("CollectionMessage", "No0Found");
        export const MoreThanOne0Found = new MessageKey("CollectionMessage", "MoreThanOne0Found");
    }
    
    export const DayOfWeek = new EnumType<DayOfWeek>("DayOfWeek");
    export type DayOfWeek =
        "Sunday" |
        "Monday" |
        "Tuesday" |
        "Wednesday" |
        "Thursday" |
        "Friday" |
        "Saturday";
    
}

