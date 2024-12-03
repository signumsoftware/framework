//////////////////////////////////
//Auto-generated. Do NOT modify!//
//////////////////////////////////

import { MessageKey, QueryKey, Type, EnumType, registerSymbol } from '../../Signum/React/Reflection'
import * as Entities from '../../Signum/React/Signum.Entities'
import * as Security from '../../Signum/React/Signum.Security'
import * as Basics from '../../Signum/React/Signum.Basics'
import * as Operations from '../../Signum/React/Signum.Operations'
import * as Authorization from '../Signum.Authorization/Signum.Authorization'


export const QueryStringValueEmbedded: Type<QueryStringValueEmbedded> = new Type<QueryStringValueEmbedded>("QueryStringValueEmbedded");
export interface QueryStringValueEmbedded extends Entities.EmbeddedEntity {
  Type: "QueryStringValueEmbedded";
  key: string;
  value: string;
}

export const RestApiKeyEntity: Type<RestApiKeyEntity> = new Type<RestApiKeyEntity>("RestApiKey");
export interface RestApiKeyEntity extends Entities.Entity {
  Type: "RestApiKey";
  user: Entities.Lite<Authorization.UserEntity>;
  apiKey: string;
}

export namespace RestApiKeyMessage {
  export const GenerateApiKey: MessageKey = new MessageKey("RestApiKeyMessage", "GenerateApiKey");
}

export namespace RestApiKeyOperation {
  export const Save : Operations.ExecuteSymbol<RestApiKeyEntity> = registerSymbol("Operation", "RestApiKeyOperation.Save");
  export const Delete : Operations.DeleteSymbol<RestApiKeyEntity> = registerSymbol("Operation", "RestApiKeyOperation.Delete");
}

export const RestLogEntity: Type<RestLogEntity> = new Type<RestLogEntity>("RestLog");
export interface RestLogEntity extends Entities.Entity {
  Type: "RestLog";
  httpMethod: string | null;
  url: string;
  startDate: string /*DateTime*/;
  endDate: string /*DateTime*/;
  replayDate: string /*DateTime*/ | null;
  requestBody: Entities.BigStringEmbedded;
  queryString: Entities.MList<QueryStringValueEmbedded>;
  user: Entities.Lite<Security.IUserEntity> | null;
  userHostAddress: string | null;
  userHostName: string | null;
  referrer: string | null;
  controller: string;
  controllerName: string | null;
  action: string;
  machineName: string | null;
  applicationName: string | null;
  exception: Entities.Lite<Basics.ExceptionEntity> | null;
  responseBody: Entities.BigStringEmbedded;
  replayState: RestLogReplayState | null;
  changedPercentage: number | null;
  allowReplay: boolean;
}

export const RestLogReplayState: EnumType<RestLogReplayState> = new EnumType<RestLogReplayState>("RestLogReplayState");
export type RestLogReplayState =
  "NoChanges" |
  "WithChanges";

