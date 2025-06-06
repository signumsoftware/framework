//////////////////////////////////
//Auto-generated. Do NOT modify!//
//////////////////////////////////

import { MessageKey, QueryKey, Type, EnumType, registerSymbol } from '../../Signum/React/Reflection'
import * as Entities from '../../Signum/React/Signum.Entities'
import * as Rules from '../Signum.Authorization/Rules/Signum.Authorization.Rules'


export namespace DiffLogMessage {
  export const PreviousLog: MessageKey = new MessageKey("DiffLogMessage", "PreviousLog");
  export const NextLog: MessageKey = new MessageKey("DiffLogMessage", "NextLog");
  export const CurrentEntity: MessageKey = new MessageKey("DiffLogMessage", "CurrentEntity");
  export const NavigatesToThePreviousOperationLog: MessageKey = new MessageKey("DiffLogMessage", "NavigatesToThePreviousOperationLog");
  export const DifferenceBetweenFinalStateOfPreviousLogAndTheInitialState: MessageKey = new MessageKey("DiffLogMessage", "DifferenceBetweenFinalStateOfPreviousLogAndTheInitialState");
  export const StateWhenTheOperationStarted: MessageKey = new MessageKey("DiffLogMessage", "StateWhenTheOperationStarted");
  export const DifferenceBetweenInitialStateAndFinalState: MessageKey = new MessageKey("DiffLogMessage", "DifferenceBetweenInitialStateAndFinalState");
  export const StateWhenTheOperationFinished: MessageKey = new MessageKey("DiffLogMessage", "StateWhenTheOperationFinished");
  export const DifferenceBetweenFinalStateAndTheInitialStateOfNextLog: MessageKey = new MessageKey("DiffLogMessage", "DifferenceBetweenFinalStateAndTheInitialStateOfNextLog");
  export const NavigatesToTheNextOperationLog: MessageKey = new MessageKey("DiffLogMessage", "NavigatesToTheNextOperationLog");
  export const DifferenceBetweenFinalStateAndTheCurrentStateOfTheEntity: MessageKey = new MessageKey("DiffLogMessage", "DifferenceBetweenFinalStateAndTheCurrentStateOfTheEntity");
  export const NavigatesToTheCurrentEntity: MessageKey = new MessageKey("DiffLogMessage", "NavigatesToTheCurrentEntity");
}

export const DiffLogMixin: Type<DiffLogMixin> = new Type<DiffLogMixin>("DiffLogMixin");
export interface DiffLogMixin extends Entities.MixinEntity {
  Type: "DiffLogMixin";
  initialState: Entities.BigStringEmbedded;
  finalState: Entities.BigStringEmbedded;
  cleaned: boolean;
}

export namespace OperationLogTypeCondition {
  export const FilteringByTarget : Rules.TypeConditionSymbol = registerSymbol("TypeCondition", "OperationLogTypeCondition.FilteringByTarget");
}

