//////////////////////////////////
//Auto-generated. Do NOT modify!//
//////////////////////////////////
import { MessageKey, QueryKey, Type, EnumType, registerSymbol } from './Reflection' 

import * as Entities from './Signum.Entities' 
export const ColorEntity_Type = new Type<ColorEntity>("ColorEntity");
export interface ColorEntity extends Entities.EmbeddedEntity {
    argb?: number;
}

export const DeleteLogParametersEntity_Type = new Type<DeleteLogParametersEntity>("DeleteLogParametersEntity");
export interface DeleteLogParametersEntity extends Entities.EmbeddedEntity {
    deleteLogsWithMoreThan?: number;
    dateLimit?: string;
    chunkSize?: number;
    maxChunks?: number;
}

export const ExceptionEntity_Type = new Type<ExceptionEntity>("Exception");
export interface ExceptionEntity extends Entities.Entity {
    creationDate?: string;
    exceptionType?: string;
    exceptionMessage?: string;
    exceptionMessageHash?: number;
    stackTrace?: string;
    stackTraceHash?: number;
    threadId?: number;
    user?: Entities.Lite<IUserEntity>;
    environment?: string;
    version?: string;
    userAgent?: string;
    requestUrl?: string;
    controllerName?: string;
    actionName?: string;
    urlReferer?: string;
    machineName?: string;
    applicationName?: string;
    userHostAddress?: string;
    userHostName?: string;
    form?: string;
    queryString?: string;
    session?: string;
    data?: string;
    referenced?: boolean;
}

export interface IUserEntity extends Entities.IEntity {
}

export const OperationLogEntity_Type = new Type<OperationLogEntity>("OperationLog");
export interface OperationLogEntity extends Entities.Entity {
    target?: Entities.Lite<Entities.IEntity>;
    origin?: Entities.Lite<Entities.IEntity>;
    operation?: Entities.OperationSymbol;
    user?: Entities.Lite<IUserEntity>;
    start?: string;
    end?: string;
    exception?: Entities.Lite<ExceptionEntity>;
}

export const PropertyRouteEntity_Type = new Type<PropertyRouteEntity>("PropertyRoute");
export interface PropertyRouteEntity extends Entities.Entity {
    path?: string;
    rootType?: TypeEntity;
}

export const QueryEntity_Type = new Type<QueryEntity>("Query");
export interface QueryEntity extends Entities.Entity {
    key?: string;
}

export interface SemiSymbol extends Entities.Entity {
    key?: string;
    name?: string;
}

export const TypeEntity_Type = new Type<TypeEntity>("Type");
export interface TypeEntity extends Entities.Entity {
    fullClassName?: string;
    cleanName?: string;
    tableName?: string;
    namespace?: string;
    className?: string;
}

export namespace External {

    export module CollectionMessage {
        export const And = new MessageKey("CollectionMessage", "And");
        export const Or = new MessageKey("CollectionMessage", "Or");
        export const No0Found = new MessageKey("CollectionMessage", "No0Found");
        export const MoreThanOne0Found = new MessageKey("CollectionMessage", "MoreThanOne0Found");
    }
    
    export enum DayOfWeek {
        Sunday = "Sunday" as any,
        Monday = "Monday" as any,
        Tuesday = "Tuesday" as any,
        Wednesday = "Wednesday" as any,
        Thursday = "Thursday" as any,
        Friday = "Friday" as any,
        Saturday = "Saturday" as any,
    }
    export const DayOfWeek_Type = new EnumType<DayOfWeek>("DayOfWeek", DayOfWeek);
    
}

