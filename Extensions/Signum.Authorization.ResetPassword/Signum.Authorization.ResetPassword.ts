//////////////////////////////////
//Auto-generated. Do NOT modify!//
//////////////////////////////////

import { MessageKey, QueryKey, Type, EnumType, registerSymbol } from '../../Signum/React/Reflection'
import * as Entities from '../../Signum/React/Signum.Entities'
import * as Operations from '../../Signum/React/Signum.Operations'
import * as Authorization from '../Signum.Authorization/Signum.Authorization'


export module ResetPasswordMessage {
  export const YouRecentlyRequestedANewPassword = new MessageKey("ResetPasswordMessage", "YouRecentlyRequestedANewPassword");
  export const YourUsernameIs = new MessageKey("ResetPasswordMessage", "YourUsernameIs");
  export const YouCanResetYourPasswordByFollowingTheLinkBelow = new MessageKey("ResetPasswordMessage", "YouCanResetYourPasswordByFollowingTheLinkBelow");
  export const ResetPasswordRequestSubject = new MessageKey("ResetPasswordMessage", "ResetPasswordRequestSubject");
  export const YourResetPasswordRequestHasExpired = new MessageKey("ResetPasswordMessage", "YourResetPasswordRequestHasExpired");
  export const WeHaveSendYouAnEmailToResetYourPassword = new MessageKey("ResetPasswordMessage", "WeHaveSendYouAnEmailToResetYourPassword");
  export const EmailNotFound = new MessageKey("ResetPasswordMessage", "EmailNotFound");
  export const YourAccountHasBeenLockedDueToSeveralFailedLogins = new MessageKey("ResetPasswordMessage", "YourAccountHasBeenLockedDueToSeveralFailedLogins");
  export const YourAccountHasBeenLocked = new MessageKey("ResetPasswordMessage", "YourAccountHasBeenLocked");
  export const TheCodeOfYourLinkIsIncorrect = new MessageKey("ResetPasswordMessage", "TheCodeOfYourLinkIsIncorrect");
  export const TheCodeOfYourLinkHasAlreadyBeenUsed = new MessageKey("ResetPasswordMessage", "TheCodeOfYourLinkHasAlreadyBeenUsed");
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

