//////////////////////////////////
//Auto-generated. Do NOT modify!//
//////////////////////////////////

import { MessageKey, QueryKey, Type, EnumType, registerSymbol } from '@framework/Reflection'
import * as Entities from '@framework/Signum.Entities'
import * as Basics from '@framework/Signum.Entities.Basics'
import * as Authorization from '../Signum.Authorization/Signum.Entities.Authorization'


export const QueryStringValueEmbedded = new Type<QueryStringValueEmbedded>("QueryStringValueEmbedded");
export interface QueryStringValueEmbedded extends Entities.EmbeddedEntity {
  Type: "QueryStringValueEmbedded";
  key: string;
  value: string;
}

export const RestApiKeyEntity = new Type<RestApiKeyEntity>("RestApiKey");
export interface RestApiKeyEntity extends Entities.Entity {
  Type: "RestApiKey";
  user: Entities.Lite<Authorization.UserEntity>;
  apiKey: string;
}

export module RestApiKeyMessage {
  export const GenerateApiKey = new MessageKey("RestApiKeyMessage", "GenerateApiKey");
}

export module RestApiKeyOperation {
  export const Save : Entities.ExecuteSymbol<RestApiKeyEntity> = registerSymbol("Operation", "RestApiKeyOperation.Save");
  export const Delete : Entities.DeleteSymbol<RestApiKeyEntity> = registerSymbol("Operation", "RestApiKeyOperation.Delete");
}

export const RestLogEntity = new Type<RestLogEntity>("RestLog");
export interface RestLogEntity extends Entities.Entity {
  Type: "RestLog";
  httpMethod: string | null;
  url: string;
  startDate: string /*DateTime*/;
  endDate: string /*DateTime*/;
  replayDate: string /*DateTime*/ | null;
  requestBody: string | null;
  queryString: Entities.MList<QueryStringValueEmbedded>;
  user: Entities.Lite<Basics.IUserEntity> | null;
  userHostAddress: string | null;
  userHostName: string | null;
  referrer: string | null;
  controller: string;
  controllerName: string | null;
  action: string;
  machineName: string | null;
  applicationName: string | null;
  exception: Entities.Lite<Basics.ExceptionEntity> | null;
  responseBody: string | null;
  replayState: RestLogReplayState | null;
  changedPercentage: number | null;
  allowReplay: boolean;
}

export const RestLogReplayState = new EnumType<RestLogReplayState>("RestLogReplayState");
export type RestLogReplayState =
  "NoChanges" |
  "WithChanges";


