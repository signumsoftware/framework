//////////////////////////////////
//Auto-generated. Do NOT modify!//
//////////////////////////////////

import { MessageKey, QueryKey, Type, EnumType, registerSymbol } from '../../../Signum/React/Reflection'
import * as Entities from '../../../Signum/React/Signum.Entities'
import * as Basics from '../../../Signum/React/Signum.Basics'
import * as Authorization from '../Signum.Authorization'


export const SessionLogEntity: Type<SessionLogEntity> = new Type<SessionLogEntity>("SessionLog");
export interface SessionLogEntity extends Entities.Entity {
  Type: "SessionLog";
  user: Entities.Lite<Authorization.UserEntity>;
  sessionStart: string /*DateTime*/;
  sessionEnd: string /*DateTime*/ | null;
  sessionTimeOut: boolean;
  userHostAddress: string | null;
  userAgent: string | null;
}

export namespace SessionLogPermission {
  export const TrackSession : Basics.PermissionSymbol = registerSymbol("Permission", "SessionLogPermission.TrackSession");
}

