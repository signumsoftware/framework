//////////////////////////////////
//Auto-generated. Do NOT modify!//
//////////////////////////////////

import { MessageKey, QueryKey, Type, EnumType, registerSymbol } from '../../Signum/React/Reflection'
import * as Entities from '../../Signum/React/Signum.Entities'
import * as Operations from '../../Signum/React/Signum.Operations'
import * as Authorization from '../Signum.Authorization/Signum.Authorization'


export const ConcurrentUserEntity: Type<ConcurrentUserEntity> = new Type<ConcurrentUserEntity>("ConcurrentUser");
export interface ConcurrentUserEntity extends Entities.Entity {
  Type: "ConcurrentUser";
  targetEntity: Entities.Lite<Entities.Entity>;
  startTime: string /*DateTime*/;
  user: Entities.Lite<Authorization.UserEntity>;
  signalRConnectionID: string;
  isModified: boolean;
}

export namespace ConcurrentUserMessage {
  export const ConcurrentUsers: MessageKey = new MessageKey("ConcurrentUserMessage", "ConcurrentUsers");
  export const CurrentlyEditing: MessageKey = new MessageKey("ConcurrentUserMessage", "CurrentlyEditing");
  export const DatabaseChangesDetected: MessageKey = new MessageKey("ConcurrentUserMessage", "DatabaseChangesDetected");
  export const LooksLikeSomeoneJustSaved0ToTheDatabase: MessageKey = new MessageKey("ConcurrentUserMessage", "LooksLikeSomeoneJustSaved0ToTheDatabase");
  export const DoYouWantToReloadIt: MessageKey = new MessageKey("ConcurrentUserMessage", "DoYouWantToReloadIt");
  export const YouHaveLocalChangesIn0ThatIsCurrentlyOpenByOtherUsersSoFarNoOneElseHasMadeModifications: MessageKey = new MessageKey("ConcurrentUserMessage", "YouHaveLocalChangesIn0ThatIsCurrentlyOpenByOtherUsersSoFarNoOneElseHasMadeModifications");
  export const LooksLikeYouAreNotTheOnlyOneCurrentlyModifiying0OnlyTheFirstOneWillBeAbleToSaveChanges: MessageKey = new MessageKey("ConcurrentUserMessage", "LooksLikeYouAreNotTheOnlyOneCurrentlyModifiying0OnlyTheFirstOneWillBeAbleToSaveChanges");
  export const YouHaveLocalChangesBut0HasAlreadyBeenSavedInTheDatabaseYouWillNotBeAbleToSaveChanges: MessageKey = new MessageKey("ConcurrentUserMessage", "YouHaveLocalChangesBut0HasAlreadyBeenSavedInTheDatabaseYouWillNotBeAbleToSaveChanges");
  export const ThisIsNotTheLatestVersionOf0: MessageKey = new MessageKey("ConcurrentUserMessage", "ThisIsNotTheLatestVersionOf0");
  export const ReloadIt: MessageKey = new MessageKey("ConcurrentUserMessage", "ReloadIt");
  export const WarningYouWillLostYourCurrentChanges: MessageKey = new MessageKey("ConcurrentUserMessage", "WarningYouWillLostYourCurrentChanges");
  export const ConsiderOpening0InANewTabAndApplyYourChangesManually: MessageKey = new MessageKey("ConcurrentUserMessage", "ConsiderOpening0InANewTabAndApplyYourChangesManually");
}

export namespace ConcurrentUserOperation {
  export const Delete : Operations.DeleteSymbol<ConcurrentUserEntity> = registerSymbol("Operation", "ConcurrentUserOperation.Delete");
}

