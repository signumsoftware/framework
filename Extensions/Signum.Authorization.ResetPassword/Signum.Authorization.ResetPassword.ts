//////////////////////////////////
//Auto-generated. Do NOT modify!//
//////////////////////////////////

import { MessageKey, QueryKey, Type, EnumType, registerSymbol } from '../../Signum/React/Reflection'
import * as Entities from '../../Signum/React/Signum.Entities'
import * as Operations from '../../Signum/React/Signum.Operations'
import * as Authorization from '../Signum.Authorization/Signum.Authorization'


export module AuthEmailMessage {
  export const YouRecentlyRequestedANewPassword = new MessageKey("AuthEmailMessage", "YouRecentlyRequestedANewPassword");
  export const YourUsernameIs = new MessageKey("AuthEmailMessage", "YourUsernameIs");
  export const YouCanResetYourPasswordByFollowingTheLinkBelow = new MessageKey("AuthEmailMessage", "YouCanResetYourPasswordByFollowingTheLinkBelow");
  export const ResetPasswordRequestSubject = new MessageKey("AuthEmailMessage", "ResetPasswordRequestSubject");
  export const YourResetPasswordRequestHasExpired = new MessageKey("AuthEmailMessage", "YourResetPasswordRequestHasExpired");
  export const WeHaveSendYouAnEmailToResetYourPassword = new MessageKey("AuthEmailMessage", "WeHaveSendYouAnEmailToResetYourPassword");
  export const EmailNotFound = new MessageKey("AuthEmailMessage", "EmailNotFound");
  export const YourAccountHasBeenLockedDueToSeveralFailedLogins = new MessageKey("AuthEmailMessage", "YourAccountHasBeenLockedDueToSeveralFailedLogins");
  export const YourAccountHasBeenLocked = new MessageKey("AuthEmailMessage", "YourAccountHasBeenLocked");
}

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

