//////////////////////////////////
//Auto-generated. Do NOT modify!//
//////////////////////////////////

import { MessageKey, QueryKey, Type, EnumType, registerSymbol } from './Reflection'
import * as Entities from './Signum.Entities'


export const ColorEntity = new Type<ColorEntity>("ColorEntity");
export interface ColorEntity extends Entities.EmbeddedEntity {
    Type: "ColorEntity";
    argb?: number;
}

export const DeleteLogParametersEntity = new Type<DeleteLogParametersEntity>("DeleteLogParametersEntity");
export interface DeleteLogParametersEntity extends Entities.EmbeddedEntity {
    Type: "DeleteLogParametersEntity";
    deleteLogsWithMoreThan?: number;
    dateLimit?: string;
    chunkSize?: number;
    maxChunks?: number;
}

export const ExceptionEntity = new Type<ExceptionEntity>("Exception");
export interface ExceptionEntity extends Entities.Entity {
    Type: "Exception";
    creationDate?: string;
    exceptionType?: string | null;
    exceptionMessage?: string | null;
    exceptionMessageHash?: number;
    stackTrace?: string | null;
    stackTraceHash?: number;
    threadId?: number;
    user?: Entities.Lite<IUserEntity> | null;
    environment?: string | null;
    version?: string | null;
    userAgent?: string | null;
    requestUrl?: string | null;
    controllerName?: string | null;
    actionName?: string | null;
    urlReferer?: string | null;
    machineName?: string | null;
    applicationName?: string | null;
    userHostAddress?: string | null;
    userHostName?: string | null;
    form?: string | null;
    queryString?: string | null;
    session?: string | null;
    data?: string | null;
    referenced?: boolean;
}

export interface IUserEntity extends Entities.Entity {
}

export const OperationLogEntity = new Type<OperationLogEntity>("OperationLog");
export interface OperationLogEntity extends Entities.Entity {
    Type: "OperationLog";
    target: Entities.Lite<Entities.Entity> | null;
    origin: Entities.Lite<Entities.Entity> | null;
    operation: Entities.OperationSymbol;
    user: Entities.Lite<IUserEntity>;
    start: string;
    end: string | null;
    exception: Entities.Lite<ExceptionEntity> | null;
}

export const PropertyRouteEntity = new Type<PropertyRouteEntity>("PropertyRoute");
export interface PropertyRouteEntity extends Entities.Entity {
    Type: "PropertyRoute";
    path?: string | null;
    rootType?: TypeEntity | null;
}

export const QueryEntity = new Type<QueryEntity>("Query");
export interface QueryEntity extends Entities.Entity {
    Type: "Query";
    key: string;
}

export interface SemiSymbol extends Entities.Entity {
    key?: string | null;
    name?: string | null;
}

export const TypeEntity = new Type<TypeEntity>("Type");
export interface TypeEntity extends Entities.Entity {
    Type: "Type";
    fullClassName: string;
    cleanName: string;
    tableName: string;
    namespace: string | null;
    className: string | null;
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


