//////////////////////////////////
//Auto-generated. Do NOT modify!//
//////////////////////////////////

import { MessageKey, QueryKey, Type, EnumType, registerSymbol } from '../../Signum/React/Reflection'
import * as Entities from '../../Signum/React/Signum.Entities'
import * as Operations from '../../Signum/React/Signum.Operations'
import * as Authorization from '../Signum.Authorization/Signum.Authorization'


export namespace ResetPasswordAuthMessage {
  export const PleaseConsiderRequestingANewLink: MessageKey = new MessageKey("ResetPasswordAuthMessage", "PleaseConsiderRequestingANewLink");
  export const RequestNewLink: MessageKey = new MessageKey("ResetPasswordAuthMessage", "RequestNewLink");
  export const NewLinkToResetPasswordHasBeenSentSuccessfully: MessageKey = new MessageKey("ResetPasswordAuthMessage", "NewLinkToResetPasswordHasBeenSentSuccessfully");
}

export namespace ResetPasswordMessage {
  export const YouRecentlyRequestedANewPassword: MessageKey = new MessageKey("ResetPasswordMessage", "YouRecentlyRequestedANewPassword");
  export const YourUsernameIs: MessageKey = new MessageKey("ResetPasswordMessage", "YourUsernameIs");
  export const YouCanResetYourPasswordByFollowingTheLinkBelow: MessageKey = new MessageKey("ResetPasswordMessage", "YouCanResetYourPasswordByFollowingTheLinkBelow");
  export const ResetPasswordRequestSubject: MessageKey = new MessageKey("ResetPasswordMessage", "ResetPasswordRequestSubject");
  export const YourResetPasswordRequestHasExpired: MessageKey = new MessageKey("ResetPasswordMessage", "YourResetPasswordRequestHasExpired");
  export const WeHaveSendYouAnEmailToResetYourPassword: MessageKey = new MessageKey("ResetPasswordMessage", "WeHaveSendYouAnEmailToResetYourPassword");
  export const EmailNotFound: MessageKey = new MessageKey("ResetPasswordMessage", "EmailNotFound");
  export const YourAccountHasBeenLockedDueToSeveralFailedLogins: MessageKey = new MessageKey("ResetPasswordMessage", "YourAccountHasBeenLockedDueToSeveralFailedLogins");
  export const YourAccountHasBeenLocked: MessageKey = new MessageKey("ResetPasswordMessage", "YourAccountHasBeenLocked");
  export const TheCodeOfYourLinkIsIncorrect: MessageKey = new MessageKey("ResetPasswordMessage", "TheCodeOfYourLinkIsIncorrect");
  export const TheCodeOfYourLinkHasAlreadyBeenUsed: MessageKey = new MessageKey("ResetPasswordMessage", "TheCodeOfYourLinkHasAlreadyBeenUsed");
  export const IfEmailIsValidWeWillSendYouAnEmailToResetYourPassword: MessageKey = new MessageKey("ResetPasswordMessage", "IfEmailIsValidWeWillSendYouAnEmailToResetYourPassword");
}

export const ResetPasswordRequestEntity: Type<ResetPasswordRequestEntity> = new Type<ResetPasswordRequestEntity>("ResetPasswordRequest");
export interface ResetPasswordRequestEntity extends Entities.Entity {
  Type: "ResetPasswordRequest";
  code: string;
  user: Authorization.UserEntity;
  requestDate: string /*DateTime*/;
  used: boolean;
}

export namespace ResetPasswordRequestOperation {
  export const Execute : Operations.ExecuteSymbol<ResetPasswordRequestEntity> = registerSymbol("Operation", "ResetPasswordRequestOperation.Execute");
}

