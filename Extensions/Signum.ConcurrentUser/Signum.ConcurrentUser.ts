//////////////////////////////////
//Auto-generated. Do NOT modify!//
//////////////////////////////////

import { MessageKey, QueryKey, Type, EnumType, registerSymbol } from '../../Signum/React/Reflection'
import * as Entities from '../../Signum/React/Signum.Entities'
import * as Operations from '../../Signum/React/Signum.Operations'
import * as Authorization from '../Signum.Authorization/Signum.Authorization'


export const ConcurrentUserEntity = new Type<ConcurrentUserEntity>("ConcurrentUser");
export interface ConcurrentUserEntity extends Entities.Entity {
  Type: "ConcurrentUser";
  targetEntity: Entities.Lite<Entities.Entity>;
  startTime: string /*DateTime*/;
  user: Entities.Lite<Authorization.UserEntity>;
  signalRConnectionID: string;
  isModified: boolean;
}

export module ConcurrentUserMessage {
  export const ConcurrentUsers = new MessageKey("ConcurrentUserMessage", "ConcurrentUsers");
  export const CurrentlyEditing = new MessageKey("ConcurrentUserMessage", "CurrentlyEditing");
  export const DatabaseChangesDetected = new MessageKey("ConcurrentUserMessage", "DatabaseChangesDetected");
  export const LooksLikeSomeoneJustSaved0ToTheDatabase = new MessageKey("ConcurrentUserMessage", "LooksLikeSomeoneJustSaved0ToTheDatabase");
  export const DoYouWantToReloadIt = new MessageKey("ConcurrentUserMessage", "DoYouWantToReloadIt");
  export const YouHaveLocalChangesIn0ThatIsCurrentlyOpenByOtherUsersSoFarNoOneElseHasMadeModifications = new MessageKey("ConcurrentUserMessage", "YouHaveLocalChangesIn0ThatIsCurrentlyOpenByOtherUsersSoFarNoOneElseHasMadeModifications");
  export const LooksLikeYouAreNotTheOnlyOneCurrentlyModifiying0OnlyTheFirstOneWillBeAbleToSaveChanges = new MessageKey("ConcurrentUserMessage", "LooksLikeYouAreNotTheOnlyOneCurrentlyModifiying0OnlyTheFirstOneWillBeAbleToSaveChanges");
  export const YouHaveLocalChangesBut0HasAlreadyBeenSavedInTheDatabaseYouWillNotBeAbleToSaveChanges = new MessageKey("ConcurrentUserMessage", "YouHaveLocalChangesBut0HasAlreadyBeenSavedInTheDatabaseYouWillNotBeAbleToSaveChanges");
  export const ThisIsNotTheLatestVersionOf0 = new MessageKey("ConcurrentUserMessage", "ThisIsNotTheLatestVersionOf0");
  export const ReloadIt = new MessageKey("ConcurrentUserMessage", "ReloadIt");
  export const WarningYouWillLostYourCurrentChanges = new MessageKey("ConcurrentUserMessage", "WarningYouWillLostYourCurrentChanges");
  export const ConsiderOpening0InANewTabAndApplyYourChangesManually = new MessageKey("ConcurrentUserMessage", "ConsiderOpening0InANewTabAndApplyYourChangesManually");
}

export module ConcurrentUserOperation {
  export const Delete : Operations.DeleteSymbol<ConcurrentUserEntity> = registerSymbol("Operation", "ConcurrentUserOperation.Delete");
}

