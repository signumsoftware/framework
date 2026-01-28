//////////////////////////////////
//Auto-generated. Do NOT modify!//
//////////////////////////////////

import { MessageKey, QueryKey, Type, EnumType, registerSymbol } from './Reflection'
import * as Entities from './Signum.Entities'
import * as Security from './Signum.Security'
import * as Operations from './Signum.Operations'


export const BootstrapStyle: EnumType<BootstrapStyle> = new EnumType<BootstrapStyle>("BootstrapStyle");
export type BootstrapStyle =
  "Light" |
  "Dark" |
  "Primary" |
  "Secondary" |
  "Success" |
  "Info" |
  "Warning" |
  "Danger";

export namespace ChangeLogMessage {
  export const ThereIsNotAnyNewChangesFrom0: MessageKey = new MessageKey("ChangeLogMessage", "ThereIsNotAnyNewChangesFrom0");
  export const SeeMore: MessageKey = new MessageKey("ChangeLogMessage", "SeeMore");
  export const SeeMoreChangeLogEntries: MessageKey = new MessageKey("ChangeLogMessage", "SeeMoreChangeLogEntries");
  export const ChangeLogs: MessageKey = new MessageKey("ChangeLogMessage", "ChangeLogs");
  export const DeployedOn0: MessageKey = new MessageKey("ChangeLogMessage", "DeployedOn0");
  export const _0ImplementedOn1WithFollowingChanges2: MessageKey = new MessageKey("ChangeLogMessage", "_0ImplementedOn1WithFollowingChanges2");
  export const ChangeLogEntries: MessageKey = new MessageKey("ChangeLogMessage", "ChangeLogEntries");
}

export const ChangeLogViewLogEntity: Type<ChangeLogViewLogEntity> = new Type<ChangeLogViewLogEntity>("ChangeLogViewLog");
export interface ChangeLogViewLogEntity extends Entities.Entity {
  Type: "ChangeLogViewLog";
  user: Entities.Lite<Security.IUserEntity>;
  lastDate: string /*DateTime*/;
}

export namespace ChangeLogViewLogOperation {
  export const Delete : Operations.DeleteSymbol<ChangeLogViewLogEntity> = registerSymbol("Operation", "ChangeLogViewLogOperation.Delete");
}

export const ClientErrorModel: Type<ClientErrorModel> = new Type<ClientErrorModel>("ClientErrorModel");
export interface ClientErrorModel extends Entities.ModelEntity {
  Type: "ClientErrorModel";
  url: string | null;
  errorType: string;
  message: string;
  stack: string | null;
  name: string | null;
}

export namespace CollapsableCardMessage {
  export const Collapse: MessageKey = new MessageKey("CollapsableCardMessage", "Collapse");
  export const Expand: MessageKey = new MessageKey("CollapsableCardMessage", "Expand");
}

export const CultureInfoEntity: Type<CultureInfoEntity> = new Type<CultureInfoEntity>("CultureInfo");
export interface CultureInfoEntity extends Entities.Entity {
  Type: "CultureInfo";
  name: string;
  nativeName: string;
  englishName: string;
}

export namespace CultureInfoOperation {
  export const Save : Operations.ExecuteSymbol<CultureInfoEntity> = registerSymbol("Operation", "CultureInfoOperation.Save");
  export const Delete : Operations.DeleteSymbol<CultureInfoEntity> = registerSymbol("Operation", "CultureInfoOperation.Delete");
}

export const DeleteLogParametersEmbedded: Type<DeleteLogParametersEmbedded> = new Type<DeleteLogParametersEmbedded>("DeleteLogParametersEmbedded");
export interface DeleteLogParametersEmbedded extends Entities.EmbeddedEntity {
  Type: "DeleteLogParametersEmbedded";
  deleteLogs: Entities.MList<DeleteLogsTypeOverridesEmbedded>;
  chunkSize: number;
  maxChunks: number;
  pauseTime: number | null;
}

export const DeleteLogsTypeOverridesEmbedded: Type<DeleteLogsTypeOverridesEmbedded> = new Type<DeleteLogsTypeOverridesEmbedded>("DeleteLogsTypeOverridesEmbedded");
export interface DeleteLogsTypeOverridesEmbedded extends Entities.EmbeddedEntity {
  Type: "DeleteLogsTypeOverridesEmbedded";
  type: Entities.Lite<TypeEntity>;
  deleteLogsOlderThan: number | null;
  deleteLogsWithExceptionsOlderThan: number | null;
}

export namespace DisabledMessage {
  export const ParentIsDisabled: MessageKey = new MessageKey("DisabledMessage", "ParentIsDisabled");
}

export const DisabledMixin: Type<DisabledMixin> = new Type<DisabledMixin>("DisabledMixin");
export interface DisabledMixin extends Entities.MixinEntity {
  Type: "DisabledMixin";
  isDisabled: boolean;
}

