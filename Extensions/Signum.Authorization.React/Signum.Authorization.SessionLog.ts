//////////////////////////////////
//Auto-generated. Do NOT modify!//
//////////////////////////////////

import { MessageKey, QueryKey, Type, EnumType, registerSymbol } from '../../Signum.React/Reflection'
import * as Entities from '../../Signum.React/Signum.Entities'
import * as Authorization from './Signum.Authorization'
import * as Rules from './Signum.Authorization.Rules'


export const SessionLogEntity = new Type<SessionLogEntity>("SessionLog");
export interface SessionLogEntity extends Entities.Entity {
  Type: "SessionLog";
  user: Entities.Lite<Authorization.UserEntity>;
  sessionStart: string /*DateTime*/;
  sessionEnd: string /*DateTime*/ | null;
  sessionTimeOut: boolean;
  userHostAddress: string | null;
  userAgent: string | null;
}

export module SessionLogPermission {
  export const TrackSession : Rules.PermissionSymbol = registerSymbol("Permission", "SessionLogPermission.TrackSession");
}

