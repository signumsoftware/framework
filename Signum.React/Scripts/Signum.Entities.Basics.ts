//////////////////////////////////
//Auto-generated. Do NOT modify!//
//////////////////////////////////

import { MessageKey, QueryKey, Type, EnumType, registerSymbol } from './Reflection'
import * as Entities from './Signum.Entities'


export const BigStringEmbedded = new Type<BigStringEmbedded>("BigStringEmbedded");
export interface BigStringEmbedded extends Entities.EmbeddedEntity {
  Type: "BigStringEmbedded";
  text: string | null;
}

export const DeleteLogParametersEmbedded = new Type<DeleteLogParametersEmbedded>("DeleteLogParametersEmbedded");
export interface DeleteLogParametersEmbedded extends Entities.EmbeddedEntity {
  Type: "DeleteLogParametersEmbedded";
  deleteLogs: Entities.MList<DeleteLogsTypeOverridesEmbedded>;
  chunkSize: number;
  maxChunks: number;
  pauseTime: number | null;
}

export const DeleteLogsTypeOverridesEmbedded = new Type<DeleteLogsTypeOverridesEmbedded>("DeleteLogsTypeOverridesEmbedded");
export interface DeleteLogsTypeOverridesEmbedded extends Entities.EmbeddedEntity {
  Type: "DeleteLogsTypeOverridesEmbedded";
  type: Entities.Lite<TypeEntity>;
  deleteLogsOlderThan: number | null;
  deleteLogsWithExceptionsOlderThan: number | null;
}

export const ExceptionEntity = new Type<ExceptionEntity>("Exception");
export interface ExceptionEntity extends Entities.Entity {
  Type: "Exception";
  creationDate: string;
  exceptionType: string | null;
  exceptionMessage: string;
  exceptionMessageHash: number;
  stackTrace: BigStringEmbedded;
  stackTraceHash: number;
  threadId: number;
  user: Entities.Lite<IUserEntity> | null;
  environment: string | null;
  version: string | null;
  userAgent: string | null;
  requestUrl: string | null;
  controllerName: string | null;
  actionName: string | null;
  urlReferer: string | null;
  machineName: string | null;
  applicationName: string | null;
  userHostAddress: string | null;
  userHostName: string | null;
  form: BigStringEmbedded;
  queryString: BigStringEmbedded;
  session: BigStringEmbedded;
  data: BigStringEmbedded;
  hResult: number;
  referenced: boolean;
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
  path: string;
  rootType: TypeEntity;
}

export const QueryEntity = new Type<QueryEntity>("Query");
export interface QueryEntity extends Entities.Entity {
  Type: "Query";
  key: string;
}

export interface SemiSymbol extends Entities.Entity {
  key: string | null;
  name: string;
}

export const TypeEntity = new Type<TypeEntity>("Type");
export interface TypeEntity extends Entities.Entity {
  Type: "Type";
  tableName: string;
  cleanName: string;
  namespace: string;
  className: string;
}