export namespace DisableOperation {
  export const Disable : Operations.ExecuteSymbol<Entities.Entity> = registerSymbol("Operation", "DisableOperation.Disable");
  export const Enabled : Operations.ExecuteSymbol<Entities.Entity> = registerSymbol("Operation", "DisableOperation.Enabled");
}

export const ExceptionEntity: Type<ExceptionEntity> = new Type<ExceptionEntity>("Exception");
export interface ExceptionEntity extends Entities.Entity {
  Type: "Exception";
  creationDate: string /*DateTime*/;
  exceptionType: string | null;
  exceptionMessage: string;
  exceptionMessageHash: number;
  stackTrace: Entities.BigStringEmbedded;
  stackTraceHash: number;
  threadId: number;
  user: Entities.Lite<Security.IUserEntity> | null;
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
  form: Entities.BigStringEmbedded;
  queryString: Entities.BigStringEmbedded;
  session: Entities.BigStringEmbedded;
  data: Entities.BigStringEmbedded;
  hResult: number;
  referenced: boolean;
  origin: ExceptionOrigin;
  traceId: string | null;
}

export const ExceptionOrigin: EnumType<ExceptionOrigin> = new EnumType<ExceptionOrigin>("ExceptionOrigin");
export type ExceptionOrigin =
  "Backend_DotNet" |
  "Frontend_React";

export interface IEmailOwnerEntity extends Entities.Entity {
}

export const PermissionSymbol: Type<PermissionSymbol> = new Type<PermissionSymbol>("Permission");
export interface PermissionSymbol extends Symbol {
  Type: "Permission";
}

export const PropertyRouteEntity: Type<PropertyRouteEntity> = new Type<PropertyRouteEntity>("PropertyRoute");
export interface PropertyRouteEntity extends Entities.Entity {
  Type: "PropertyRoute";
  path: string;
  rootType: TypeEntity;
}

export namespace PropertyRouteMessage {
  export const Translated: MessageKey = new MessageKey("PropertyRouteMessage", "Translated");
}

export const QueryEntity: Type<QueryEntity> = new Type<QueryEntity>("Query");
export interface QueryEntity extends Entities.Entity {
  Type: "Query";
  key: string;
}

export namespace SearchVisualTip {
  export const SearchHelp : VisualTipSymbol = registerSymbol("VisualTip", "SearchVisualTip.SearchHelp");
  export const GroupHelp : VisualTipSymbol = registerSymbol("VisualTip", "SearchVisualTip.GroupHelp");
  export const FilterHelp : VisualTipSymbol = registerSymbol("VisualTip", "SearchVisualTip.FilterHelp");
  export const ColumnHelp : VisualTipSymbol = registerSymbol("VisualTip", "SearchVisualTip.ColumnHelp");
}

export interface SemiSymbol extends Entities.Entity {
  key: string | null;
  name: string;
}

export interface Symbol extends Entities.Entity {
  key: string;
}

export const SystemEventLogEntity: Type<SystemEventLogEntity> = new Type<SystemEventLogEntity>("SystemEventLog");
export interface SystemEventLogEntity extends Entities.Entity {
  Type: "SystemEventLog";
  machineName: string;
  date: string /*DateTime*/;
  user: Entities.Lite<Security.IUserEntity> | null;
  eventType: string;
  exception: Entities.Lite<ExceptionEntity> | null;
}

export const TranslatableRouteType: EnumType<TranslatableRouteType> = new EnumType<TranslatableRouteType>("TranslatableRouteType");
export type TranslatableRouteType =
  "Text" |
  "Html";

export const TypeEntity: Type<TypeEntity> = new Type<TypeEntity>("Type");
export interface TypeEntity extends Entities.Entity {
  Type: "Type";
  tableName: string;
  cleanName: string;
  namespace: string;
  className: string;
}

export const VisualTipConsumedEntity: Type<VisualTipConsumedEntity> = new Type<VisualTipConsumedEntity>("VisualTipConsumed");
export interface VisualTipConsumedEntity extends Entities.Entity {
  Type: "VisualTipConsumed";
  visualTip: VisualTipSymbol;
  user: Entities.Lite<Security.IUserEntity>;
  consumedOn: string /*DateTime*/;
}

export namespace VisualTipConsumedOperation {
  export const Delete : Operations.DeleteSymbol<VisualTipConsumedEntity> = registerSymbol("Operation", "VisualTipConsumedOperation.Delete");
}

export namespace VisualTipMessage {
  export const Help: MessageKey = new MessageKey("VisualTipMessage", "Help");
}

export const VisualTipSymbol: Type<VisualTipSymbol> = new Type<VisualTipSymbol>("VisualTip");
export interface VisualTipSymbol extends Symbol {
  Type: "VisualTip";
}

