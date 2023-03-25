//////////////////////////////////
//Auto-generated. Do NOT modify!//
//////////////////////////////////

import { MessageKey, QueryKey, Type, EnumType, registerSymbol } from '../../Signum.React/Reflection'
import * as Entities from '../../Signum.React/Signum.Entities'
import * as Operations from '../../Signum.React/Signum.Operations'
import * as Authorization from '../Signum.Authorization.React/Signum.Authorization'


export const ResetPasswordRequestEntity = new Type<ResetPasswordRequestEntity>("ResetPasswordRequest");
export interface ResetPasswordRequestEntity extends Entities.Entity {
  Type: "ResetPasswordRequest";
  code: string;
  user: Authorization.UserEntity;
  requestDate: string /*DateTime*/;
  used: boolean;
}

export module ResetPasswordRequestOperation {
  export const Execute : Operations.ExecuteSymbol<ResetPasswordRequestEntity> = registerSymbol("Operation", "ResetPasswordRequestOperation.Execute");
}

